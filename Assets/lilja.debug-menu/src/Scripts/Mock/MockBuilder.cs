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
}