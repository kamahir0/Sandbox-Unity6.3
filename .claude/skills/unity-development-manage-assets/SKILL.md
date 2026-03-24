---
name: unity-development-manage-assets
description: >-
  Use when importing or deleting assets, creating materials or Assembly Definitions, compiling C# code, or running EditMode/PlayMode tests.
metadata:
  version: "1.2.0"
---

# UniCli — Manage Assets

All commands: `unicli exec <Command> [options] --json`

## AssetDatabase
`Import(path)` `Delete(path)` `Find(filter)` `GetPath(guid)`
> Filter syntax: `t:TypeName`, `l:Label`
> ⚠️ Move/Rename has no dedicated command — use `Eval` with `AssetDatabase.MoveAsset(src, dst)`.

## Material
`Create(assetPath, shader)` `Inspect(assetPath)`
`GetColor/SetColor(assetPath, propertyName, r, g, b, a)`
`GetFloat/SetFloat(assetPath, propertyName, value)`

## Prefab
`Save(instanceId, assetPath)` `Instantiate(assetPath)` `Apply(instanceId)`
`Unpack(instanceId)` `GetStatus(instanceId)`

## AnimatorController
`Create(assetPath)` `Inspect(assetPath)`
`AddParameter/RemoveParameter(assetPath, name, type)`
`AddState(assetPath, layerIndex, stateName)`
`AddTransition(assetPath, layerIndex, fromState, toState)`
`AddTransitionCondition(assetPath, layerIndex, fromState, toState, parameter, mode, threshold)`

## Animator ⚠️ PlayMode only
`Inspect(instanceId)` `SetController(instanceId, assetPath)`
`Play(instanceId, stateName)` `CrossFade(instanceId, stateName, transitionDuration)`
`SetParameter(instanceId, name, value)`

## AssemblyDefinition
`Create(name, directory, includePlatforms)` `Get(name)` `List`
`AddReference/RemoveReference(name, reference)`
> After changes: run `AssetDatabase.Import` → `Compile`

## Compile / TestRunner
`Compile` — run after any C# edit, verify 0 errors
`TestRunner.RunEditMode(resultFilter, stackTraceLines?)`
`TestRunner.RunPlayMode(resultFilter, stackTraceLines?)`
> resultFilter: `failures` | `none`

## PackageManager
`List` `GetInfo(name)` `Search(query)`
`Add(identifier)` `Remove(name)` `Update(name)`

## NuGet
`List` `Install(id, version)` `Uninstall(id)` `Restore`
`ListSources` `AddSource(name, url)` `RemoveSource(name)`