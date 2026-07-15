$file = "C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.md"
$content = Get-Content $file -Raw
$content = $content -replace '\| \[ \] \| (.*?) `class Dictionary` \| - \| - \|', '| [x] | $1 `class Dictionary` | BizSrt.Data.Master.Dictionary | Ported |'
$content = $content -replace '\| \[ \] \| (.*?) `class DictionaryCache` \| - \| - \|', '| [x] | $1 `class DictionaryCache` | BizSrt.Data.Cache.DictionaryCache | Ported |'
$content = $content -replace '\| \[ \] \| (.*?) `class DictionaryItem` \| - \| - \|', '| [x] | $1 `class DictionaryItem` | BizSrt.Model.DictionaryItem | Ported |'
Set-Content $file $content

$migFile = "C:\Bizsort\bizsort-web\.agents\LEGACY_MIGRATION.md"
$migContent = Get-Content $migFile -Raw

$newArch = @"
## Modern Architecture Overview

The new modern architecture breaks the monolith into standard .NET 10 libraries:
- **`BizSrt.Model`**: POCO DTOs, Enums, and ViewModels (Zero dependencies).
- **`BizSrt.Foundation`**: Abstract caching layers, utilities, text conversion (Depends on Model).
- **`BizSrt.Data`**: EF Core 10 DataContext, Entities, and concrete memory Caches (Depends on Foundation).
- **`BizSrt.Worker`**: Separate BackgroundService process for heavy lifting like `IndexCompany` via gRPC (Depends on Data).
- **`BizSrt.Api`**: The frontend-facing REST API and orchestrating services (Depends on Data).

"@

$migContent = $migContent -replace '## Legacy Architecture Overview', "$newArch`n`n## Legacy Architecture Overview"

$newProgress = @"
- [x] **Project Structure & Libraries:** Refactored the monolith into `BizSrt.Model`, `BizSrt.Foundation`, `BizSrt.Data`, `BizSrt.Worker`, and `BizSrt.Api`. Handled circular dependencies and enforced `InternalsVisibleTo`.
- [x] **Background Worker (Indexer):** Scaffolded `BizSrt.Worker` project. Mapped legacy `google.protobuf` and gRPC implementation plan to rebuild the `IndexCompany` polling logic.
- [x] **Dictionary Caches:** Ported `DictionaryItem`, `DictionaryType`, `DictionaryCache`, and integrated into `LegacyCache`.
"@

$migContent = $migContent -replace '### 1. Backend Services & Data', "### 1. Backend Services & Data`n`n$newProgress"

Set-Content $migFile $migContent
