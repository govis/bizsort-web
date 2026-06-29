using System.Linq;
using BizSrt.Api.Data.Entities;

namespace BizSrt.Api.Data;

public static class QueryExtensions
{
    public static IQueryable<CompanyOffice> LocationQuery(this IQueryable<CompanyOffice> offices, AppDbContext dbContext, int location)
    {
        if (location <= 0) return offices;

        var childLocations = dbContext.Locations_Unwound
            .Where(lu => lu.Parent == location)
            .Select(lu => lu.Child);

        return offices.Where(co => co.Location == location || childLocations.Contains(co.Location));
    }
}
