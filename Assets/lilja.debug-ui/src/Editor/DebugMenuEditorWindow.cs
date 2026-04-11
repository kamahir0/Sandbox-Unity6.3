using System.Collections.Generic;
using Lilja.DebugUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugUI.Editor
{
    /// <summary>
    /// ランタイムの DebugMenu とリンクするエディタウィンドウ。
    /// PlayMode 中に DebugPageCache のページインスタンスを借用して表示する。
    /// ナビゲーションはエディタ独自の履歴を持ち、ランタイムには影響しない。
    /// </summary>
    public sealed class DebugMenuEditorWindow : EditorWindow
    {
        [MenuItem("Window/Lilja/Debug Menu Inspector")]
        private static void Open()
        {
            var window = GetWindow<DebugMenuEditorWindow>();
            window.titleContent = new GUIContent("Debug Menu Inspector");
        }

        // ── 借用状態 ──────────────────────────────────────────────────────

        private DebugPage _borrowedPage;
        private string _borrowedPageName;
        private readonly Stack<string> _editorHistory = new();

        // ── UI 要素 ────────────────────────────────────────────────────────

        private VisualElement _notPlayingView;
        private VisualElement _playingView;
        private Label _headerTitle;
        private Button _backButton;
        private ScrollView _pageListScrollView;
        private VisualElement _rightPane;

        // ── パス定数 ──────────────────────────────────────────────────────

        private const string ThemeUssPath =
            "Assets/lilja.debug-ui/src/Editor/StyleSheets/DebugMenuEditorTheme.uss";
        private const string MenuUssPath =
            "Assets/lilja.debug-ui/src/Runtime/StyleSheets/DebugMenu.uss";

        // ── ライフサイクル ─────────────────────────────────────────────────

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            // PlayMode 終了後は _window が null なので ReturnPage は安全に何もしない
            ReturnCurrentPage();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // スタイルシートをロード・適用
            var themeUss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeUssPath);
            var menuUss = AssetDatabase.LoadAssetAtPath<StyleSheet>(MenuUssPath);
            if (themeUss != null) root.styleSheets.Add(themeUss);
            if (menuUss != null) root.styleSheets.Add(menuUss);
            root.AddToClassList("editor-debug-window");

            BuildNotPlayingView(root);
            BuildPlayingView(root);

            RefreshPlayModeState();
        }

        // ── PlayMode 対応 ──────────────────────────────────────────────────

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // ランタイムが破棄される前に借用をキャンセル
                    _borrowedPage = null;
                    _borrowedPageName = null;
                    _editorHistory.Clear();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    ShowNotPlayingView();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    // Initialize() は任意のタイミングで呼ばれるため、完了を待機する
                    EditorApplication.delayCall += WaitForInitialize;
                    break;
            }
        }

        private void WaitForInitialize()
        {
            if (!Application.isPlaying)
                return;

            var names = DebugMenu.GetRegisteredPageNames();
            if (names == null || names.Count == 0)
            {
                // まだ Initialize されていない → 次フレームに再試行
                EditorApplication.delayCall += WaitForInitialize;
                return;
            }

            ShowPlayingView();
        }

        private void RefreshPlayModeState()
        {
            if (Application.isPlaying && DebugMenu.GetRegisteredPageNames()?.Count > 0)
                ShowPlayingView();
            else
                ShowNotPlayingView();
        }

        private void ShowNotPlayingView()
        {
            if (_notPlayingView == null || _playingView == null) return;
            _notPlayingView.style.display = DisplayStyle.Flex;
            _playingView.style.display = DisplayStyle.None;
        }

        private void ShowPlayingView()
        {
            if (_notPlayingView == null || _playingView == null) return;
            _notPlayingView.style.display = DisplayStyle.None;
            _playingView.style.display = DisplayStyle.Flex;
            RefreshPageList();
        }

        // ── ナビゲーション ─────────────────────────────────────────────────

        private void OnNavigateEvent(DebugNavigateEvent evt)
        {
            evt.StopPropagation();
            EditorNavigateTo(evt.PageName);
        }

        private void EditorNavigateTo(string pageName)
        {
            if (string.IsNullOrEmpty(pageName)) return;

            var prevName = _borrowedPageName;
            ReturnCurrentPage();

            var page = DebugMenu.BorrowPage(pageName);
            if (page == null) return;

            if (!string.IsNullOrEmpty(prevName) && prevName != pageName)
                _editorHistory.Push(prevName);

            _borrowedPage = page;
            _borrowedPageName = pageName;

            // アニメーションなし・即表示
            _rightPane.Add(page);
            page.style.left = new StyleLength(0f);
            SuppressScrollbarFocus(page);

            // ランタイムが奪い返したことを DetachFromPanelEvent で検知
            page.RegisterCallback<DetachFromPanelEvent>(OnBorrowedPageDetached);

            page.OnShown();
            UpdateHeader();
        }

        private void EditorBack()
        {
            if (_editorHistory.Count == 0) return;
            var prev = _editorHistory.Pop();
            // 履歴を復元するため Pop 後にナビゲート（Push は EditorNavigateTo 内で行われるが
            // 戻り方向なので現在のページ名を history に追加しない）
            ReturnCurrentPage();

            var page = DebugMenu.BorrowPage(prev);
            if (page == null) return;

            _borrowedPage = page;
            _borrowedPageName = prev;

            _rightPane.Add(page);
            page.style.left = new StyleLength(0f);
            SuppressScrollbarFocus(page);
            page.RegisterCallback<DetachFromPanelEvent>(OnBorrowedPageDetached);
            page.OnShown();
            UpdateHeader();
        }

        private void ReturnCurrentPage()
        {
            if (_borrowedPage == null) return;
            _borrowedPage.UnregisterCallback<DetachFromPanelEvent>(OnBorrowedPageDetached);
            _borrowedPage.OnHidden();
            DebugMenu.ReturnPage(_borrowedPage);
            _borrowedPage = null;
            _borrowedPageName = null;
        }

        private void OnBorrowedPageDetached(DetachFromPanelEvent evt)
        {
            // ランタイムが NavigateTo でページを奪い返した → エディタ側をリセット
            _borrowedPage = null;
            _borrowedPageName = null;
            _editorHistory.Clear();
            UpdateHeader();
        }

        // ── UI 構築 ────────────────────────────────────────────────────────

        private void BuildNotPlayingView(VisualElement root)
        {
            _notPlayingView = new VisualElement();
            _notPlayingView.style.flexGrow = 1;
            _notPlayingView.style.alignItems = Align.Center;
            _notPlayingView.style.justifyContent = Justify.Center;

            var label = new Label("PlayMode 実行中のみ使用できます");
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _notPlayingView.Add(label);

            root.Add(_notPlayingView);
        }

        private void BuildPlayingView(VisualElement root)
        {
            _playingView = new VisualElement();
            _playingView.style.flexGrow = 1;
            _playingView.style.flexDirection = FlexDirection.Column;

            // ヘッダー
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 4;
            header.style.paddingRight = 4;
            header.style.paddingTop = 2;
            header.style.paddingBottom = 2;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));

            _backButton = new Button(EditorBack) { text = "←" };
            _backButton.style.display = DisplayStyle.None;
            _backButton.style.marginRight = 4;
            header.Add(_backButton);

            _headerTitle = new Label("ページを選択してください");
            _headerTitle.style.flexGrow = 1;
            header.Add(_headerTitle);

            _playingView.Add(header);

            // ボディ（左ペイン + 右ペイン）
            var body = new TwoPaneSplitView(0, 160, TwoPaneSplitViewOrientation.Horizontal);
            body.style.flexGrow = 1;

            // 左ペイン: ページ一覧
            var leftContainer = new VisualElement();
            leftContainer.style.flexGrow = 1;
            leftContainer.style.borderRightWidth = 1;
            leftContainer.style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));

            var leftHeader = new Label("Pages");
            leftHeader.style.paddingLeft = 4;
            leftHeader.style.paddingTop = 2;
            leftHeader.style.paddingBottom = 2;
            leftHeader.style.borderBottomWidth = 1;
            leftHeader.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));
            leftContainer.Add(leftHeader);

            _pageListScrollView = new ScrollView();
            _pageListScrollView.style.flexGrow = 1;
            leftContainer.Add(_pageListScrollView);

            body.Add(leftContainer);

            // 右ペイン: DebugPage 表示コンテナ
            _rightPane = new VisualElement();
            _rightPane.AddToClassList("c-page-stack");
            _rightPane.style.flexGrow = 1;
            _rightPane.style.overflow = Overflow.Hidden;
            _rightPane.style.position = Position.Relative;
            _rightPane.RegisterCallback<DebugNavigateEvent>(OnNavigateEvent);
            body.Add(_rightPane);

            _playingView.Add(body);
            root.Add(_playingView);
        }

        private void RefreshPageList()
        {
            if (_pageListScrollView == null) return;
            _pageListScrollView.Clear();

            var names = DebugMenu.GetRegisteredPageNames();
            if (names == null) return;

            foreach (var name in names)
            {
                var pageName = name; // ラムダキャプチャ用
                var btn = new Button(() => EditorNavigateTo(pageName)) { text = pageName };
                btn.style.marginLeft = 0;
                btn.style.marginRight = 0;
                btn.style.marginTop = 1;
                btn.style.marginBottom = 0;
                _pageListScrollView.Add(btn);
            }
        }

        // 垂直スクロールバーのフォーカスビジュアル（青ハイライト）を抑制する。
        // CSS では Unity 組み込み USS を上書きできないため C# で処理する。
        private static void SuppressScrollbarFocus(VisualElement root)
        {
            root.Query<ScrollView>().ForEach(sv =>
            {
                sv.focusable = false;
                sv.verticalScroller.focusable = false;
                sv.verticalScroller.slider.focusable = false;
            });
        }

        private void UpdateHeader()
        {
            if (_headerTitle == null || _backButton == null) return;

            _headerTitle.text = string.IsNullOrEmpty(_borrowedPageName)
                ? "ページを選択してください"
                : _borrowedPageName;

            _backButton.style.display = _editorHistory.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
