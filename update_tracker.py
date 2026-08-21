filepath = r'C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.md'
with open(filepath, 'r', encoding='utf8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    # Fix FeaturedCompanyCache false-done
    if 'class FeaturedCompanyCache' in line and 'Ported successfully' in line:
        lines[i] = '| [!!] | \u00a0\u00a0\u00a0\u00a0\u2197 class FeaturedCompanyCache | BizSrt.Api.Data.Cache.Company.FeaturedCompaniesCache | **PORTED WITH BUGS (2026-08-21 audit).** Two critical LINQ issues documented as fixed were never applied: (1) .ToArray() before .OrderByDescending().Take(500) - full table fetched into C# memory. (2) Location filter uses join+Distinct() instead of .Any() (EXISTS). Fix both before production. |\n'
    # Fix CachedCompanyProfile false-blank
    if 'class CachedCompanyProfile' in line and '| - |' in line and '[ ]' in line:
        lines[i] = '| [!!] | \u00a0\u00a0\u00a0\u00a0\u2197 class CachedCompanyProfile | BizSrt.Api.Data.Cache.Company.CachedCompanyProfile | **PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** Missing ServiceType (long) and TransactionType (short) properties vs legacy. ImageSize property exists on model but is not mapped in either EF loader query (Metadata blob not selected). |\n'
    # Fix CompanyProfilesCache false-blank
    if 'class CompanyProfilesCache' in line and '| - |' in line and '[ ]' in line:
        lines[i] = '| [!!] | \u00a0\u00a0\u00a0\u00a0\u2197 class CompanyProfilesCache | BizSrt.Api.Data.Cache.Company.CompanyProfilesCache | **PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** Cache class exists and functions but does not map ServiceType, TransactionType, or compute ImageSize from Metadata. See CachedCompanyProfile entry above. |\n'

with open(filepath, 'w', encoding='utf8') as f:
    f.writelines(lines)

print('Done')
