$ErrorActionPreference = "Stop"
$baseDir = "C:\Bizsort\bizsort-web"
cd $baseDir

Write-Host "Moving Dictionary..."
Move-Item "foundation\Cache\Dictionary.cs" "database\Cache\FoundationDictionary.cs" -Force

function Replace-StringInDirectory($dir, $oldString, $newString) {
    Get-ChildItem -Path $dir -Recurse -Filter *.cs | ForEach-Object {
        $content = Get-Content $_.FullName
        if ($content -match [regex]::Escape($oldString)) {
            $content -replace [regex]::Escape($oldString), $newString | Set-Content $_.FullName
        }
    }
}

Write-Host "Replacing Namespaces..."
Replace-StringInDirectory "database" "BizSrt.Foundation.Cache.Dictionary" "BizSrt.Data.Cache.Dictionary"
Replace-StringInDirectory "database" "BizSrt.Foundation.Cache.SecurityProfileDictionary" "BizSrt.Data.Cache.SecurityProfileDictionary"

# Update the namespace inside the moved file itself
$content = Get-Content "database\Cache\FoundationDictionary.cs"
$content = $content -replace "namespace BizSrt.Foundation.Cache", "namespace BizSrt.Data.Cache"
$content = $content -replace "using BizSrt.Data.Entities;", ""
$content = $content -replace "using BizSrt.Data;", ""
Set-Content "database\Cache\FoundationDictionary.cs" $content

Write-Host "Rebuilding Solution..."
dotnet build backend\BizSrt.Api.csproj
