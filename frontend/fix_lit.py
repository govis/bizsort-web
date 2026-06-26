import re

def fix_lit_properties(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find the constructor
    constructor_match = re.search(r'constructor\(\) \{([^}]+)\}', content)
    if not constructor_match:
        return
        
    constructor_body = constructor_match.group(1)
    
    # Extract properties with initializers
    props = re.findall(r'(@property\(\{.*?\}\)\s+|@state\(\)\s+)(private\s+)?([\w_]+)(\??)(?::\s*([^=;]+))?\s*=\s*([^;]+);', content)
    
    new_initializations = []
    
    for prop in props:
        decorator = prop[0]
        is_private = prop[1] or ''
        name = prop[2]
        optional = prop[3]
        type_hint = prop[4]
        value = prop[5].strip()
        
        # If there's no explicit type, we can infer some basic ones or just use `any` 
        if not type_hint:
            if value in ['true', 'false']:
                type_hint = 'boolean'
            elif value.startswith(('\'', '\"', '`')):
                type_hint = 'string'
            elif value.isdigit():
                type_hint = 'number'
            else:
                type_hint = 'any'
                
        # Rewrite the declaration
        old_decl = re.compile(re.escape(decorator + is_private + name + optional) + r'(?:\:\s*' + re.escape(prop[4]) + r')?\s*=\s*' + re.escape(prop[5]) + r';')
        new_decl = f'{decorator.strip()}\n    {is_private}{name}!: {type_hint.strip()};'
        content = old_decl.sub(new_decl, content, count=1)
        
        new_initializations.append(f'        this.{name} = {value};')
        
    # Inject into constructor
    if new_initializations:
        new_constructor_body = constructor_body + '\n' + '\n'.join(new_initializations) + '\n    '
        content = content.replace('constructor() {' + constructor_body + '}', 'constructor() {' + new_constructor_body + '}')
        
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

fix_lit_properties(r'C:\Bizsort\bizsort-web\frontend\src\components\search\category\input.ts')
fix_lit_properties(r'C:\Bizsort\bizsort-web\frontend\src\components\search\location\input.ts')
print('Fixed lit properties')
