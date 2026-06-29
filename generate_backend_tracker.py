import os
import re
from collections import defaultdict

legacy_dir = r"C:\Bizsort\legacy\server"
# ⚠️  SCAFFOLD ONLY — this script writes to a .scaffold.md file for reference.
# NEVER change tracker_path to point at LEGACY_BACKEND_TRACKER.md directly.
# LEGACY_BACKEND_TRACKER.md is manually curated; running this script would destroy all
# hand-maintained [x] statuses, modern equivalents, and migration notes.
# Workflow: run this → diff against LEGACY_BACKEND_TRACKER.md → cherry-pick new entries manually.
tracker_path = r"C:\Bizsort\bizsort-web\.agents\LEGACY_BACKEND_TRACKER.scaffold.md"

namespace_pattern = re.compile(r'^\s*namespace\s+([A-Za-z0-9_\.]+)', re.MULTILINE)
class_pattern = re.compile(r'^\s*(?:public|internal|protected|private)?\s*(?:static|sealed|abstract|partial)?\s*(?:class|interface|struct|record)\s+([A-Za-z0-9_]+)', re.MULTILINE)
method_pattern = re.compile(r'^\s*public\s+(?:static\s+|virtual\s+|override\s+|async\s+)*[A-Za-z0-9_<>\[\]\s]+\s+([A-Za-z0-9_]+)\s*\(', re.MULTILINE)

# Pre-fill some known modern equivalents for the ones we've ported
known_ports = {
    "Data.Cache": {
        "CompanyProfilesCache": ("BizSrt.Api.Data.Cache.CompanyProfilesCache", "Ported from ReadManyExpirationCache. Reduced payload size. Registered as Singleton."),
        "CategoryCache": ("BizSrt.Api.Data.Cache.CategoryCache", "Ported base dictionary caches."),
        "LocationSearchCache": ("BizSrt.Api.Data.Cache.LocationSearchCache", "Ported successfully."),
        "AreaNamesCache": ("BizSrt.Api.Data.Cache.AreaNamesCache", "Ported successfully."),
        "StreetNamesCache": ("BizSrt.Api.Data.Cache.StreetNamesCache", "Ported successfully.")
    },
    "Data": {
        "FeaturedCompanyCache": ("BizSrt.Api.Data.Cache.Company.FeaturedCompaniesCache", "Ported successfully. Combined with FeaturedCache<T> logic."),
        "FeaturedCache": ("BizSrt.Api.Data.Cache.Company.FeaturedCompaniesCache", "Ported successfully."),
    },
    "Model.List": {
        "DirectorySliceInput": ("BizSrt.Api.Model.List.DirectorySliceInput", "Ported successfully.")
    },
    "Data.Master": {
        "Location": ("BizSrt.Api.Data.Master.Location", "Ported base location logic.")
    },
    "Data.Company": {
        "Profile": ("BizSrt.Api.Data.Company.CompanyService", "Modernized static methods into DI injected Service `CompanyService`."),
    },
    "Service.Company.Profile": {
        "Service": ("BizSrt.Api.Service.Company.CompanyEndpoints", "Migrated MVC Controllers to Minimal API Endpoints. Mapped `/api/company/profile`.")
    },
    "Foundation.Cache": {
        "ReadManyExpirationCache": ("BizSrt.Api.Foundation.Cache.ReadManyExpirationCache", "Ported caching primitives."),
        "ReadOneExpirationCache": ("BizSrt.Api.Foundation.Cache.ReadOneExpirationCache", "Ported caching primitives."),
        "ReadAllExpirationCache": ("BizSrt.Api.Foundation.Cache.ReadAllExpirationCache", "Ported caching primitives.")
    }
}

namespaces = defaultdict(lambda: defaultdict(list))

for dirpath, _, filenames in os.walk(legacy_dir):
    if "obj" in dirpath or "bin" in dirpath:
        continue
    for f in filenames:
        if f.endswith('.cs'):
            filepath = os.path.join(dirpath, f)
            with open(filepath, 'r', encoding='utf-8', errors='ignore') as file:
                content = file.read()
                
            ns_matches = namespace_pattern.findall(content)
            # Default namespace if none found
            ns = ns_matches[0] if ns_matches else "Global"
            
            classes = class_pattern.findall(content)
            
            for cls in classes:
                # find methods for this class (rough approximation by looking at the whole file)
                # in a real parser we'd scope this to the class body, but regex is fine for a quick tracker
                methods = method_pattern.findall(content)
                methods = list(set([m for m in methods if m not in ['get', 'set']]))
                
                namespaces[ns][cls].extend(methods)

with open(tracker_path, "w", encoding="utf-8") as f:
    f.write("# Exhaustive Legacy BACKEND Porting Tracker\n\n")
    f.write("| Status | Legacy Item | Modern Equivalent | Migration Notes / Description |\n")
    f.write("|---|---|---|---|\n")

    for ns in sorted(namespaces.keys()):
        f.write(f"| | **Namespace: `{ns}`** | | |\n")
        
        for cls in sorted(namespaces[ns].keys()):
            status = "[ ]"
            equiv = "-"
            desc = "-"
            
            # Check if ported
            if ns in known_ports and cls in known_ports[ns]:
                status = "[x]"
                equiv = f"`{known_ports[ns][cls][0]}`"
                desc = known_ports[ns][cls][1]
            
            # If the class name matches something we might have ported broadly
            elif cls in ["ReadOneCache", "ReadManyCache", "LegacyCache", "LocationSearch", "AreaNames", "IdName", "LocationRef", "CategoryCache", "LocationCache"]:
                status = "[x]"
                equiv = f"Ported"
                desc = "Ported during foundation migration"

            f.write(f"| {status} | ↳ `class {cls}` | {equiv} | {desc} |\n")
            
            # Write methods
            for method in sorted(set(namespaces[ns][cls])):
                f.write(f"| | &nbsp;&nbsp;&nbsp;&nbsp;↳ `{method}()` | | |\n")

print("Generated LEGACY_BACKEND_TRACKER.scaffold.md — diff against LEGACY_BACKEND_TRACKER.md and cherry-pick new entries manually.")
