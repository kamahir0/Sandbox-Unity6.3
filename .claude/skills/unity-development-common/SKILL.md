---
name: unity-development-common
description: >-
  Common rules to refer to in all tasks involving the Unity Editor. Always refer to these rules when using unicli, modifying files, or compiling.
---

# UniCli — Unity Editor CLI (Common)

UniCli は named pipe 経由で Unity Editor を操作する。
Editor が開いており `com.yucchiy.unicli-server` がインストール済みであること。
初回セットアップや接続トラブルは `unicli check` / `unicli install` を実行する。

---

## ⚠️ 作業開始前：専門スキルを先に呼ぶ（必須）

| やりたいこと | 呼ぶべきスキル |
|---|---|
| シーン・Prefab・GameObject・コンポーネントの作成・変更 | `unity-development-edit-scene` |
| プロジェクト情報の読み取り・確認・ログ取得・型検索 | `unity-development-read-project` |
| アセットのインポート・削除、C#コンパイル、テスト、パッケージ管理 | `unity-development-manage-assets` |
| Source Generator が生成した .cs ファイルの読み取り | `unity-source-generator-reader` |
| `unicli exec` にないカスタムコマンドの追加 | `unicli-create-custom-command` |

> フェーズが変わるたびに対応スキルを呼び直す。

---

## ⛔ `unicli exec Eval` の制限

| ルール | 詳細 |
|---|---|
| シーン・GameObject操作に使わない | `unity-development-edit-scene` の専用コマンドを使う |
| 2回連続失敗で即停止 | 同じアプローチでの3回目試行は禁止。スキル再確認またはユーザーに確認 |
| ループ禁止 | 微修正を繰り返さず、方針自体を見直す |

---

## ファイル操作後の必須後処理

| 操作 | 必要なコマンド | 順序 |
|---|---|---|
| `.cs` 新規作成 / 削除 | `AssetDatabase.Import` → `Compile` | この順で |
| `.cs` 編集のみ | `Compile` のみ | — |
| ディレクトリ作成 / 移動 / リネーム（`.cs` 含む） | `AssetDatabase.Import` → `Compile` | この順で |
| AssemblyDefinition 作成 / 変更 | `AssetDatabase.Import` → `Compile` | この順で |
| 上記を複数組み合わせた場合 | `AssetDatabase.Import` → `Compile` | 最後に1回でよい |

```bash
unicli exec AssetDatabase.Import --path "<変更したパス>" --json
unicli exec Compile --json   # エラー 0 件を確認してから完了とする
```

---

## 基本ルール

- 出力をパースする場合は **必ず `--json`** を付ける
- 接続失敗時は 2〜3 回リトライ後、ユーザーに Editor が開いているか確認する
- 使えるコマンドは都度動的に探す（記憶に頼らない）:

```bash
unicli commands --json | grep -i "<キーワード>"
unicli exec <command> --help
```