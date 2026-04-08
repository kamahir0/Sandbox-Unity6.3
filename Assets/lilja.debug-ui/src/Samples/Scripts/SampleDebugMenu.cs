using System;
using System.Collections.Generic;
using Lilja.DebugUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// デバッグメニューのセットアップ例。
/// Bootstrap 用 GameObject にアタッチして Start() で初期化する。
/// DebugMenuOpenButton コンポーネントと組み合わせて使用する。
/// </summary>
public class SampleDebugMenu : MonoBehaviour
{
    private void Start()
    {
        DebugMenu.Initialize(new RootPage());

        // GetPage<T>() のデモ: 初期化後に外部からページへ動的追加
        var root = DebugMenu.GetPage<RootPage>();
        root?.AddDebugUI(b =>
        {
            b.VisualElement(new DebugLabel($"起動時刻: {System.DateTime.Now:HH:mm:ss}"));
        });
    }

    // ─── Root ─────────────────────────────────────────────────────

    class RootPage : DebugPage
    {
        public override void Configure(IDebugUIBuilder builder)
        {
            var playerIcon = Resources.Load<Sprite>("sample_icon_1");
            builder.NavigationButton("Player", () => new PlayerPage(), new StyleBackground(playerIcon));

            var audioIcon = Resources.Load<Texture2D>("sample_icon_2");
            builder.NavigationButton("Audio", () => new AudioPage(), new StyleBackground(audioIcon));

            var settingsIcon = Resources.Load<VectorImage>("sample_icon_3");
            builder.NavigationButton("Settings", () => new SettingsPage(), new StyleBackground(settingsIcon));

            builder.NavigationButton("Scene", b =>
            {
                var titleBtn = new DebugButton("Title");
                titleBtn.clicked += () => SceneManager.LoadScene("Title");
                b.VisualElement(titleBtn);

                var stage1Btn = new DebugButton("Stage 1");
                stage1Btn.clicked += () => SceneManager.LoadScene("Stage1");
                b.VisualElement(stage1Btn);
            });

            // 動的UIデモページへのナビゲーション
            builder.NavigationButton("Dynamic UI Demo", () => new DynamicDemoPage());

            builder.Foldout("App Info", b =>
            {
                b.VisualElement(new DebugLabel($"Version: {Application.version}"));
                b.VisualElement(new DebugLabel($"Platform: {Application.platform}"));
            });
        }
    }

    // ─── Dynamic UI Demo ──────────────────────────────────────────

    /// <summary>
    /// AddDebugUI / VirtualFoldout / PlaceBehind のデモページ。
    /// </summary>
    class DynamicDemoPage : DebugPage
    {
        private VirtualFoldout _enemyFoldout;
        private readonly List<IDisposable> _enemyHandles = new();
        private int _enemyCounter;
        private IDisposable _placedHandle;

        public override void Configure(IDebugUIBuilder builder)
        {
            // ── VirtualFoldout デモ ──────────────────────────────
            // 子要素が0のとき非表示、1つ以上で表示される Foldout
            _enemyFoldout = new VirtualFoldout("ポップ中エネミー");
            builder.VisualElement(_enemyFoldout);

            var spawnBtn = new DebugButton("エネミーをスポーン");
            spawnBtn.clicked += SpawnEnemy;
            builder.VisualElement(spawnBtn);

            var clearBtn = new DebugDangerButton("全エネミーを撃破");
            clearBtn.clicked += ClearAllEnemies;
            builder.VisualElement(clearBtn);

            // ── PlaceBehind デモ ─────────────────────────────────
            var anchor = new DebugLabel("─── PlaceBehind アンカー ───");
            builder.VisualElement(anchor);

            var insertBtn = new DebugButton("PlaceBehind でボタンを挿入");
            insertBtn.clicked += () =>
            {
                _placedHandle?.Dispose();
                _placedHandle = anchor.PlaceBehind(b =>
                {
                    var btn = new DebugSecondaryButton("動的に挿入されたボタン");
                    btn.clicked += () => Debug.Log("[DynamicDemo] PlaceBehind ボタンがタップされました");
                    b.VisualElement(btn);
                });
            };
            builder.VisualElement(insertBtn);

            var removeBtn = new DebugDangerButton("挿入を削除");
            removeBtn.clicked += () => _placedHandle?.Dispose();
            builder.VisualElement(removeBtn);
        }

        private void SpawnEnemy()
        {
            var id = ++_enemyCounter;
            var enemyName = $"Enemy-{id:D3}";

            IDisposable handle = null;
            handle = _enemyFoldout.AddDebugUI(b =>
            {
                b.HorizontalScope(hb =>
                {
                    hb.VisualElement(new DebugLabel(enemyName));

                    var killBtn = new DebugDangerButton("即死");
                    killBtn.clicked += () =>
                    {
                        Debug.Log($"[DynamicDemo] {enemyName} を撃破");
                        handle?.Dispose();
                        _enemyHandles.Remove(handle);
                    };
                    hb.VisualElement(killBtn);
                });
            });

            _enemyHandles.Add(handle);
            Debug.Log($"[DynamicDemo] {enemyName} がスポーンしました");
        }

        private void ClearAllEnemies()
        {
            foreach (var h in _enemyHandles) h.Dispose();
            _enemyHandles.Clear();
            Debug.Log("[DynamicDemo] 全エネミーを撃破しました");
        }
    }

    // ─── Player ───────────────────────────────────────────────────

    class PlayerPage : DebugPage
    {
        public override void Configure(IDebugUIBuilder builder)
        {
            var hpField = new DebugIntegerField("HP") { value = 100 };
            var setHpBtn = new DebugButton("HP をセット");
            setHpBtn.clicked += () => Debug.Log($"[Debug] HP → {hpField.value}");
            builder.VisualElement(hpField);
            builder.VisualElement(setHpBtn);

            builder.Foldout("チート", b =>
            {
                var fullHpBtn = new DebugButton("HP 最大化");
                fullHpBtn.clicked += () => Debug.Log("[Debug] HP 最大化");
                b.VisualElement(fullHpBtn);

                var invincibleBtn = new DebugButton("無敵モード");
                invincibleBtn.clicked += () => Debug.Log("[Debug] 無敵モード");
                b.VisualElement(invincibleBtn);
            });

            builder.NavigationButton("Player", () => new PlayerPage());
            builder.NavigationButton("Audio", () => new AudioPage());
        }
    }

    // ─── Settings ────────────────────────────────────────────────

    class SettingsPage : DebugPage
    {
        public override void Configure(IDebugUIBuilder builder)
        {
            var nameField = new DebugTextField("プレイヤー名") { value = "Player1" };
            builder.VisualElement(nameField);

            builder.NavigationButton<PlayerPage>();

            builder.VisualElement(new DebugLabel("プレイヤー設定"));

            var themeGroup = new DebugRadioButtonGroup("テーマ") { choices = new List<string> { "ライト", "ダーク" } };
            builder.VisualElement(themeGroup);

            var featureGroup = new DebugToggleGroup("有効機能");
            featureGroup.Add(new DebugToggleGroupItem("デバッグログ"));
            featureGroup.Add(new DebugToggleGroupItem("FPS表示"));
            builder.VisualElement(featureGroup);

            builder.Foldout("詳細設定", b =>
            {
                var outputGroup = new DebugToggleGroup("出力先");
                outputGroup.Add(new DebugToggleGroupItem("コンソール"));
                outputGroup.Add(new DebugToggleGroupItem("ファイル"));
                b.VisualElement(outputGroup);

                b.VisualElement(new DebugIntegerField("回数"));
            });

            builder.HorizontalScope(b =>
            {
                var resetBtn = new DebugSecondaryButton("リセット");
                resetBtn.clicked += () => Debug.Log("[Debug] リセット");
                b.VisualElement(resetBtn);

                var applyBtn = new DebugButton("適用");
                applyBtn.clicked += () => Debug.Log($"[Debug] 名前={nameField.value}");
                b.VisualElement(applyBtn);
            });

            var deleteBtn = new DebugDangerButton("設定を削除");
            deleteBtn.clicked += () => Debug.Log("[Debug] 設定を削除");
            builder.VisualElement(deleteBtn);
        }
    }

    // ─── Audio ────────────────────────────────────────────────────

    class AudioPage : DebugPage
    {
        public override void Configure(IDebugUIBuilder builder)
        {
            var bgmField = new DebugFloatField("BGM") { value = AudioListener.volume };
            var seField = new DebugFloatField("SE") { value = 1f };
            var applyBtn = new DebugButton("適用");
            applyBtn.clicked += () =>
            {
                AudioListener.volume = bgmField.value;
                Debug.Log($"[Debug] BGM={bgmField.value} SE={seField.value}");
            };
            builder.VisualElement(bgmField);
            builder.VisualElement(seField);
            builder.VisualElement(applyBtn);
            builder.NavigationButton("Player", () => new PlayerPage());
        }
    }
}
