using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Model;
using BizSrt.Foundation.Entity;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True");

using (var db = new AppDbContext(optionsBuilder.Options))
{
    var media = db.CompanyMedia.Where(m => m.Type == (byte)MediaType.Default_Image && m.Metadata != null).Take(10).Select(m => new { m.Id, m.Company, m.Metadata }).ToList();
    foreach (var m in media) {
        var size = BizSrt.Foundation.Entity.Image.ResolveSize(ImageEntity.Company, m.Metadata);
        Console.WriteLine($"Company {m.Company}, Media {m.Id}: Size={size} (Metadata len={m.Metadata.Length})");
        var meta = new BizSrt.Foundation.Entity.Image.ImageMetadata(m.Metadata);
        Console.WriteLine($"  -> parsed width={meta.Width}, height={meta.Height}");
    }
}
