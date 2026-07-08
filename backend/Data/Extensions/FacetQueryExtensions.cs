using System.Linq;
using BizSrt.Api.Data;
using BizSrt.Api.Data.Entities;
using BizSrt.Api.Model.Semantic;

namespace BizSrt.Api.Data.Extensions;

public static class FacetQueryExtensions
{
    public static IQueryable<CompanyProfile> ApplyFacets(
        this IQueryable<CompanyProfile> query, 
        AppDbContext dc, 
        FacetFilter? include, 
        FacetFilter? exclude)
    {
        if (include != null && include.NoFilters > 0 && exclude != null && exclude.NoFilters > 0)
        {
            return from c in query
                   join cfi in (from cf in dc.CompanyFacets
                                join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                                where include.FilterNames.Contains(cfv.Name) && include.FilterValues.Contains(cfv.Id)
                                group cf by cf.Company into cfg
                                where cfg.Count() == include.NoFilters
                                select cfg.Key) on c.Id equals cfi
                   join cfe in (from cf in dc.CompanyFacets
                                join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                                where exclude.FilterNames.Contains(cfv.Name) && exclude.FilterValues.Contains(cfv.Id)
                                group cf by cf.Company into cfg
                                where cfg.Count() > 0
                                select (int?)cfg.Key) on c.Id equals cfe into cfet
                   from cfe in cfet.DefaultIfEmpty()
                   where cfe == null
                   select c;
        }

        if (include != null && include.NoFilters > 0)
        {
            if (include.NoFilters == 1)
            {
                var facetName = include.FilterNames[0];
                var facetValue = include.FilterValues[0];
                query = from c in query
                        join cf in (from cf in dc.CompanyFacets
                                    join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                                    where cfv.Name == facetName && cfv.Id == facetValue
                                    select cf.Company) on c.Id equals cf
                        select c;
            }
            else
            {
                query = from c in query
                        join cf in (from cf in dc.CompanyFacets
                                    join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                                    where include.FilterNames.Contains(cfv.Name) && include.FilterValues.Contains(cfv.Id)
                                    group cf by cf.Company into cfg
                                    where cfg.Count() == include.NoFilters
                                    select cfg.Key) on c.Id equals cf
                        select c;
            }
        }

        if (exclude != null && exclude.NoFilters > 0)
        {
            query = from c in query
                    from cf in (from cf in dc.CompanyFacets
                                join cfv in dc.CompanyFacetValues on cf.FacetValue equals cfv.Id
                                where exclude.FilterNames.Contains(cfv.Name) && exclude.FilterValues.Contains(cfv.Id)
                                select cf)
                    .Where(cf => cf.Company == c.Id)
                    .Take(1)
                    .DefaultIfEmpty()
                    where cf == null
                    select c;
        }

        return query;
    }

    public static IQueryable<Product> ApplyFacets(
        this IQueryable<Product> query, 
        AppDbContext dc, 
        FacetFilter? include, 
        FacetFilter? exclude)
    {
        if (include != null && include.NoFilters > 0 && exclude != null && exclude.NoFilters > 0)
        {
            return from p in query
                   join pfi in (from pf in dc.CompanyProductFacets
                                join pfv in dc.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                                where include.FilterNames.Contains(pfv.Name) && include.FilterValues.Contains(pfv.Id)
                                group pf by pf.Product into pfg
                                where pfg.Count() == include.NoFilters
                                select pfg.Key) on p.Id equals pfi
                   join pfe in (from pf in dc.CompanyProductFacets
                                join pfv in dc.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                                where exclude.FilterNames.Contains(pfv.Name) && exclude.FilterValues.Contains(pfv.Id)
                                group pf by pf.Product into pfg
                                where pfg.Count() > 0
                                select (long?)pfg.Key) on p.Id equals pfe into pfet
                   from pfe in pfet.DefaultIfEmpty()
                   where pfe == null
                   select p;
        }

        if (include != null && include.NoFilters > 0)
        {
            if (include.NoFilters == 1)
            {
                var facetName = include.FilterNames[0];
                var facetValue = include.FilterValues[0];
                query = from p in query
                        join pf in (from pf in dc.CompanyProductFacets
                                    join pfv in dc.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                                    where pfv.Name == facetName && pfv.Id == facetValue
                                    select pf.Product) on p.Id equals pf
                        select p;
            }
            else
            {
                query = from p in query
                        join pf in (from pf in dc.CompanyProductFacets
                                    join pfv in dc.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                                    where include.FilterNames.Contains(pfv.Name) && include.FilterValues.Contains(pfv.Id)
                                    group pf by pf.Product into pfg
                                    where pfg.Count() == include.NoFilters
                                    select pfg.Key) on p.Id equals pf
                        select p;
            }
        }

        if (exclude != null && exclude.NoFilters > 0)
        {
            query = from p in query
                    from pf in (from pf in dc.CompanyProductFacets
                                join pfv in dc.CompanyProductFacetValues on pf.FacetValue equals pfv.Id
                                where exclude.FilterNames.Contains(pfv.Name) && exclude.FilterValues.Contains(pfv.Id)
                                select pf)
                    .Where(pf => pf.Product == p.Id)
                    .Take(1)
                    .DefaultIfEmpty()
                    where pf == null
                    select p;
        }

        return query;
    }
}
