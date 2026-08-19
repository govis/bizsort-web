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

        public override async Task<IndexOfferingResponse> IndexOffering(IndexOfferingRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.IndexOfferingAsync(dc, request.OfferingId, context.CancellationToken);

            return new IndexOfferingResponse { Success = true };
        }

        public override async Task<IndexOfferingFacetSetResponse> IndexOfferingFacetSet(IndexOfferingFacetSetRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.IndexOfferingFacetSetAsync(dc, request.SetId, context.CancellationToken);

            return new IndexOfferingFacetSetResponse { Success = true };
        }

        public override async Task<DeleteOfferingFacetSetResponse> DeleteOfferingFacetSet(DeleteOfferingFacetSetRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.DeleteOfferingFacetSetAsync(dc, request.SetId, context.CancellationToken);

            return new DeleteOfferingFacetSetResponse { Success = true };
        }
    }
}
