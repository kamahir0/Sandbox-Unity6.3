using Lilja.DebugMenu;
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
        DebugMenuManager.Initialize(new RootPage());
    }

    // ─── Root ─────────────────────────────────────────────────────

    class RootPage : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.NavigationButton("Player", () => new PlayerPage());
            builder.NavigationButton("Audio", () => new AudioPage());
            builder.NavigationButton("Scene", b =>
            {
                var titleBtn = new DebugButton("Title");
                titleBtn.clicked += () => SceneManager.LoadScene("Title");
                b.VisualElement(titleBtn);

                var stage1Btn = new DebugButton("Stage 1");
                stage1Btn.clicked += () => SceneManager.LoadScene("Stage1");
                b.VisualElement(stage1Btn);
            });
            builder.Foldout("App Info", b =>
            {
                b.VisualElement(new DebugLabel($"Version: {Application.version}"));
                b.VisualElement(new DebugLabel($"Platform: {Application.platform}"));
            });
        }
    }

    // ─── Player ───────────────────────────────────────────────────

    class PlayerPage : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
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
        }
    }

    // ─── Audio ────────────────────────────────────────────────────

    class AudioPage : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
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
        }
    }
}
