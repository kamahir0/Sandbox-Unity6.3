---
name: unity-cs-post-process
description: |
  Run this skill at the end of any task that created or edited one or more .cs files.
  Trigger condition: any write operation (create_file, str_replace, bash) targeting a .cs path during the task.
  Action: run AssetDatabase.Import then Compile via unicli.
  Keywords: .cs, C#, Unity, compile, import, asset
---

# Unity C# Post-Process

## Trigger

Run this skill at task completion **if any `.cs` file was created or edited** during the task.

## Steps

**1. Import**
```bash
unicli exec AssetDatabase.Import --json
```
- On error → report to user, ask whether to proceed

**2. Compile**
```bash
unicli exec Compile --json
```
- On error → report error details to user
- On success → report completion

## Error handling

If `errors` in the JSON output is non-empty, treat as failure and surface the messages.
If `unicli` is not found, report it to the user.