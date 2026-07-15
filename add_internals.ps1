$xml = @"
  <ItemGroup>
    <InternalsVisibleTo Include="BizSrt.Api" />
    <InternalsVisibleTo Include="BizSrt.Worker" />
  </ItemGroup>
</Project>
"@

$files = @(
    "C:\Bizsort\bizsort-web\database\BizSrt.Data.csproj",
    "C:\Bizsort\bizsort-web\foundation\BizSrt.Foundation.csproj",
    "C:\Bizsort\bizsort-web\model\BizSrt.Model.csproj"
)

foreach ($file in $files) {
    $content = Get-Content $file -Raw
    $content = $content -replace '</Project>', $xml
    Set-Content $file $content
}

dotnet build C:\Bizsort\bizsort-web\backend\BizSrt.Api.csproj
