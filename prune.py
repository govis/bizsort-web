import re
import os
from collections import defaultdict

with open('frontend-astro/tsc_output.txt', encoding='utf-16') as f:
    lines = f.readlines()

errors_by_file = defaultdict(list)

for line in lines:
    match = re.match(r'(.+?)\((\d+),(\d+)\): error (TS\d+): (.+)', line)
    if not match:
        continue
    filepath = os.path.join('frontend-astro', match.group(1).replace('/', os.sep))
    line_num = int(match.group(2)) - 1
    msg = match.group(5)
    errors_by_file[filepath].append((line_num, msg))

for filepath, errors in errors_by_file.items():
    if not os.path.exists(filepath):
        continue
        
    with open(filepath, 'r', encoding='utf-8') as f:
        file_lines = f.readlines()
        
    # Sort descending by line number to avoid shifting issues when deleting lines
    errors.sort(key=lambda x: x[0], reverse=True)
    
    for line_num, msg in errors:
        if line_num >= len(file_lines):
            continue
            
        target_line = file_lines[line_num]
        
        if "All imports in import declaration are unused" in msg:
            file_lines[line_num] = ''
            continue
            
        if "is declared but never used" in msg or "is declared but its value is never read" in msg:
            var_name_match = re.search(r"'([^']+)'", msg)
            if var_name_match:
                var_name = var_name_match.group(1)
                
                # If it's an import line
                if target_line.strip().startswith('import'):
                    new_line = re.sub(r'\b' + var_name + r'\b\s*,?\s*', '', target_line)
                    new_line = new_line.replace('{ ,', '{ ').replace(', }', ' }').replace('{ }', '')
                    # If empty import left
                    if new_line.strip().startswith('import') and 'from' in new_line and not re.search(r'\{.*\}', new_line) and not re.search(r'\w', new_line.split('from')[0].replace('import','')):
                        file_lines[line_num] = ''
                    elif new_line.strip() == "import {  } from '';" or new_line.strip() == "import '';":
                        file_lines[line_num] = ''
                    else:
                        file_lines[line_num] = new_line
                
                # If it's a parameter in a function or method (e.g. not an import, not a class property)
                elif "export " not in target_line and "=" not in target_line and "declare " not in target_line:
                    # Prefix with _
                    # Only replace exact word match
                    file_lines[line_num] = re.sub(r'\b' + var_name + r'\b', '_' + var_name, target_line, count=1)
                    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.writelines(file_lines)

print("Done pruning!")
