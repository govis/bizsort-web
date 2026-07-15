$ErrorActionPreference = "Stop"
$baseDir = "C:\Bizsort\bizsort-web"
cd $baseDir

Write-Host "Creating Projects..."
dotnet new classlib -n BizSrt.Model -o model
dotnet new classlib -n BizSrt.Foundation -o foundation
dotnet new classlib -n BizSrt.Data -o database
dotnet new worker -n BizSrt.Worker -o background

Write-Host "Configuring Dependencies..."
cd foundation
dotnet add reference ../model/BizSrt.Model.csproj
cd ../database
dotnet add reference ../model/BizSrt.Model.csproj
dotnet add reference ../foundation/BizSrt.Foundation.csproj
dotnet add package Microsoft.EntityFrameworkCore.SqlServer -v 10.0.*
dotnet add package Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite -v 10.0.*
cd ../background
dotnet add reference ../database/BizSrt.Data.csproj
cd ../backend
dotnet add reference ../database/BizSrt.Data.csproj
dotnet add reference ../foundation/BizSrt.Foundation.csproj
dotnet add reference ../model/BizSrt.Model.csproj
cd $baseDir

Write-Host "Moving Files..."
Remove-Item -Path "model\Class1.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "foundation\Class1.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "database\Class1.cs" -Force -ErrorAction SilentlyContinue

Copy-Item -Path "backend\Model\*" -Destination "model\" -Recurse -Force
Copy-Item -Path "backend\Foundation\*" -Destination "foundation\" -Recurse -Force
Copy-Item -Path "backend\Data\*" -Destination "database\" -Recurse -Force

Remove-Item -Path "backend\Model" -Recurse -Force
Remove-Item -Path "backend\Foundation" -Recurse -Force
Remove-Item -Path "backend\Data" -Recurse -Force

Write-Host "Performing Namespace Replacements..."
function Replace-StringInDirectory($dir, $oldString, $newString) {
    Get-ChildItem -Path $dir -Recurse -Filter *.cs | ForEach-Object {
        $content = Get-Content $_.FullName
        if ($content -match [regex]::Escape($oldString)) {
            $content -replace [regex]::Escape($oldString), $newString | Set-Content $_.FullName
        }
    }
}

Replace-StringInDirectory $baseDir "BizSrt.Api.Model" "BizSrt.Model"
Replace-StringInDirectory $baseDir "BizSrt.Api.Foundation" "BizSrt.Foundation"
Replace-StringInDirectory $baseDir "BizSrt.Api.Data" "BizSrt.Data"

Write-Host "Done!"
