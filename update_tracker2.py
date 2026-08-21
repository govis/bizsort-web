filepath = r'C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.md'
with open(filepath, 'r', encoding='utf8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'class CachedCompanyProfile' in line and '[!!]' in line:
        lines[i] = '| [!!] | \u00a0\u00a0\u00a0\u00a0\u2197 class CachedCompanyProfile | BizSrt.Api.Data.Cache.Company.CachedCompanyProfile | **PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** ServiceType and TransactionType mapped. ImageSize property exists on model but is not mapped in either EF loader query (Metadata blob not selected). |\n'
    if 'class CompanyProfilesCache' in line and '[!!]' in line:
        lines[i] = '| [!!] | \u00a0\u00a0\u00a0\u00a0\u2197 class CompanyProfilesCache | BizSrt.Api.Data.Cache.Company.CompanyProfilesCache | **PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** ServiceType and TransactionType mapped. Cache class exists and functions but does not compute ImageSize from Metadata. See CachedCompanyProfile entry above. |\n'

with open(filepath, 'w', encoding='utf8') as f:
    f.writelines(lines)

print('Done')
