using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Data.Entities;

namespace BizSrt.Api.Process
{
    public static class Company
    {
        public static async Task IndexCompanyAsync(AppDbContext dc, int companyId, CancellationToken cancellationToken = default)
        {
            if (companyId <= 0)
                throw new ArgumentOutOfRangeException(nameof(companyId));

            var company = await dc.CompanyProfiles.SingleOrDefaultAsync(c => c.Id == companyId, cancellationToken);
            if (company == null)
                return;

            // Update the indexed timestamp to indicate we have processed this company
            company.Indexed = DateTime.UtcNow;

            var cache = await dc.CompanyFacets
                .Where(bf => bf.Company == companyId && !bf.UserDefined)
                .ToArrayAsync(cancellationToken);

            var facets = new System.Collections.Generic.List<LocalFacet>();
            var processed = new System.Collections.Generic.List<long>();

            if (company.Category > 0)
            {
                createCompanyFacet(facets, company);
            }

            bool refreshFacetSets = false;
            foreach (var facet in facets)
            {
                var facetName = BizSrt.Api.Data.Cache.LegacyCache.CompanyFacetNames[facet.Name, BizSrt.Foundation.Cache.TwoKeySuppress.None, facet.Name];
                var facetValue = BizSrt.Api.Data.Cache.LegacyCache.CompanyFacetValues[new BizSrt.Api.Data.Cache.Company.Facet.CachedValue.Key { Name = facetName, ValueType = (byte)facet.ValueType, Value = facet.ValueData }, BizSrt.Foundation.Cache.TwoKeySuppress.None, facet.ValueText];
                var bf = cache.SingleOrDefault(cbf => cbf.Company == companyId && cbf.FacetValue == facetValue);
                if (bf == null)
                {
                    bf = new CompanyFacet { Company = companyId, FacetValue = facetValue, UserDefined = false };
                    dc.CompanyFacets.Add(bf);
                    refreshFacetSets = true;
                }
                else
                {
                    processed.Add(bf.Id);
                }
            }

            var dbf = cache.Where(cbf => !processed.Contains(cbf.Id)).ToArray();
            if (dbf.Length > 0)
            {
                dc.CompanyFacets.RemoveRange(dbf);
                refreshFacetSets = true;
            }

            company.Indexed = DateTime.UtcNow;

            var companyOffices = await dc.CompanyOffices
                .Where(co => co.Company == companyId)
                .ToArrayAsync(cancellationToken);

            if (companyOffices.Length > 0)
            {
                var officeIds = companyOffices.Select(co => co.Id).ToArray();
                var existingAuditIds = await dc.CompanyOffices_Audit
                    .Where(coa => officeIds.Contains(coa.Id))
                    .Select(coa => coa.Id)
                    .ToArrayAsync(cancellationToken);
                
                var existingAuditIdsSet = new System.Collections.Generic.HashSet<int>(existingAuditIds);

                foreach (var companyOffice in companyOffices)
                {
                    if (!existingAuditIdsSet.Contains(companyOffice.Id))
                    {
                        var coAudit = new CompanyOffice_Audit
                        {
                            Id = companyOffice.Id,
                            Company = companyOffice.Company,
                            PlaceId = companyOffice.PlaceId,
                            Location = companyOffice.Location,
                            PostalCode = companyOffice.PostalCode,
                            StreetNumber = companyOffice.StreetNumber,
                            StreetName = companyOffice.StreetName,
                            Address1 = companyOffice.Address1,
                            Phone = companyOffice.Phone,
                            Phone1 = companyOffice.Phone1,
                            Fax = companyOffice.Fax,
                            Name = companyOffice.Name
                        };
                        if (companyOffice.GeoLocation != null && companyOffice.GeoLocation is NetTopologySuite.Geometries.Point pt)
                        {
                            coAudit.Latitude = (float)pt.Y;
                            coAudit.Longitude = (float)pt.X;
                        }
                        coAudit.UpdatedBy = "IndexCompany";
                        coAudit.Updated = DateTime.UtcNow;
                        dc.CompanyOffices_Audit.Add(coAudit);
                    }
                }
            }

            await dc.SaveChangesAsync(cancellationToken);

            if (refreshFacetSets)
            {
                await refreshCompanyFacetSetsAsync(dc, companyId, cancellationToken);
            }
        }

        private static async Task refreshCompanyFacetSetsAsync(AppDbContext dc, int companyId, CancellationToken cancellationToken)
        {
            var sets = await (from fsd in dc.CompanyFacetSetDetails
                              join cf in dc.CompanyFacets on fsd.Value equals cf.FacetValue
                              where cf.Company == companyId
                              select fsd.Set)
                             .Distinct()
                             .ToArrayAsync(cancellationToken);

            var existingSets = await dc.FacetSetCompanies
                .Where(fsc => fsc.Company == companyId)
                .Select(fsc => fsc.FacetSet)
                .ToListAsync(cancellationToken);

            var toAdd = sets.Except(existingSets).Select(s => new FacetSetCompany { FacetSet = s, Company = companyId }).ToArray();
            var toRemoveSets = existingSets.Except(sets).ToArray();

            if (toAdd.Length > 0)
                dc.FacetSetCompanies.AddRange(toAdd);

            if (toRemoveSets.Length > 0)
            {
                var toRemove = await dc.FacetSetCompanies
                    .Where(fsc => fsc.Company == companyId && toRemoveSets.Contains(fsc.FacetSet))
                    .ToListAsync(cancellationToken);
                dc.FacetSetCompanies.RemoveRange(toRemove);
            }

            await dc.SaveChangesAsync(cancellationToken);
        }

        public static async Task IndexCompanyFacetSetAsync(AppDbContext dc, int setId, CancellationToken cancellationToken = default)
        {
            var facets = await (from bfsd in dc.CompanyFacetSetDetails
                                join bfsv in dc.CompanyFacetValues on bfsd.Value equals bfsv.Id
                                where bfsd.Set == setId
                                select new BizSrt.Model.Semantic.Facet { Name = bfsv.Name, Value = bfsv.Id, Exclude = bfsd.Exclude })
                               .ToArrayAsync(cancellationToken);

            if (facets.Length > 0)
            {
                var facetSet = await dc.CompanyFacetSets.SingleAsync(bfs => bfs.Id == setId, cancellationToken);
                var cq = BizSrt.Api.Data.Company.ProfileQueryExtensions.Get(dc, new BizSrt.Model.Semantic.FacetFilter(facets, false), new BizSrt.Model.Semantic.FacetFilter(facets, true));

                var existingFsc = await dc.FacetSetCompanies.Where(bsp => bsp.FacetSet == setId).ToArrayAsync(cancellationToken);
                dc.FacetSetCompanies.RemoveRange(existingFsc);

                var companyIds = await cq.Select(c => c.Id).ToArrayAsync(cancellationToken);
                dc.FacetSetCompanies.AddRange(companyIds.Select(cid => new FacetSetCompany { FacetSet = setId, Company = cid }));

                facetSet.Indexed = DateTime.UtcNow;
                await dc.SaveChangesAsync(cancellationToken);

                var cachedSet = BizSrt.Api.Data.Cache.LegacyCache.CompanyFacetSets?[setId];
                if (cachedSet != null)
                {
                    cachedSet.Indexed = true;
                }
            }
            else
            {
                throw new InvalidOperationException($"No Company Facets found for Set {setId}");
            }
        }

        private enum FacetValueType : byte
        {
            _Status = 1,
            _Category = 2,
            _Type = 3,
            _Industry = 4
        }

        private class LocalFacet
        {
            public FacetValueType ValueType { get; set; }
            public bool UserDefined { get; set; }
            public string Name { get; set; }
            public byte[] ValueData { get; set; } = Array.Empty<byte>();
            public string ValueText { get; set; } = string.Empty;

            public LocalFacet(FacetValueType valueType, bool userDefined)
            {
                ValueType = valueType;
                UserDefined = userDefined;
                Name = string.Empty;
            }
        }

        private static void createCompanyFacet(System.Collections.Generic.List<LocalFacet> facets, CompanyProfile company)
        {
            if (company.Id > 0)
            {
                if (company.Category > 0)
                {
                    var cat = BizSrt.Api.Data.Cache.LegacyCache.Categories?[company.Category];
                    if (cat != null)
                    {
                        facets.Add(new LocalFacet(FacetValueType._Category, false)
                        {
                            Name = "Category",
                            ValueData = BitConverter.GetBytes(company.Category),
                            ValueText = cat.QualifiedName
                        });
                    }
                }
                if (company.TransactionType != 0)
                {
                    var transactionTypes = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.TransactionType>(BizSrt.Model.DictionaryType.TransactionType);
                    if (transactionTypes != null)
                    {
                        foreach (var transactionType in transactionTypes)
                        {
                            if ((transactionType.ItemKey & company.TransactionType) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Type, false)
                                {
                                    Name = "Type",
                                    ValueData = BitConverter.GetBytes(transactionType.ItemKey),
                                    ValueText = transactionType.ItemText
                                });
                        }
                    }
                }
                if (company.ServiceType != 0)
                {
                    var serviceTypes = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.ServiceType>(BizSrt.Model.DictionaryType.ServiceType);
                    if (serviceTypes != null)
                    {
                        foreach (var serviceType in serviceTypes)
                        {
                            if ((serviceType.ItemKey & company.ServiceType) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Type, false)
                                {
                                    Name = "Type",
                                    ValueData = BitConverter.GetBytes(-Convert.ToInt32(serviceType.ItemKey)),
                                    ValueText = serviceType.ItemText
                                });
                        }
                    }
                }
                if (company.Industry != 0)
                {
                    var industries = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.Industry>(BizSrt.Model.DictionaryType.Industry);
                    if (industries != null)
                    {
                        foreach (var industry in industries)
                        {
                            if ((industry.ItemKey & company.Industry) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Industry, false)
                                {
                                    Name = "Industry",
                                    ValueData = BitConverter.GetBytes(industry.ItemKey),
                                    ValueText = industry.ItemText
                                });
                        }
                    }
                }
            }
        }
    }
}
