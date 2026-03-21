---
name: unity-development-common
description: >-
  Use for UniCli basics that apply to all Unity Editor automation: verifying
  or installing the UniCli server (unicli check, unicli install), setting the
  project path (UNICLI_PROJECT), discovering and executing commands
  (unicli commands, unicli exec), and implementing custom CommandHandlers
  when built-in commands are insufficient.
metadata:
  version: "1.0.0"
---

# UniCli — Unity Editor CLI (Common)

UniCli lets you interact with Unity Editor directly from the terminal via named pipes. The Editor must be open with `com.yucchiy.unicli-server` installed.

## RULES — Always Follow These

- **Always use `--json`** when parsing output programmatically.
- **If connection to Unity Editor fails**: Retry 2–3 times, then ask the user to confirm Unity Editor is running with the project open.
- **Discover commands dynamically**: Use `unicli commands --json` to list all available commands and `unicli exec <command> --help` to see parameters. Do not rely on memorized command lists — the project may have custom commands.

## Prerequisites

Before running commands, verify that the CLI is installed and the Editor is reachable:

```bash
unicli check
```

If `unicli check` reports that the server package is not installed, run `unicli install` to install it:

```bash
unicli install
```

If the server package version does not match the CLI version, run `unicli install --update` to update it:

```bash
unicli install --update
```

## Project Path

By default, `unicli` looks for a Unity project in the current working directory. If the Unity project is in a subdirectory, set the `UNICLI_PROJECT` environment variable:

```bash
export UNICLI_PROJECT=path/to/unity/project
```

Or prefix each command:

```bash
UNICLI_PROJECT=path/to/unity/project unicli exec Compile --json
```

## Command Selection — Follow This Every Time

**TIER 1 → TIER 2**

### TIER 1: Specialized Commands (always try first)

```bash
unicli commands --json | grep -i "<keyword>"   # Search for a command
unicli exec <command> --help                   # See parameters
unicli exec <command> [...args] --json         # Execute
```

### TIER 2: Chain Multiple Specialized Commands

If no single command covers your goal, combine up to ~4 sequential commands. Example:

```bash
unicli exec GameObject.Find --namePattern "MyObject" --json > /tmp/go.json
GO_ID=$(jq -r '.results[0].instanceId' /tmp/go.json)
unicli exec GameObject.GetComponents --instanceId "$GO_ID" --json
```

## Executing Commands

Run commands with `unicli exec <command>`. Pass parameters as `--key value` flags:

```bash
unicli exec GameObject.Find --namePattern "Main Camera" --json
```

Boolean flags can be passed without a value:

```bash
unicli exec GameObject.Find --includeInactive --json
```

Array parameters can be passed by repeating the same flag:

```bash
unicli exec BuildPlayer.Build --options Development --options ConnectWithProfiler --json
```

### Common options

- `--json` — Output in JSON format (recommended for structured processing)
- `--timeout <ms>` — Set command timeout in milliseconds
- `--no-focus` — Don't bring Unity Editor to front
- `--help` — Show command parameters and nested type details

## Custom Command Handlers

When TIER 1 and TIER 2 are insufficient, implement a `CommandHandler` — it provides type safety, structured I/O, and discoverability via `unicli commands`.

```bash
# 1. Create asmdef
unicli exec AssemblyDefinition.Create \
  --name "MyProject.UniCli.Editor" \
  --directory "Assets/Editor/UniCli" \
  --includePlatforms Editor --json
unicli exec AssemblyDefinition.AddReference \
  --name "MyProject.UniCli.Editor" \
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

- Request/Response types must be `[Serializable]` with **public fields** (not properties) — required by `JsonUtility`
- Use `Unit` as `TRequest` or `TResponse` when no input/output is needed
- Throw `CommandFailedException` on failure
- Constructor parameters are resolved from `ServiceRegistry` for dependency injection
