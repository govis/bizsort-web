filepath = r'C:\Bizsort\bizsort-web\.agents\LEGACY_MIGRATION.md'
with open(filepath, 'r', encoding='utf8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'CachedCompanyProfile — Missing ServiceType and TransactionType' in line and '[ ]' in line:
        lines[i] = lines[i].replace('[ ]', '[x]')
    if 'FeaturedCompaniesCache — Full table scan before sort' in line and '[ ]' in line:
        lines[i] = lines[i].replace('[ ]', '[x]')
    if 'FeaturedCompaniesCache — Location filter uses join+Distinct() instead of ANY' in line and '[ ]' in line:
        lines[i] = lines[i].replace('[ ]', '[x]')

with open(filepath, 'w', encoding='utf8') as f:
    f.writelines(lines)
