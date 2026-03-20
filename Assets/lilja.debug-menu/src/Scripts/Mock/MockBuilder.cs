using Lilja.DebugMenu;
using UnityEngine;
using UnityEngine.UIElements;

public class MockBuilder : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private int _currentPageIndex = 0;
    private DebugMenuFrame _frame;
    private DebugPage _page1;
    private DebugPage _page2;
    private VisualElement _pageContainer;

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        root.Clear();

        // DebugMenuRoot
        var menuRoot = new DebugMenuRoot();
        root.Add(menuRoot);

        // DebugMenuFrame
        _frame = new DebugMenuFrame("Debug Menu");
        _frame.AddToClassList("l-debug-frame");
        menuRoot.Add(_frame);

        // バックボタンクリック時にページを戻る
        _frame.BackClicked += () => GoToPreviousPage();

        // ページコンテナ（複数ページをスタック）
        _pageContainer = new VisualElement();
        _pageContainer.AddToClassList("l-debug-page-container");
        _pageContainer.style.position = Position.Relative;
        _pageContainer.style.flexGrow = 1;
        _pageContainer.style.overflow = Overflow.Hidden;
        _frame.Add(_pageContainer);

        // ページ1：プレイヤー設定
        _page1 = CreatePage1();
        _page1.AddToClassList("l-debug-page");
        SetupPageLayout(_page1);
        _pageContainer.Add(_page1);

        // ページ2：詳細設定
        _page2 = CreatePage2();
        _page2.AddToClassList("l-debug-page");
        SetupPageLayout(_page2);
        _page2.style.display = DisplayStyle.None;  // 初期非表示
        _pageContainer.Add(_page2);

        // テスト用ボタン：次のページへ
        var nextPageButton = new DebugButton("詳細設定へ");
        _page1.Add(new VisualElement { style = { marginTop = 12 } });
        _page1.Add(nextPageButton);
        nextPageButton.clicked += () => GoToNextPage(_page1, _page2);
    }

    private void SetupPageLayout(DebugPage page)
    {
        page.style.position = Position.Absolute;
        page.style.left = 0;
        page.style.top = 0;
        page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        page.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
    }

    private DebugPage CreatePage1()
    {
        var page = new DebugPage();

        // プレイヤー設定ラベル
        page.Add(new DebugLabel("プレイヤー設定"));

        // プレイヤー名テキストフィールド
        page.Add(new DebugTextField("プレイヤー名") { value = "Player1" });

        // テーマ ラジオボタングループ
        page.Add(new DebugRadioButtonGroup("テーマ") { choices = new[] { "ライト", "ダーク" } });

        // 有効機能 トグルグループ
        var toggleGroup = new DebugToggleGroup("有効機能");
        toggleGroup.Add(new DebugToggleGroupItem("デバッグログ"));
        toggleGroup.Add(new DebugToggleGroupItem("FPS表示"));
        page.Add(toggleGroup);

        return page;
    }

    private DebugPage CreatePage2()
    {
        var page = new DebugPage();

        page.Add(new DebugLabel("詳細設定"));

        // 詳細設定 フォールドアウト
        var foldout = new DebugFoldout() { text = "出力設定" };
        var outputGroup = new DebugToggleGroup("出力先");
        outputGroup.Add(new DebugToggleGroupItem("コンソール"));
        outputGroup.Add(new DebugToggleGroupItem("ファイル"));
        foldout.Add(outputGroup);
        foldout.Add(new DebugIntegerField("ログレベル"));
        page.Add(foldout);

        page.Add(new DebugLabel("その他の設定"));
        page.Add(new DebugIntegerField("更新頻度（ms）"));

        return page;
    }

    private void GoToNextPage(DebugPage currentPage, DebugPage nextPage)
    {
        _currentPageIndex++;
        AnimatePageTransition(currentPage, nextPage, slideInFromRight: true);
    }

    private void GoToPreviousPage()
    {
        if (_currentPageIndex <= 0) return;

        if (_currentPageIndex == 1)
        {
            // Page2からPage1に戻る
            StartCoroutine(AnimateSlideOut(_page2, 0.4f, () =>
            {
                _page2.style.display = DisplayStyle.None;
                _page1.style.display = DisplayStyle.Flex;
                _page1.style.left = new StyleLength(new Length(0, LengthUnit.Percent));
            }));
        }

        _currentPageIndex--;
    }

    private void AnimatePageTransition(DebugPage outPage, DebugPage inPage, bool slideInFromRight)
    {
        inPage.style.display = DisplayStyle.Flex;

        if (slideInFromRight)
        {
            // inPageを画面外右から移動
            inPage.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            StartCoroutine(AnimateSlideIn(inPage, 0.4f));
        }
    }

    private System.Collections.IEnumerator AnimateSlideIn(DebugPage page, float duration)
    {
        float elapsedTime = 0f;
        float startX = 100f;
        float endX = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // イージング: Ease-In-Out Cubic
            float easedProgress = progress < 0.5f
                ? 4f * progress * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 3f) / 2f;

            float currentX = Mathf.Lerp(startX, endX, easedProgress);
            page.style.left = new StyleLength(new Length(currentX, LengthUnit.Percent));

            yield return null;
        }

        page.style.left = new StyleLength(new Length(endX, LengthUnit.Percent));
    }

    private System.Collections.IEnumerator AnimateSlideOut(DebugPage page, float duration, System.Action onComplete)
    {
        float elapsedTime = 0f;
        float startX = 0f;
        float endX = 100f;  // 右へスライドアウト

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            // イージング: Ease-In-Out Cubic
            float easedProgress = progress < 0.5f
                ? 4f * progress * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 3f) / 2f;

            float currentX = Mathf.Lerp(startX, endX, easedProgress);
            page.style.left = new StyleLength(new Length(currentX, LengthUnit.Percent));

            yield return null;
        }

        page.style.left = new StyleLength(new Length(endX, LengthUnit.Percent));
        onComplete?.Invoke();
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