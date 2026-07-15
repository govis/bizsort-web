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

            // TODO: Port CompanyFacets cache logic when the required concurrent dictionary caches are fully ported.
            // Currently, CompanyFacetCache and LegacyCache.CompanyFacetValues are missing from BizSrt.Data.
            
            // TODO: Port CompanyFacets cache logic when the required concurrent dictionary caches are fully ported.
            // Currently, CompanyFacetCache and LegacyCache.CompanyFacetValues are missing from BizSrt.Data.
            
            bool refreshFacetSets = false;
            // var dbf = cache.Where(cbf => !processedValues.Contains(cbf.FacetValue)).ToArray();
            // if (dbf.Length > 0)
            // {
            //     // dc.CompanyFacets.RemoveRange(dbf); // Keyless, need to handle differently or map keys. 
            //     // TODO: Properly map CompanyFacet primary key in EF so we can remove
            //     refreshFacetSets = true;
            // }

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
                
                // TODO: Implement the LINQ building block CompanyProfile.Get(dc, inclFilters, exclFilters) to replace this logic
                // var cq = CompanyProfile.Get(dc, new Model.Semantic.FacetFilter(facets, false), new Model.Semantic.FacetFilter(facets, true));

                // dc.FacetSetCompanies.RemoveRange(dc.FacetSetCompanies.Where(bsp => bsp.FacetSet == setId));
                // dc.FacetSetCompanies.AddRange(from c in cq.ToArray()
                //                               select new FacetSetCompany { FacetSet = setId, Company = c.Id });

                facetSet.Indexed = DateTime.UtcNow;
                await dc.SaveChangesAsync(cancellationToken);

                // TODO: Cache.CompanyFacetSets[key] = setId; and update Indexed property in cache
            }
            else
            {
                throw new InvalidOperationException($"No Company Facets found for Set {setId}");
            }
        }
    }
}
