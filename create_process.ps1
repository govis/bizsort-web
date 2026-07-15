$companyProcess = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Model;
using BizSrt.Data.Cache;
using BizSrt.Data.Entities;
using BizSrt.Data.Master;

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

            // In legacy, this fetched Admin.Data.Company.Profile. 
            // We use the basic CompanyProfile for now, which has Category, TransactionType, ServiceType, Industry.

            var cache = await dc.CompanyFacets
                .Where(bf => bf.Company == companyId) // In legacy there was UserDefined, we skip if missing
                .ToArrayAsync(cancellationToken);

            var facets = new List<BizSrt.Model.Company.Facet>();

            if (company.Category > 0)
            {
                createCompanyFacet(facets, company);
            }

            bool refreshFacetSets = false;
            var processedValues = new List<int>();

            foreach (var facet in facets)
            {
                var facetName = LegacyCache.CompanyFacetNames[facet.Name];
                var facetValue = LegacyCache.CompanyFacetValues[new CompanyFacetCache.CachedValue.Key(facetName, (byte)facet.ValueType, facet.ValueData), BizSrt.Foundation.Cache.TwoKeySuppress.None, facet.ValueText];
                
                var bf = cache.SingleOrDefault(cbf => cbf.Company == companyId && cbf.FacetValue == facetValue);
                if (bf == null)
                {
                    bf = new CompanyFacet { Company = companyId, FacetValue = facetValue };
                    dc.CompanyFacets.Add(bf);
                    refreshFacetSets = true;
                }
                else
                {
                    processedValues.Add(bf.FacetValue);
                }
            }

            var dbf = cache.Where(cbf => !processedValues.Contains(cbf.FacetValue)).ToArray();
            if (dbf.Length > 0)
            {
                dc.CompanyFacets.RemoveRange(dbf);
                refreshFacetSets = true;
            }

            company.Indexed = DateTime.UtcNow;

            // Historical Office Audits
            var companyOffices = await dc.CompanyOffices
                .Where(co => co.Company == companyId)
                .ToArrayAsync(cancellationToken);
                
            foreach (var companyOffice in companyOffices)
            {
                bool exists = await dc.CompanyOffices_Audit.AnyAsync(coa => coa.Id == companyOffice.Id, cancellationToken);
                if (!exists)
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
                    if (companyOffice.GeoLocation != null)
                    {
                        coAudit.Latitude = (float)companyOffice.GeoLocation.Y;
                        coAudit.Longitude = (float)companyOffice.GeoLocation.X;
                    }
                    coAudit.UpdatedBy = "IndexCompany";
                    coAudit.Updated = DateTime.UtcNow;
                    dc.CompanyOffices_Audit.Add(coAudit);
                }
            }

            await dc.SaveChangesAsync(cancellationToken);

            if (refreshFacetSets)
            {
                await refreshCompanyFacetSetsAsync(dc, companyId, cancellationToken);
            }
        }

        private static void createCompanyFacet(List<BizSrt.Model.Company.Facet> facets, CompanyProfile company)
        {
            if (company.Category > 0)
            {
                facets.Add(new BizSrt.Model.Company.Facet(BizSrt.Model.Company.FacetValueType._Category, false)
                {
                    Name = "Category",
                    ValueData = BitConverter.GetBytes(company.Category),
                    ValueText = LegacyCache.Categories[company.Category]?.QualifiedName ?? ""
                });
            }
            if (company.TransactionType != 0)
            {
                var transactionTypes = Dictionary.Get<TransactionType>(DictionaryType.TransactionType);
                foreach (var transactionType in transactionTypes)
                {
                    if ((transactionType.ItemKey & company.TransactionType) > 0)
                    {
                        facets.Add(new BizSrt.Model.Company.Facet(BizSrt.Model.Company.FacetValueType._Type, false)
                        {
                            Name = "Type",
                            ValueData = BitConverter.GetBytes(transactionType.ItemKey),
                            ValueText = transactionType.ItemText
                        });
                    }
                }
            }
            if (company.ServiceType != 0)
            {
                var serviceTypes = Dictionary.Get<ServiceType>(DictionaryType.ServiceType);
                foreach (var serviceType in serviceTypes)
                {
                    if ((serviceType.ItemKey & company.ServiceType) > 0)
                    {
                        facets.Add(new BizSrt.Model.Company.Facet(BizSrt.Model.Company.FacetValueType._Type, false)
                        {
                            Name = "Type",
                            ValueData = BitConverter.GetBytes(-Convert.ToInt32(serviceType.ItemKey)), 
                            ValueText = serviceType.ItemText
                        });
                    }
                }
            }
            if (company.Industry != 0)
            {
                var industries = Dictionary.Get<Industry>(DictionaryType.Industry);
                foreach (var industry in industries)
                {
                    if ((industry.ItemKey & company.Industry) > 0)
                    {
                        facets.Add(new BizSrt.Model.Company.Facet(BizSrt.Model.Company.FacetValueType._Industry, false)
                        {
                            Name = "Industry",
                            ValueData = BitConverter.GetBytes(industry.ItemKey),
                            ValueText = industry.ItemText
                        });
                    }
                }
            }
        }

        private static async Task refreshCompanyFacetSetsAsync(AppDbContext dc, int company, CancellationToken cancellationToken)
        {
            var pfCountQuery = from bf in dc.CompanyFacets
                          join bfsv in dc.CompanyFacetValues on bf.FacetValue equals bfsv.Id
                          join bfsd in dc.CompanyFacetSetDetails on bf.FacetValue equals bfsd.Value
                          where bf.Company == company
                          group bfsd by bfsd.Set into g
                          select new { Set = g.Key, Count = g.Count() };
            var pfCount = await pfCountQuery.ToArrayAsync(cancellationToken);

            var exclQuery = from bf in dc.CompanyFacets
                       join bfsv in dc.CompanyFacetValues on bf.FacetValue equals bfsv.Id
                       join bfsd in dc.CompanyFacetSetDetails on bf.FacetValue equals bfsd.Value
                       where bf.Company == company && bfsd.Exclude
                       group bfsd by bfsd.Set into g
                       select g.Key;
            var excl = await exclQuery.ToArrayAsync(cancellationToken);

            var qQuery = from bfs in dc.CompanyFacetSets
                    join bfc in pfCount on new { Set = bfs.Id, Count = bfs.InclFacets } equals new { bfc.Set, Count = (byte)bfc.Count }
                    where !excl.Contains(bfs.Id)
                    select bfs.Id;
            var sets = await qQuery.ToArrayAsync(cancellationToken);

            var existing = await dc.FacetSetCompanies.Where(fsb => fsb.Company == company).ToArrayAsync(cancellationToken);
            dc.FacetSetCompanies.RemoveRange(existing);

            var newSets = sets.Select(fs => new FacetSetCompany { FacetSet = fs, Company = company });
            dc.FacetSetCompanies.AddRange(newSets);

            await dc.SaveChangesAsync(cancellationToken);
        }
    }
}
"@

New-Item -Path "C:\Bizsort\bizsort-web\backend\Process" -ItemType Directory -Force | Out-Null
Set-Content "C:\Bizsort\bizsort-web\backend\Process\Company.cs" $companyProcess

$grpcService = @"
using Grpc.Core;
using BizSrt.Model.Grpc;
using BizSrt.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BizSrt.Api.Grpc
{
    public class CompanyGrpcService : CompanyService.CompanyServiceBase
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CompanyGrpcService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override async Task<IndexCompanyResponse> IndexCompany(IndexCompanyRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.IndexCompanyAsync(dc, request.CompanyId, context.CancellationToken);

            return new IndexCompanyResponse { Success = true };
        }
    }
}
"@

New-Item -Path "C:\Bizsort\bizsort-web\backend\Grpc" -ItemType Directory -Force | Out-Null
Set-Content "C:\Bizsort\bizsort-web\backend\Grpc\CompanyGrpcService.cs" $grpcService

# Build both projects to verify
cd C:\Bizsort\bizsort-web\backend
dotnet build
if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE }

cd C:\Bizsort\bizsort-web\background
dotnet build
if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE }
