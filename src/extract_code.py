import os
import re

docs_dir = r"c:\Users\johnl\source\repos\lofcz\LLMTornado\src\LlmTornado.Cli.Blazor\docs"
src_dir = r"c:\Users\johnl\source\repos\lofcz\LLMTornado\src"

for stage_file in os.listdir(docs_dir):
    if not stage_file.startswith("stage-") or not stage_file.endswith(".md"):
        continue

    file_path = os.path.join(docs_dir, stage_file)
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    # Pattern to match: **Path:** `path/to/file` followed by ```cs or ```csharp or ```razor or ```css
    # and then the code block.
    # regex matches: \*\*Path:\*\*\s*`([^`]+)`\s*```([a-z]+)\n(.*?)```
    matches = re.finditer(r"\*\*Path:\*\*\s*`([^`]+)`[\s\S]*?```([a-z]+)\n(.*?)```", content, re.DOTALL)
    for m in matches:
        rel_path = m.group(1).strip()
        code = m.group(3)
        
        # sometimes path contains "src/LlmTornado.Cli.Blazor/..."
        if rel_path.startswith("src/"):
            rel_path = rel_path[4:]
            
        full_path = os.path.join(src_dir, rel_path)
        
        # In case the doc path doesn't start with src/
        if not full_path.startswith(src_dir):
            full_path = os.path.join(src_dir, rel_path)
            
        os.makedirs(os.path.dirname(full_path), exist_ok=True)
        with open(full_path, "w", encoding="utf-8") as out_f:
            out_f.write(code)
        
        print(f"Created {full_path}")
