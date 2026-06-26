import os
import re
from collections import defaultdict

legacy_dir = r"C:\Bizsort\legacy\website\wwwroot"
modern_dir = r"C:\Bizsort\bizsort-web\frontend"
tracker_path = r"C:\Bizsort\bizsort-web\.agents\LEGACY_FRONTEND_TRACKER.md"

# Categories
# 1. Views (_routes or pages)
# 2. Components (component/)
# 3. Models and ViewModels (src/model, src/viewmodel)
# 4. Client-side Caches (src/state or src/cache)
# 5. Service and Session Helpers (src/service, src/session)

known_ports = {
    "src/model.ts": ("frontend/src/model.ts", "Ported core models."),
    "src/model/foundation.ts": ("frontend/src/model/foundation.ts", "Ported foundational models."),
    "src/exception.ts": ("frontend/src/exception.ts", "Ported exception models."),
    "src/settings.ts": ("frontend/src/settings.ts", "Ported settings."),
    "src/viewmodel.ts": ("frontend/src/viewmodel.ts", "Ported base viewmodels without jQuery."),
    "src/service/company.ts": ("frontend/src/service/company.ts", "Ported company service helpers."),
    "src/service/category.ts": ("frontend/src/service/category.ts", "Ported category service helpers."),
    "src/service/location.ts": ("frontend/src/service/location.ts", "Ported location service helpers."),
    "src/viewmodel/search/category/input.ts": ("frontend/src/viewmodel/search/category/input.ts", "Ported search category viewmodel."),
    "src/viewmodel/location/input.ts": ("frontend/src/viewmodel/location/input.ts", "Ported search location viewmodel."),
    "component/search/category/input.ts": ("frontend/src/components/search/category/input.ts", "Ported category input lit component. Converted to WebAwesome."),
    "component/search/location/input.ts": ("frontend/src/components/search/location/input.ts", "Ported location input lit component. Converted to WebAwesome."),
    "component/search/home.ts": ("frontend/src/components/search/home.ts", "Ported search home UI logic.")
}

categories = {
    "Views / Pages": [],
    "Components": [],
    "Models & ViewModels": [],
    "Client-side Caches": [],
    "Service & Session Helpers": [],
    "Other / Global": []
}

class_pattern = re.compile(r'^\s*(?:export\s+)?(?:class|interface|type)\s+([A-Za-z0-9_]+)', re.MULTILINE)
element_pattern = re.compile(r'@customElement\(\'([^\']+)\'\)')

def categorize(rel_path):
    rel_path_fwd = rel_path.replace('\\', '/')
    if 'page/' in rel_path_fwd or 'views/' in rel_path_fwd or '_routes' in rel_path_fwd:
        return "Views / Pages"
    if rel_path_fwd.startswith('component/'):
        return "Components"
    if rel_path_fwd.startswith('src/model') or rel_path_fwd.startswith('src/viewmodel'):
        return "Models & ViewModels"
    if rel_path_fwd.startswith('src/state') or rel_path_fwd.startswith('src/cache'):
        return "Client-side Caches"
    if rel_path_fwd.startswith('src/service') or rel_path_fwd.startswith('src/session'):
        return "Service & Session Helpers"
    return "Other / Global"

file_map = defaultdict(list)

for dirpath, _, filenames in os.walk(legacy_dir):
    for f in filenames:
        if f.endswith('.ts') and not f.endswith('.d.ts'):
            filepath = os.path.join(dirpath, f)
            rel_path = os.path.relpath(filepath, legacy_dir).replace('\\', '/')
            
            with open(filepath, 'r', encoding='utf-8', errors='ignore') as file:
                content = file.read()
                
            classes = class_pattern.findall(content)
            elements = element_pattern.findall(content)
            
            cat = categorize(rel_path)
            
            file_info = {
                "path": rel_path,
                "classes": classes,
                "elements": elements
            }
            categories[cat].append(file_info)

with open(tracker_path, "w", encoding="utf-8") as f:
    f.write("# Exhaustive Legacy Frontend Porting Tracker\n\n")
    f.write("| Status | Legacy Item | Modern Equivalent | Migration Notes / Description |\n")
    f.write("|---|---|---|---|\n")

    for cat_name, items in categories.items():
        if not items: continue
        f.write(f"| | **{cat_name}** | | |\n")
        
        # Sort items by path
        items.sort(key=lambda x: x['path'])
        
        for item in items:
            status = "[ ]"
            equiv = "-"
            desc = "-"
            
            if item['path'] in known_ports:
                status = "[x]"
                equiv = f"`{known_ports[item['path']][0]}`"
                desc = known_ports[item['path']][1]
            
            f.write(f"| {status} | ↳ `{item['path']}` | {equiv} | {desc} |\n")
            
            # Write custom elements
            for el in set(item['elements']):
                f.write(f"| | &nbsp;&nbsp;&nbsp;&nbsp;↳ `<{el}>` | | |\n")
            
            # Write classes
            for cls in set(item['classes']):
                f.write(f"| | &nbsp;&nbsp;&nbsp;&nbsp;↳ `class {cls}` | | |\n")

print("Generated rich LEGACY_FRONTEND_TRACKER.md")
