filepath = r'C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.md'
with open(filepath, 'r', encoding='utf8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'GetInfoAsync' in line and '[!!]' in line:
        lines[i] = lines[i].replace('[!!]', '[x]').replace('**PORTED WITH BUGS (2026-08-21 audit).** Hits EF Core CompanyProfiles directly instead of resolving via CompanyProfilesCache.', 'Fully ported.')

with open(filepath, 'w', encoding='utf8') as f:
    f.writelines(lines)
