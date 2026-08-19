using System.Linq;
using BizSrt.Data;
using BizSrt.Data.Entities;
using BizSrt.Model.Semantic;

namespace BizSrt.Api.Data.Company;

public static class OfferingQueryExtensions
{
    private static IQueryable<CompanyOfferingFacet> GetFacets(AppDbContext dc, FacetFilter facetFilter)
    {
        if (facetFilter.NoFilters > 1)
        {
            return from cpf in dc.CompanyOfferingFacets
                   join cpfv in dc.CompanyOfferingFacetValues on cpf.FacetValue equals cpfv.Id
                   where facetFilter.FilterNames.Contains((int)cpfv.Name) && facetFilter.FilterValues.Contains(cpfv.Id)
                   select cpf;
        }
        else
        {
            var facetName = facetFilter.FilterNames[0];
            var facetValue = facetFilter.FilterValues[0];
            return from cpf in dc.CompanyOfferingFacets
                   join cpfv in dc.CompanyOfferingFacetValues on cpf.FacetValue equals cpfv.Id
                   where cpfv.Name == facetName && cpfv.Id == facetValue
                   select cpf;
        }
    }

    public static IQueryable<Offering> Get(AppDbContext dc, FacetFilter? include, FacetFilter? exclude)
    {
        IQueryable<Offering> cq = dc.Offerings;

        if (include != null && include.NoFilters > 0 && exclude != null && exclude.NoFilters > 0)
        {
            return from p in cq
                   join cfi in (from cf in GetFacets(dc, include)
                                group cf by cf.Offering into cfg
                                where cfg.Count() == include.NoFilters
                                select cfg.Key) on p.Id equals cfi
                   join cfe in (from cf in GetFacets(dc, exclude)
                                group cf by cf.Offering into cfg
                                where cfg.Count() > 0
                                select (long?)cfg.Key) on p.Id equals cfe into cfet
                   from cfe in cfet.DefaultIfEmpty()
                   where cfe == null
                   select p;
        }

        if (include != null && include.NoFilters > 0)
        {
            cq = from p in cq
                 join cf in (from cf in GetFacets(dc, include)
                             group cf by cf.Offering into cfg
                             where cfg.Count() == include.NoFilters
                             select cfg.Key) on p.Id equals cf
                 select p;
        }

        if (exclude != null && exclude.NoFilters > 0)
        {
            cq = from p in cq
                 from cf in GetFacets(dc, exclude)
                 .Where(cf => cf.Offering == p.Id)
                 .Take(1)
                 .DefaultIfEmpty()
                 where cf == null
                 select p;
        }

        return cq;
    }
}
