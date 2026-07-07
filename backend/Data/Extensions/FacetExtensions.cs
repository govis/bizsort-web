using System.Collections.Generic;
using System.Linq;
using BizSrt.Api.Model.Semantic;

namespace BizSrt.Api.Data.Extensions;

public static class FacetExtensions
{
    public interface IValueCount
    {
        short Name { get; }
        int Value { get; }
        int Count { get; }
    }

    public class ValueCount : IValueCount
    {
        public short Name { get; set; }
        public int Value { get; set; }
        public int Count { get; set; }
    }

    public static FacetName[] GetFacets(IEnumerable<IValueCount> facets, FacetFilter? inclFacets, int recCount)
    {
        var facetNames = new List<FacetName>();

        var fnq = from f in facets
                  where inclFacets == null || inclFacets.NoFilters == 0 ||
                  (inclFacets.NoFilters == 1 && (f.Name != inclFacets.FilterNames[0] || f.Value != inclFacets.FilterValues[0])) ||
                  (inclFacets.NoFilters > 1 && (!inclFacets.FilterNames.Contains(f.Name) || !inclFacets.FilterValues.Contains(f.Value)))
                  group f by f.Name into fg
                  select fg;

        foreach (var fn in fnq)
        {
            var values = (from fv in fn
                          where recCount == 0 || fv.Count < recCount
                          select new FacetValue
                          {
                              Key = fv.Value,
                              Count = fv.Count
                          }).ToArray();

            if (values.Length > 0)
            {
                facetNames.Add(new FacetName
                {
                    Key = fn.Key,
                    Values = values
                });
            }
        }

        return facetNames.ToArray();
    }
}
