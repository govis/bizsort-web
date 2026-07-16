using System.Linq;
using BizSrt.Data.Entities;

namespace BizSrt.Data;

public static class QueryExtensions
{
    public static IQueryable<CompanyOffice> LocationQuery(this IQueryable<CompanyOffice> offices, AppDbContext dbContext, int location)
    {
        if (location <= 0) return offices;

        var childLocations = dbContext.Locations_Unwound
            .Where(lu => lu.Parent == location)
            .Select(lu => lu.Child)
            .ToList();
        
        childLocations.Add(location);

        return offices.Where(co => childLocations.Contains(co.Location));
    }
}
