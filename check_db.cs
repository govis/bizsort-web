using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
class Program {
    static void Main() {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=BizSort;Trusted_Connection=True;Encrypt=False;");
        using var dbContext = new AppDbContext(optionsBuilder.Options);
        var company = dbContext.CompanyProfiles.FirstOrDefault(c => c.Id == 8981);
        Console.WriteLine(company != null ? company.Name : "NOT FOUND");
    }
}
