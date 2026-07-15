$proto = @"
syntax = "proto3";

option csharp_namespace = "BizSrt.Model.Grpc";

package company;

service CompanyService {
  rpc IndexCompany (IndexCompanyRequest) returns (IndexCompanyResponse);
}

message IndexCompanyRequest {
  int32 company_id = 1;
}

message IndexCompanyResponse {
  bool success = 1;
}
"@
New-Item -Path "C:\Bizsort\bizsort-web\model\Protos" -ItemType Directory -Force | Out-Null
Set-Content "C:\Bizsort\bizsort-web\model\Protos\company.proto" $proto

# Update BizSrt.Api.csproj
$apiProj = Get-Content "C:\Bizsort\bizsort-web\backend\BizSrt.Api.csproj" -Raw
$apiProj = $apiProj -replace '</ItemGroup>\s*</Project>', @"
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" Version="2.62.0" />
  </ItemGroup>
  <ItemGroup>
    <Protobuf Include="..\model\Protos\company.proto" GrpcServices="Server" />
  </ItemGroup>
</Project>
"@
Set-Content "C:\Bizsort\bizsort-web\backend\BizSrt.Api.csproj" $apiProj

# Update BizSrt.Worker.csproj
$workerProj = Get-Content "C:\Bizsort\bizsort-web\background\BizSrt.Worker.csproj" -Raw
$workerProj = $workerProj -replace '</ItemGroup>\s*</Project>', @"
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Grpc.Net.Client" Version="2.62.0" />
    <PackageReference Include="Google.Protobuf" Version="3.26.1" />
    <PackageReference Include="Grpc.Tools" Version="2.63.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <Protobuf Include="..\model\Protos\company.proto" GrpcServices="Client" />
  </ItemGroup>
</Project>
"@
Set-Content "C:\Bizsort\bizsort-web\background\BizSrt.Worker.csproj" $workerProj

# Move and rename Worker
New-Item -Path "C:\Bizsort\bizsort-web\background\Workers\Company" -ItemType Directory -Force | Out-Null
Move-Item "C:\Bizsort\bizsort-web\background\Jobs\CompanyIndexerWorker.cs" "C:\Bizsort\bizsort-web\background\Workers\Company\Indexer.cs" -Force

$workerContent = Get-Content "C:\Bizsort\bizsort-web\background\Workers\Company\Indexer.cs" -Raw
$workerContent = $workerContent -replace 'namespace BizSrt.Worker.Jobs', 'namespace BizSrt.Worker.Company'
$workerContent = $workerContent -replace 'public class CompanyIndexerWorker : AsyncQueueWorker<CompanyIndexerWorkItem>', 'public class Indexer : AsyncQueueWorker<CompanyIndexerWorkItem>'
$workerContent = $workerContent -replace 'ILogger<CompanyIndexerWorker>', 'ILogger<Indexer>'
$workerContent = $workerContent -replace 'public CompanyIndexerWorker\(', 'public Indexer('

# Update Worker to call gRPC
$workerContent = $workerContent -replace 'using BizSrt.Model;', "using BizSrt.Model;`nusing BizSrt.Model.Grpc;"
$workerContent = $workerContent -replace 'private readonly IServiceScopeFactory _scopeFactory;', @"
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CompanyService.CompanyServiceClient _grpcClient;
"@
$workerContent = $workerContent -replace 'public Indexer\((.*?)\) : base\(logger\)', 'public Indexer($1, CompanyService.CompanyServiceClient grpcClient) : base(logger)'
$workerContent = $workerContent -replace '            _scopeFactory = scopeFactory;', "            _scopeFactory = scopeFactory;`n            _grpcClient = grpcClient;"

$processAsync = @"
        protected override async Task ProcessAsync(CompanyIndexerWorkItem workItem, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Sending gRPC request to index Company {workItem.CompanyId}...");
                var request = new IndexCompanyRequest { CompanyId = workItem.CompanyId };
                await _grpcClient.IndexCompanyAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Successfully indexed Company {workItem.CompanyId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to index Company {workItem.CompanyId} via gRPC.");
                throw;
            }
            finally
            {
                _pendingItems.TryRemove(workItem.CompanyId, out _);
            }
        }
"@
$workerContent = $workerContent -replace '(?s)protected override async Task ProcessAsync.*?\}', $processAsync
Set-Content "C:\Bizsort\bizsort-web\background\Workers\Company\Indexer.cs" $workerContent

# Update background Program.cs
$progWorker = Get-Content "C:\Bizsort\bizsort-web\background\Program.cs" -Raw
$progWorker = $progWorker -replace 'using BizSrt.Worker.Jobs;', 'using BizSrt.Worker.Company;'
$progWorker = $progWorker -replace 'builder.Services.AddHostedService<CompanyIndexerWorker>\(\);', @"
builder.Services.AddGrpcClient<BizSrt.Model.Grpc.CompanyService.CompanyServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5000");
});
builder.Services.AddHostedService<Indexer>();
"@
Set-Content "C:\Bizsort\bizsort-web\background\Program.cs" $progWorker

# Update backend Program.cs
$progApi = Get-Content "C:\Bizsort\bizsort-web\backend\Program.cs" -Raw
$progApi = $progApi -replace 'var builder = WebApplication.CreateBuilder\(args\);', "var builder = WebApplication.CreateBuilder(args);`nbuilder.Services.AddGrpc();"
$progApi = $progApi -replace 'app.Run\(\);', "app.MapGrpcService<BizSrt.Api.Grpc.CompanyGrpcService>();`napp.Run();"
Set-Content "C:\Bizsort\bizsort-web\backend\Program.cs" $progApi
