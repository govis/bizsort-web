import re

files = [
    r'C:\Bizsort\bizsort-web\frontend\src\components\search\category\input.ts',
    r'C:\Bizsort\bizsort-web\frontend\src\components\search\location\input.ts'
]

for filepath in files:
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    content = re.sub(r'(@property\(\{.*?\}\)\s+)(?:private\s+)?([\w_]+)!:', r'\1declare \2:', content)
    content = re.sub(r'(@state\(\)\s+)(private\s+)?([\w_]+)!:', r'\1declare \3:', content)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
