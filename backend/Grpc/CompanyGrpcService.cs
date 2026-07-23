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

        public override async Task<IndexProductResponse> IndexProduct(IndexProductRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.IndexProductAsync(dc, request.ProductId, context.CancellationToken);

            return new IndexProductResponse { Success = true };
        }

        public override async Task<IndexProductFacetSetResponse> IndexProductFacetSet(IndexProductFacetSetRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.IndexProductFacetSetAsync(dc, request.SetId, context.CancellationToken);

            return new IndexProductFacetSetResponse { Success = true };
        }

        public override async Task<DeleteProductFacetSetResponse> DeleteProductFacetSet(DeleteProductFacetSetRequest request, ServerCallContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var dc = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await Process.Company.DeleteProductFacetSetAsync(dc, request.SetId, context.CancellationToken);

            return new DeleteProductFacetSetResponse { Success = true };
        }
    }
}
