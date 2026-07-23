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
            var pfCount = from bf in dc.CompanyFacets
                          join bfsv in dc.CompanyFacetValues on bf.FacetValue equals bfsv.Id
                          join bfsd in dc.CompanyFacetSetDetails on bf.FacetValue equals bfsd.Value
                          where bf.Company == companyId
                          group bfsd by bfsd.Set into g
                          select new { Set = g.Key, Count = g.Count() };

            var excl = from bf in dc.CompanyFacets
                       join bfsv in dc.CompanyFacetValues on bf.FacetValue equals bfsv.Id
                       join bfsd in dc.CompanyFacetSetDetails on bf.FacetValue equals bfsd.Value
                       where bf.Company == companyId && bfsd.Exclude
                       group bfsd by bfsd.Set into g
                       select (int?)g.Key;

            var q = from bfs in dc.CompanyFacetSets
                    join bfc in pfCount on new { Set = bfs.Id, Count = bfs.InclFacets } equals new { bfc.Set, Count = (byte)bfc.Count }
                    join e in excl on bfs.Id equals e into et
                    from e in et.DefaultIfEmpty()
                    where e == null
                    select bfs.Id;

            var sets = await q.ToArrayAsync(cancellationToken);

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

        public static async Task IndexProductAsync(AppDbContext dc, long productId, CancellationToken cancellationToken = default)
        {
            if (productId <= 0)
                throw new ArgumentOutOfRangeException(nameof(productId));

            var product = await dc.Products.SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product == null)
                return;

            var companyProduct = await dc.CompanyProducts.SingleOrDefaultAsync(cp => cp.Product == productId, cancellationToken);
            if (companyProduct == null)
                return;

            var cache = await dc.CompanyProductFacets
                .Where(pf => pf.Product == productId && !pf.UserDefined)
                .ToArrayAsync(cancellationToken);

            var facets = new System.Collections.Generic.List<LocalFacet>();
            var processed = new System.Collections.Generic.List<long>();

            if (!(product.Status == (byte)BizSrt.Model.Product.Status.Pending || product.Status == (byte)BizSrt.Model.Product.Status.Rejected || product.Status == (byte)BizSrt.Model.Product.Status.Deleted))
            {
                createProductFacet(facets, product, companyProduct);
            }
            else
            {
                createProductFacet(facets, product, companyProduct, FacetValueType._Status);
            }

            bool refreshFacetSets = false;
            foreach (var facet in facets)
            {
                var facetName = BizSrt.Api.Data.Cache.LegacyCache.CompanyProductFacetNames[facet.Name, BizSrt.Foundation.Cache.TwoKeySuppress.None, facet.Name];
                var facetValue = BizSrt.Api.Data.Cache.LegacyCache.CompanyProductFacetValues[new BizSrt.Api.Data.Cache.Company.Product.Facet.CachedValue.Key { Name = facetName, ValueType = (byte)facet.ValueType, Value = facet.ValueData }, BizSrt.Foundation.Cache.TwoKeySuppress.None, facet.ValueText];
                
                var pf = cache.SingleOrDefault(cpf => cpf.Product == productId && cpf.FacetValue == facetValue);
                if (pf == null)
                {
                    pf = new CompanyProductFacet { Product = productId, FacetValue = facetValue, UserDefined = false };
                    dc.CompanyProductFacets.Add(pf);
                    refreshFacetSets = true;
                }
                else
                {
                    processed.Add(pf.Id);
                }
            }

            var dpf = cache.Where(cpf => !processed.Contains(cpf.Id)).ToArray();
            if (dpf.Length > 0)
            {
                dc.CompanyProductFacets.RemoveRange(dpf);
                refreshFacetSets = true;
            }

            companyProduct.Indexed = DateTime.UtcNow;

            await dc.SaveChangesAsync(cancellationToken);

            if (refreshFacetSets)
            {
                await refreshProductFacetSetsAsync(dc, productId, cancellationToken);
            }
        }

        private static async Task refreshProductFacetSetsAsync(AppDbContext dc, long productId, CancellationToken cancellationToken)
        {
            var pfCount = from pf in dc.CompanyProductFacets
                          join pfsv in dc.CompanyProductFacetValues on pf.FacetValue equals pfsv.Id
                          join pfsd in dc.CompanyProductFacetSetDetails on pf.FacetValue equals pfsd.Value
                          where pf.Product == productId
                          group pfsd by pfsd.Set into g
                          select new { Set = g.Key, Count = g.Count() };

            var excl = from pf in dc.CompanyProductFacets
                       join pfsv in dc.CompanyProductFacetValues on pf.FacetValue equals pfsv.Id
                       join pfsd in dc.CompanyProductFacetSetDetails on pf.FacetValue equals pfsd.Value
                       where pf.Product == productId && pfsd.Exclude
                       group pfsd by pfsd.Set into g
                       select (int?)g.Key;

            var q = from pfs in dc.CompanyProductFacetSets
                    join pfc in pfCount on new { Set = pfs.Id, Count = pfs.InclFacets } equals new { pfc.Set, Count = (byte)pfc.Count }
                    join e in excl on pfs.Id equals e into et
                    from e in et.DefaultIfEmpty()
                    where e == null
                    select pfs.Id;

            var sets = await q.ToArrayAsync(cancellationToken);

            var existingSets = await dc.FacetSetCompanyProducts
                .Where(fsp => fsp.Product == productId)
                .Select(fsp => fsp.FacetSet)
                .ToListAsync(cancellationToken);

            var toAdd = sets.Except(existingSets).Select(s => new FacetSetCompanyProduct { FacetSet = s, Product = productId }).ToArray();
            var toRemoveSets = existingSets.Except(sets).ToArray();

            if (toAdd.Length > 0)
                dc.FacetSetCompanyProducts.AddRange(toAdd);

            if (toRemoveSets.Length > 0)
            {
                var toRemove = await dc.FacetSetCompanyProducts
                    .Where(fsp => fsp.Product == productId && toRemoveSets.Contains(fsp.FacetSet))
                    .ToListAsync(cancellationToken);
                dc.FacetSetCompanyProducts.RemoveRange(toRemove);
            }

            await dc.SaveChangesAsync(cancellationToken);
        }

        public static async Task IndexProductFacetSetAsync(AppDbContext dc, int setId, CancellationToken cancellationToken = default)
        {
            var facets = await (from pfsd in dc.CompanyProductFacetSetDetails
                                join pfsv in dc.CompanyProductFacetValues on pfsd.Value equals pfsv.Id
                                where pfsd.Set == setId
                                select new BizSrt.Model.Semantic.Facet { Name = pfsv.Name, Value = pfsv.Id, Exclude = pfsd.Exclude })
                               .ToArrayAsync(cancellationToken);

            if (facets.Length > 0)
            {
                var facetSet = await dc.CompanyProductFacetSets.SingleAsync(bfs => bfs.Id == setId, cancellationToken);
                var cq = BizSrt.Api.Data.Company.Product.ProfileQueryExtensions.Get(dc, new BizSrt.Model.Semantic.FacetFilter(facets, false), new BizSrt.Model.Semantic.FacetFilter(facets, true));

                var existingFsp = await dc.FacetSetCompanyProducts.Where(fsp => fsp.FacetSet == setId).ToArrayAsync(cancellationToken);
                dc.FacetSetCompanyProducts.RemoveRange(existingFsp);

                var productIds = await cq.Select(p => p.Id).ToArrayAsync(cancellationToken);
                dc.FacetSetCompanyProducts.AddRange(productIds.Select(pid => new FacetSetCompanyProduct { FacetSet = setId, Product = pid }));

                facetSet.Indexed = DateTime.UtcNow;
                await dc.SaveChangesAsync(cancellationToken);

                var cachedSet = BizSrt.Api.Data.Cache.LegacyCache.CompanyProductFacetSets?[setId];
                if (cachedSet != null)
                {
                    cachedSet.Indexed = true;
                }
            }
            else
            {
                throw new InvalidOperationException($"No Company Product Facets found for Set {setId}");
            }
        }

        private static void createProductFacet(System.Collections.Generic.List<LocalFacet> facets, Product product, CompanyProduct companyProduct)
        {
            if (product.Id > 0)
            {
                if (companyProduct.Category > 0)
                {
                    var cat = BizSrt.Api.Data.Cache.LegacyCache.Categories?[companyProduct.Category];
                    if (cat != null)
                    {
                        facets.Add(new LocalFacet(FacetValueType._Category, false)
                        {
                            Name = "Category",
                            ValueData = BitConverter.GetBytes(companyProduct.Category),
                            ValueText = cat.QualifiedName
                        });
                    }
                }
                if (product.Type != 0)
                {
                    var types = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.Product.ProductType>(BizSrt.Model.DictionaryType.ProductType);
                    if (types != null)
                    {
                        foreach (var t in types)
                        {
                            if ((t.ItemKey & product.Type) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Type, false)
                                {
                                    Name = "Type",
                                    ValueData = BitConverter.GetBytes(t.ItemKey),
                                    ValueText = t.ItemText
                                });
                        }
                    }
                }
                if (companyProduct.ServiceType != null && companyProduct.ServiceType != 0)
                {
                    var serviceTypes = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.ServiceType>(BizSrt.Model.DictionaryType.ServiceType);
                    if (serviceTypes != null)
                    {
                        foreach (var st in serviceTypes)
                        {
                            if ((st.ItemKey & companyProduct.ServiceType) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Type, false)
                                {
                                    Name = "Type",
                                    ValueData = BitConverter.GetBytes(-Convert.ToInt32(st.ItemKey)),
                                    ValueText = st.ItemText
                                });
                        }
                    }
                }
                if (companyProduct.Industry != null && companyProduct.Industry != 0)
                {
                    var industries = BizSrt.Api.Data.Cache.LegacyCache.Dictionary?.Get<BizSrt.Model.Industry>(BizSrt.Model.DictionaryType.Industry);
                    if (industries != null)
                    {
                        foreach (var industry in industries)
                        {
                            if ((industry.ItemKey & companyProduct.Industry) > 0)
                                facets.Add(new LocalFacet(FacetValueType._Industry, false)
                                {
                                    Name = "Industry",
                                    ValueData = BitConverter.GetBytes(industry.ItemKey),
                                    ValueText = industry.ItemText
                                });
                        }
                    }
                }

                createProductFacet(facets, product, companyProduct, FacetValueType._Status);
            }
        }

        private static void createProductFacet(System.Collections.Generic.List<LocalFacet> facets, Product product, CompanyProduct companyProduct, FacetValueType type)
        {
            if (product.Id > 0 && type == FacetValueType._Status)
            {
                var statusEnum = (BizSrt.Model.Product.Status)product.Status;
                facets.Add(new LocalFacet(FacetValueType._Status, false)
                {
                    Name = "Status",
                    ValueData = new byte[] { (byte)product.Status },
                    ValueText = BizSrt.Foundation.Text.StringEnum.GetStringValue(statusEnum)
                });
            }
        }

        public static async Task DeleteProductFacetSetAsync(AppDbContext dc, int setId, CancellationToken cancellationToken = default)
        {
            var facetSet = await dc.CompanyProductFacetSets.SingleOrDefaultAsync(pfs => pfs.Id == setId, cancellationToken);
            if (facetSet != null)
            {
                var existingFsp = await dc.FacetSetCompanyProducts.Where(fsp => fsp.FacetSet == setId).ToArrayAsync(cancellationToken);
                dc.FacetSetCompanyProducts.RemoveRange(existingFsp);

                var existingPfsd = await dc.CompanyProductFacetSetDetails.Where(pfsd => pfsd.Set == setId).ToArrayAsync(cancellationToken);
                dc.CompanyProductFacetSetDetails.RemoveRange(existingPfsd);

                dc.CompanyProductFacetSets.Remove(facetSet);

                await dc.SaveChangesAsync(cancellationToken);

                var cachedSet = BizSrt.Api.Data.Cache.LegacyCache.CompanyProductFacetSets?[setId];
                if (cachedSet != null)
                {
                    cachedSet.Indexed = false;
                }
            }
        }
    }
}
