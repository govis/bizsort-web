import os

legacy_dir = r"C:\Bizsort\legacy\server"
modern_dir = r"C:\Bizsort\bizsort-web\backend"

tracker_path = r"C:\Bizsort\bizsort-web\.agents\LEGACY_TRACKER.md"

def get_csharp_files(root_dir):
    files = []
    for dirpath, _, filenames in os.walk(root_dir):
        if "obj" in dirpath or "bin" in dirpath:
            continue
        for f in filenames:
            if f.endswith('.cs'):
                rel_path = os.path.relpath(os.path.join(dirpath, f), root_dir)
                files.append(rel_path.replace("\\", "/"))
    return set(files)

legacy_files = get_csharp_files(legacy_dir)
modern_files = get_csharp_files(modern_dir)

# Try to find a modern equivalent for each legacy file
# Since legacy files might be nested differently, let's just do a rough matching by basename and some path heuristics
ported_files = set()
for lf in legacy_files:
    lf_basename = os.path.basename(lf).lower()
    for mf in modern_files:
        if os.path.basename(mf).lower() == lf_basename or os.path.basename(mf).lower() == lf_basename.replace(".cs", "service.cs") or os.path.basename(mf).lower() == lf_basename.replace(".cs", "cache.cs") or os.path.basename(mf).lower() == lf_basename.replace(".cs", "endpoints.cs"):
            # Check if directory structure somewhat matches
            if lf.split('/')[0] in mf.split('/'):
                ported_files.add(lf)
                break

with open(tracker_path, "w") as f:
    f.write("# Legacy Codebase Porting Tracker\n\n")
    f.write("This file captures the legacy codebase in its entirety and tracks what has been ported so far.\n\n")
    
    dirs = {}
    for lf in sorted(list(legacy_files)):
        dirname = os.path.dirname(lf)
        if dirname not in dirs:
            dirs[dirname] = []
        dirs[dirname].append(lf)
        
    for dirname in sorted(dirs.keys()):
        if not dirname: continue
        f.write(f"## {dirname}\n\n")
        for lf in dirs[dirname]:
            status = "[x]" if lf in ported_files else "[ ]"
            f.write(f"- {status} `{lf}`\n")
        f.write("\n")

print("Generated LEGACY_TRACKER.md")
