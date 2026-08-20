import os, re

service_dir = r'C:\Bizsort\bizsort-web\frontend-astro\src\service'

for file in os.listdir(service_dir):
    if file.endswith('.ts') and file not in ['api.ts', 'image.ts', 'proxy.ts']:
        path = os.path.join(service_dir, file)
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # 1. Remove API_BASE definition
        content = re.sub(r'const API_BASE =.*?;', '', content, flags=re.DOTALL)
        
        # 2. Add import
        if 'apiFetch' not in content:
            content = "import { apiFetch } from './api.js';\n" + content
            
        # 3. Replace fetch calls
        content = re.sub(r'await fetch\(`\$\{API_BASE\}(.*?)`\)', r'await apiFetch(`\1`)', content)
        content = re.sub(r'await fetch\(`\$\{API_BASE\}(.*?)`, (.*?)\)', r'await apiFetch(`\1`, \2)', content)
        
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
