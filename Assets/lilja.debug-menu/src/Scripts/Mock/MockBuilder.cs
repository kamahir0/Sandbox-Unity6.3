using Lilja.DebugMenu;
using UnityEngine;
using UnityEngine.UIElements;

public class MockBuilder : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private DebugMenuFrame _frame;

    private void OnEnable()
    {
        // DebugMenuFrameを初期化
        _frame = Init();

        #region A
        // // ページを登録
        // _frame.RegisterPage("home", CreatePage1());
        // _frame.RegisterPage("settings", CreatePage2());
        // _frame.RegisterPage("log", CreatePage3());

        // // 初期ページへ遷移
        // _frame.Navigate("home");
        #endregion

        #region B
        var page1 = new Page1();
        var page2 = new Page2();
        _frame.RegisterPage(page1.GetType().Name, page1);
        _frame.RegisterPage(page2.GetType().Name, page2);
        _frame.Navigate(page1.GetType().Name);
        #endregion
    }

    private DebugMenuFrame Init()
    {
        var root = _uiDocument.rootVisualElement;
        root.Clear();

        // DebugMenuRoot
        var menuRoot = new DebugMenuRoot();
        root.Add(menuRoot);

        // DebugMenuFrame
        var frame = new DebugMenuFrame();
        frame.AddToClassList("c-menu-frame--default-size");
        menuRoot.Add(frame);

        return frame;
    }

    private DebugPage CreatePage1()
    {
        var page = new DebugPage();

        page.Add(new DebugLabel("プレイヤー設定"));
        page.Add(new DebugTextField("プレイヤー名") { value = "Player1" });
        page.Add(new DebugRadioButtonGroup("テーマ") { choices = new[] { "ライト", "ダーク" } });

        var toggleGroup = new DebugToggleGroup("有効機能");
        toggleGroup.Add(new DebugToggleGroupItem("デバッグログ"));
        toggleGroup.Add(new DebugToggleGroupItem("FPS表示"));
        page.Add(toggleGroup);

        page.Add(new VisualElement { style = { marginTop = 12 } });
        var nextButton = new DebugButton("詳細設定へ");
        nextButton.clicked += () => _frame.Navigate("settings");
        page.Add(nextButton);

        return page;
    }

    private DebugPage CreatePage2()
    {
        var page = new DebugPage();

        page.Add(new DebugLabel("詳細設定"));

        var foldout = new DebugFoldout() { text = "出力設定" };
        var outputGroup = new DebugToggleGroup("出力先");
        outputGroup.Add(new DebugToggleGroupItem("コンソール"));
        outputGroup.Add(new DebugToggleGroupItem("ファイル"));
        foldout.Add(outputGroup);
        foldout.Add(new DebugIntegerField("ログレベル"));
        page.Add(foldout);

        page.Add(new DebugLabel("その他の設定"));
        page.Add(new DebugIntegerField("更新頻度（ms）"));

        page.Add(new VisualElement { style = { marginTop = 12 } });
        var nextButton = new DebugButton("ログ設定へ");
        nextButton.clicked += () => _frame.Navigate("log");
        page.Add(nextButton);

        return page;
    }

    private DebugPage CreatePage3()
    {
        var page = new DebugPage();

        page.Add(new DebugLabel("ログ設定"));

        var outputGroup = new DebugToggleGroup("出力先");
        outputGroup.Add(new DebugToggleGroupItem("コンソール"));
        outputGroup.Add(new DebugToggleGroupItem("ファイル"));
        outputGroup.Add(new DebugToggleGroupItem("リモート"));
        page.Add(outputGroup);

        page.Add(new DebugIntegerField("ログレベル"));
        page.Add(new DebugIntegerField("最大行数"));
        page.Add(new DebugTextField("出力パス") { value = "Logs/debug.log" });

        return page;
    }

    public class Page1 : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ1");
            builder.Button("次へ1");
            builder.NavigationButton<Page1>();
            builder.NavigationButton<Page2>();
        }
    }

    public class Page2 : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ2");
            builder.Button("次へ2");
            builder.NavigationButton<Page2>();
        }
    }
}
