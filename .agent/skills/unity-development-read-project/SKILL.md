---
name: unity-development-read-project
description: >-
  Use when reading or inspecting a Unity project without modifying any assets: querying GameObjects, console logs, player connections, remote debug commands, project/editor settings, assets, types, or modules.
metadata:
  version: "1.2.0"
---

# UniCli — Read Project

All commands: `unicli exec <Command> [options] --json`

## コマンド一覧

**Console**
`GetLog` `GetLog(logType)` `GetLog(logType, stackTraceLines)`

**GameObject**
`Find(namePattern)` `GetHierarchy`

**Connection**
`List` `Status` `Connect(id)` `Connect(ip)` `Connect(deviceId)`

**Remote**
`List` `Invoke(command)` `Invoke(command, data as JSON string)`
ビルトインコマンド: `Debug.SystemInfo` `Debug.Stats` `Debug.GetLogs` `Debug.GetHierarchy` `Debug.FindGameObjects` `Debug.GetScenes` `Debug.GetPlayerPref`

**Project / Settings**
`Project.Inspect` `PlayerSettings.Inspect` `EditorSettings.Inspect` `EditorUserBuildSettings.Inspect`

**Search**
`Search(query)` `Search(query, maxResults)` `Search(query, includePackages)`
フィルタ: `t:TypeName`（型）、`l:Label`（ラベル）

**Type**
`Type.List` `Type.List(baseType)` `Type.List(filter)` `Type.Inspect(typeName)`

**Module**
`Module.List` `Module.Enable(name)` `Module.Disable(name)`

## 注意事項

- **Remote** の使用には `UNICLI_REMOTE` シンボル定義 + Development Build + Autoconnect Profiler が必要。
- `Module.Enable` / `Disable` 後、コマンドディスパッチャは即時リロードされる。モジュール名は `unicli commands --json` の `module` フィールドと一致する。