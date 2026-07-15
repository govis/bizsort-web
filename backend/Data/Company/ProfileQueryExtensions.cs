using System.Linq;
using BizSrt.Data;
using BizSrt.Data.Entities;
using BizSrt.Model.Semantic;

namespace BizSrt.Api.Data.Company;

public static class ProfileQueryExtensions
{
    private static IQueryable<CompanyFacet> GetFacets(AppDbContext dc, FacetFilter facetFilter)
    {
        if (facetFilter.NoFilters > 1)
        {
            return from cf in dc.CompanyFacets
                   join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                   // We cast cfv.Name to int to match the int[] array
                   where facetFilter.FilterNames.Contains((int)cfv.Name) && facetFilter.FilterValues.Contains(cfv.Id)
                   select cf;
        }
        else
        {
            var facetName = facetFilter.FilterNames[0];
            var facetValue = facetFilter.FilterValues[0];
            return from cf in dc.CompanyFacets
                   join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                   where cfv.Name == facetName && cfv.Id == facetValue
                   select cf;
        }
    }

    public static IQueryable<CompanyProfile> Get(AppDbContext dc, FacetFilter? include, FacetFilter? exclude)
    {
        IQueryable<CompanyProfile> cq = dc.CompanyProfiles;

        if (include != null && include.NoFilters > 0 && exclude != null && exclude.NoFilters > 0)
        {
            return from c in cq
                   join cfi in (from cf in GetFacets(dc, include)
                                group cf by cf.Company into cfg
                                where cfg.Count() == include.NoFilters
                                select cfg.Key) on c.Id equals cfi
                   join cfe in (from cf in GetFacets(dc, exclude)
                                group cf by cf.Company into cfg
                                where cfg.Count() > 0
                                select (int?)cfg.Key) on c.Id equals cfe into cfet
                   from cfe in cfet.DefaultIfEmpty()
                   where cfe == null
                   select c;
        }

        if (include != null && include.NoFilters > 0)
        {
            cq = from c in cq
                 join cf in (from cf in GetFacets(dc, include)
                             group cf by cf.Company into cfg
                             where cfg.Count() == include.NoFilters
                             select cfg.Key) on c.Id equals cf
                 select c;
        }

        if (exclude != null && exclude.NoFilters > 0)
        {
            cq = from c in cq
                 from cf in GetFacets(dc, exclude)
                 .Where(cf => cf.Company == c.Id)
                 .Take(1)
                 .DefaultIfEmpty()
                 where cf == null
                 select c;
        }

        return cq;
    }
}
