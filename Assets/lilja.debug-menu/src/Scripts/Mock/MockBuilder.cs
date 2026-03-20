using Lilja.DebugMenu;
using UnityEngine;
using UnityEngine.UIElements;

public class MockBuilder : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        root.Clear();

        // DebugMenuRoot
        var menuRoot = new DebugMenuRoot();
        root.Add(menuRoot);

        // DebugMenuFrame
        var frame = new DebugMenuFrame("Debug Menu");
        frame.style.width = 700;
        frame.style.maxWidth = new StyleLength(new Length(90, LengthUnit.Percent));
        menuRoot.Add(frame);

        // バックボタンクリック時にアンカードポジションをアニメーション付きで移動
        frame.BackClicked += () => AnimateFramePosition(frame);

        // DebugPage
        var scrollView = new DebugPage();
        frame.Add(scrollView);

        // プレイヤー設定ラベル
        scrollView.Add(new DebugLabel("プレイヤー設定"));

        // プレイヤー名テキストフィールド
        scrollView.Add(new DebugTextField("プレイヤー名") { value = "Player1" });

        // テーマ ラジオボタングループ
        scrollView.Add(new DebugRadioButtonGroup("テーマ") { choices = new[] { "ライト", "ダーク" } });

        // 有効機能 トグルグループ
        var toggleGroup = new DebugToggleGroup("有効機能");
        toggleGroup.Add(new DebugToggleGroupItem("デバッグログ"));
        toggleGroup.Add(new DebugToggleGroupItem("FPS表示"));
        scrollView.Add(toggleGroup);

        // 詳細設定 フォールドアウト
        var foldout = new DebugFoldout() { text = "詳細設定" };
        var outputGroup = new DebugToggleGroup("出力先");
        outputGroup.Add(new DebugToggleGroupItem("コンソール"));
        outputGroup.Add(new DebugToggleGroupItem("ファイル"));
        foldout.Add(outputGroup);
        foldout.Add(new DebugIntegerField("回数"));
        scrollView.Add(foldout);

        // ボタン行
        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.marginTop = 12;

        var resetButton = new DebugSecondaryButton("リセット");
        resetButton.style.flexGrow = 1;
        buttonRow.Add(resetButton);

        var applyButton = new DebugButton("適用");
        applyButton.style.flexGrow = 1;
        buttonRow.Add(applyButton);

        scrollView.Add(buttonRow);
    }

    private void AnimateFramePosition(DebugMenuFrame frame)
    {
        var currentTranslate = frame.style.translate.value;
        var targetTranslate = new Translate(
            currentTranslate.x.value + 20,
            currentTranslate.y.value + 10
        );

        StartCoroutine(AnimateTranslate(frame, currentTranslate, targetTranslate, 0.5f));
    }

    private System.Collections.IEnumerator AnimateTranslate(DebugMenuFrame frame, Translate start, Translate target, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // イージング: Ease-In-Out Quad
            float easedProgress = progress < 0.5f
                ? 2f * progress * progress
                : -1f + (4f - 2f * progress) * progress;

            var current = new Translate(
                Mathf.Lerp(start.x.value, target.x.value, easedProgress),
                Mathf.Lerp(start.y.value, target.y.value, easedProgress)
            );

            frame.style.translate = current;
            yield return null;
        }

        frame.style.translate = target;
    }
}