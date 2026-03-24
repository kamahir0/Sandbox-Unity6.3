---
name: unity-development-edit-scene
description: >-
  Use when creating or modifying GameObjects, components, scenes, or prefabs in Unity. 
metadata:
  version: "1.3.0"
---

# UniCli — Edit Scene

All commands: `unicli exec <Command> [options] --json`

## コマンド一覧

**GameObject**
`Create` `CreatePrimitive(primitiveType)` `Destroy` `Duplicate` `Find(namePattern)`
`GetComponents` `GetHierarchy` `Rename` `SetActive(active)` `SetParent(parentPath)`
`SetTransform(position/rotation/scale as JSON)`

**Component**
`GameObject.AddComponent(typeName)` `GameObject.RemoveComponent(componentInstanceId)`
`Component.SetProperty(componentInstanceId, propertyPath, value)`

**Scene**
`Open(path)` `Close(name)` `New` `Save` `List` `GetActive` `SetActive(name)`

**Prefab**
`Instantiate(path)` `Apply` `Save(path)` `Unpack` `GetStatus`

**Selection**
`SetGameObject(path)` `SetGameObjects(paths as JSON array)`

すべての操作対象は `--instanceId` で指定。パスはUnityプロジェクトルートからの相対パス。

## 注意事項

- **`Component.SetProperty` の `--componentInstanceId`** は GameObject の instanceId とは別。必ず `GameObject.GetComponents` で取得したコンポーネントの instanceId を使え。
- **`Prefab.Apply` 後**: `AssetDatabase.Import --path "<prefabPath>"` を必ず実行せよ。