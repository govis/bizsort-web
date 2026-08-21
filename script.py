code = '''using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True");

using (var db = new AppDbContext(optionsBuilder.Options))
{
    var media = db.CompanyMedia.Where(m => (m.Type & 3) > 0 && m.Metadata != null).Take(5).Select(m => m.Metadata).ToList();
    foreach (var m in media) {
        Console.WriteLine($"Length: {m.Length}, Bytes: {BitConverter.ToString(m)}");
    }
}'''
with open(r'TestResolve\Program.cs', 'w') as f: f.write(code)
