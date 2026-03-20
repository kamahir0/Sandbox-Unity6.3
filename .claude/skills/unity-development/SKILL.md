---
name: unity-development
description: >-
  Use for Unity Editor automation through UniCli in projects where `unicli` is
  available: editing files under `Assets/` or `Packages/`, compiling Unity
  code, running EditMode/PlayMode tests, and creating or modifying GameObjects,
  scenes, prefabs, assets, packages, build settings, or project settings.
  Follow required safeguards such as `AssetDatabase.Import` after file changes
  and `Compile` verification after C# edits.
metadata:
  version: "2.0.0"
---

# UniCli — Unity Editor CLI

UniCli lets you interact with Unity Editor directly from the terminal via named pipes. The Editor must be open with `com.yucchiy.unicli-server` installed.

## Prerequisites

```bash
unicli check          # Verify CLI and Editor connection
unicli install        # Install server package if missing
unicli install --update  # Update if version mismatch
```

If connection fails, retry 2–3 times — the Editor may need a moment to start the server.

## Command Selection — Follow This Every Time

**TIER 1 → TIER 2 → TIER 3 (last resort)**

### TIER 1: Specialized Commands (always try first)

```bash
unicli commands --json | grep -i "<keyword>"   # Search for a command
unicli exec <command> --help                   # See parameters
unicli exec <command> [...args] --json         # Execute
```

Always use `--json` when parsing output programmatically. Commands are discovered dynamically — do not rely on memorized lists; the project may have custom commands.

### TIER 2: Chain Multiple Specialized Commands

If no single command covers your goal, combine up to ~4 sequential commands. Example:

```bash
unicli exec GameObject.Find --name "MyObject" --json > /tmp/go.json
GO_ID=$(jq -r '.instanceId' /tmp/go.json)
unicli exec Component.Get --instanceId "$GO_ID" --json
```

### TIER 3: `unicli eval` — Last Resort Only

Use `eval` only when TIER 1 and TIER 2 are both impractical, and the logic is simple enough to debug. `eval` is unstable and recompiles on every execution (high latency). If you find yourself reaching for `eval` repeatedly, create a CommandHandler instead (see "Custom Command Handlers" below).

**Known constraints:**
- Return only simple types (`string`, `int`, `bool`) or a JSON string — anonymous types fail
- Use fully qualified type names, or pass `--declarations 'using My.Namespace;'`
- Add null checks — reference chains fail silently

```bash
# Good: simple return
unicli eval 'return Application.unityVersion;' --json

# Good: multi-line with null check
unicli eval "$(cat <<'EOF'
var go = GameObject.Find("Main Camera");
if (go == null) return "NOT_FOUND";
return go.transform.position.ToString();
EOF
)" --json
```

If `eval` fails twice, fall back to TIER 2 or create a CommandHandler.

## Mandatory Safeguards

1. **After creating/modifying any file under `Assets/` or `Packages/`:**
   ```bash
   unicli exec AssetDatabase.Import --path "<path>" --json
   ```
   Never create `.meta` files manually. Skipping this causes missing references and broken imports.

2. **After modifying C# code:**
   ```bash
   unicli exec Compile --json
   ```

3. **For platform-specific builds:** Use `unicli exec BuildPlayer.Compile --target <platform> --json` to catch platform-specific errors.

## Common Workflows

**Compile and test:**
```bash
unicli exec Compile --json
unicli exec TestRunner.RunEditMode --resultFilter failures --json
unicli exec TestRunner.RunPlayMode --resultFilter failures --json
```
Use `--resultFilter none` for a summary only. Use `--stackTraceLines 3` when diagnosing failures.

**Check console logs:**
```bash
unicli exec ConsoleLog.Get --logType "Warning,Error" --json
```

**Project path (if Unity project is in a subdirectory):**
```bash
export UNICLI_PROJECT=path/to/unity/project
```

**Player connection:**
```bash
unicli exec Connection.List --json
unicli exec Connection.Connect '{"id":-1}' --json
unicli exec Connection.Status --json
```

**Remote debug commands (requires `UNICLI_REMOTE` define + Development Build):**
```bash
unicli exec Remote.List --json
unicli exec Remote.Invoke '{"command":"Debug.Stats"}' --json
```

## Custom Command Handlers

When `eval` would be needed repeatedly, implement a `CommandHandler` instead — it provides type safety, structured I/O, and discoverability via `unicli commands`.

```bash
# 1. Create asmdef
unicli exec AssemblyDefinition.Create \
  --path "Assets/Editor/UniCli/MyProject.UniCli.Editor.asmdef" \
  --name "MyProject.UniCli.Editor" --editorOnly --json
unicli exec AssemblyDefinition.AddReference \
  --path "Assets/Editor/UniCli/MyProject.UniCli.Editor.asmdef" \
  --reference "UniCli.Server.Editor" --json

# 2. Create handler script, then import and compile
unicli exec AssetDatabase.Import --path "Assets/Editor/UniCli" --json
unicli exec Compile --json

# 3. Verify and use
unicli commands --json
unicli exec MyCategory.MyAction --targetName "test" --json
```

**Handler implementation:**
```csharp
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;

namespace MyProject.UniCli.Editor.Handlers
{
    [System.Serializable]
    public class MyRequest { public string targetName = ""; }

    [System.Serializable]
    public class MyResponse { public string result; }

    public sealed class MyCustomHandler : CommandHandler<MyRequest, MyResponse>
    {
        public override string CommandName => "MyCategory.MyAction";
        public override string Description => "Description shown in unicli commands";

        protected override ValueTask<MyResponse> ExecuteAsync(MyRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<MyResponse>(new MyResponse { result = $"Processed {request.targetName}" });
        }
    }
}
```

- Request/Response types must be `[Serializable]` with **public fields** (not properties)
- Use `Unit` as `TRequest` or `TResponse` when no input/output is needed
- Throw `CommandFailedException` on failure
- Constructor parameters are resolved from `ServiceRegistry` for dependency injection
