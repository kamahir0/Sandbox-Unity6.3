---
name: unicli-create-custom-command
description: >-
  Use when creating a new UniCli custom CommandHandler in a Unity project.Trigger on requests about adding UniCli commands, implementing CommandHandler, calling custom commands via "unicli exec", or setting up AssemblyDefinition for UniCli. Also triggers on Japanese phrases like "コマンドを追加", "カスタムコマンド", "CommandHandler を実装".
  Covers full workflow: AssemblyDefinition setup → handler implementation → registration → smoke test.
---

# UniCli — カスタムコマンド作成

## 前提確認

```bash
unicli check --json   # サーバー接続確認
unicli commands --json | grep -i "<候補キーワード>"  # 既存コマンドと重複がないか確認
```

接続失敗時は 2〜3 回リトライ後、ユーザーに Editor が開いているか確認する。

---

## Step 1 — AssemblyDefinition のセットアップ

プロジェクト固有の UniCli 用 asmdef がまだなければ作成する。
**既に存在する場合はスキップ**（`AssemblyDefinition.Create` は重複時にエラーになる）。

```bash
# 存在確認
ls Assets/Editor/UniCli/*.asmdef 2>/dev/null && echo "EXISTS" || echo "NOT FOUND"

# 存在しない場合のみ実行
unicli exec AssemblyDefinition.Create \
  --name "MyProject.UniCli.Editor" \
  --directory "Assets/Editor/UniCli" \
  --includePlatforms Editor --json

unicli exec AssemblyDefinition.AddReference \
  --name "MyProject.UniCli.Editor" \
  --reference "UniCli.Server.Editor" --json

unicli exec AssetDatabase.Import --path "Assets/Editor/UniCli" --json
unicli exec Compile --json
```

---

## Step 2 — CommandHandler の実装

ファイル: `Assets/Editor/UniCli/Handlers/<Category><Action>Handler.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;

namespace MyProject.UniCli.Editor.Handlers
{
    [System.Serializable]
    public class MyRequest
    {
        public string targetName = "";   // フィールド必須（プロパティ不可）
    }

    [System.Serializable]
    public class MyResponse
    {
        public string result;
    }

    public sealed class MyHandler : CommandHandler<MyRequest, MyResponse>
    {
        public override string CommandName => "MyCategory.MyAction";
        public override string Description => "unicli commands に表示される説明";

        protected override ValueTask<MyResponse> ExecuteAsync(
            MyRequest request, CancellationToken cancellationToken)
        {
            // 失敗時は throw new CommandFailedException("reason");
            return new ValueTask<MyResponse>(
                new MyResponse { result = $"Processed {request.targetName}" });
        }
    }
}
```

### 実装ルール（必須）

| 項目 | ルール |
|---|---|
| Request/Response | `[Serializable]` + **public フィールド**（`JsonUtility` 制約） |
| 失敗通知 | `throw new CommandFailedException("reason")` |
| DI | コンストラクタ引数は `ServiceRegistry` から解決される |
| CommandName | `"Category.Action"` 形式で既存コマンドと重複しないこと |

---

## Step 3 — インポート & コンパイル

`.cs` ファイルを新規作成したため、必ずこの順で実行する。

```bash
unicli exec AssetDatabase.Import --path "Assets/Editor/UniCli/Handlers" --json
unicli exec Compile --json
```

`Compile` の出力に **エラーが 0 件**であることを確認してから次へ進む。

---

## Step 4 — 動作確認

```bash
# コマンドが登録されたか確認
unicli commands --json | grep -i "MyCategory"

# 実行テスト
unicli exec MyCategory.MyAction --targetName "TestObject" --json
```

`unicli commands` に新コマンドが表示され、`exec` が期待通りの JSON を返せば完了。