filepath = r'C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.md'
with open(filepath, 'r', encoding='utf8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'class CachedCompanyProfile' in line and '[!!]' in line:
        lines[i] = lines[i].replace('[!!]', '[x]').replace('**PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** ServiceType and TransactionType mapped. ImageSize property exists on model but is not mapped in either EF loader query (Metadata blob not selected).', 'Fully ported and mapped.')
    if 'class CompanyProfilesCache' in line and '[!!]' in line:
        lines[i] = lines[i].replace('[!!]', '[x]').replace('**PARTIALLY PORTED WITH GAPS (2026-08-21 audit).** ServiceType and TransactionType mapped. Cache class exists and functions but does not compute ImageSize from Metadata. See CachedCompanyProfile entry above.', 'Fully ported and mapped.')

with open(filepath, 'w', encoding='utf8') as f:
    f.writelines(lines)
