using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニューのウィンドウ
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuWindow : VisualElement
    {
        // UI
        private Button _backButton;
        private Label _label;
        private VisualElement _contentContainer;
        private VisualElement _header;

        // ナビゲーション
        private readonly DebugPagePool _pagePool = new();
        private readonly Stack<DebugPage> _history = new();


        // 位置コントロール
        private DebugMenuPositionController _positionController;
        private const float AnimationDuration = 0.4f;

        private DebugPage _currentPage;
        private bool _isAnimating;



        private enum PagePosition
        {
            In = 0,
            OutL = -100,
            OutR = 100
        }

        /// <inheritdoc/>
        public override VisualElement contentContainer => _contentContainer;

        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DebugMenuWindow()
        {
            AddToClassList(DebugMenuWindowUssClass.Root);
            AddToClassList(DebugMenuWindowUssClass.Surface);
            AddToClassList(DebugMenuWindowUssClass.DefaultSize);

            BuildHeader();
            BuildContent();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        /// <summary>
        /// ヘッダー領域（バックボタン・タイトル・スペーサー）を構築する
        /// </summary>
        private void BuildHeader()
        {
            _header = new VisualElement();
            _header.AddToClassList(DebugMenuWindowUssClass.Header);
            hierarchy.Add(_header);

            // バックボタン
            _backButton = new Button();
            _backButton.AddToClassList(DebugMenuWindowUssClass.BackButton);
            var backButtonIcon = new VisualElement();
            backButtonIcon.AddToClassList(DebugMenuWindowUssClass.BackButtonIcon);
            backButtonIcon.pickingMode = PickingMode.Ignore;
            _backButton.Add(backButtonIcon);
            _backButton.clicked += Back;
            _header.Add(_backButton);

            // タイトルラベル
            _label = new Label();
            _label.AddToClassList(DebugMenuWindowUssClass.Title);
            _header.Add(_label);

            // スペーサー
            // NOTE: バックボタンと同幅のスペーサーでタイトルを視覚的に中央寄せ
            var spacer = new VisualElement();
            spacer.AddToClassList(DebugMenuWindowUssClass.HeaderSpacer);
            spacer.pickingMode = PickingMode.Ignore;
            _header.Add(spacer);
        }

        /// <summary>
        /// コンテンツエリア（ページスタック）を構築する
        /// </summary>
        private void BuildContent()
        {
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(DebugMenuWindowUssClass.Content);
            _contentContainer.AddToClassList(DebugMenuWindowUssClass.PageStack);
            hierarchy.Add(_contentContainer);
        }

        /// <summary>
        /// コンストラクタ（ランタイム）
        /// </summary>
        public DebugMenuWindow(DebugPage rootPage) : this()
        {
            if (rootPage == null) return;

            // name が未設定なら型名をフォールバックとして使用
            if (string.IsNullOrEmpty(rootPage.name))
            {
                rootPage.name = rootPage.GetType().Name;
            }

            _currentPage = rootPage;
            Label = rootPage.name;

            // 循環防止マーカー設置後に Configure
            _pagePool.Reserve(rootPage.name);
            rootPage.Configure(new DebugPageBuilder(rootPage, _pagePool));

            rootPage.SetLayout();
            _contentContainer.Add(rootPage);
            ShowPageImmediately(rootPage, PagePosition.In);
            SetBackButtonVisibility();
        }

        /// <summary>
        /// 指定したページへ遷移する
        /// </summary>
        internal void Navigate(string pageName)
        {
            if (_isAnimating) return;
            if (_currentPage == null) return;

            var targetPage = _pagePool.Rent(pageName);
            if (targetPage == null) return;

            OnNavigate(targetPage);
        }

        /// <summary>
        /// GenericDebugPage を即席生成して遷移する。事前登録不要。
        /// </summary>
        internal void NavigateTemp(string pageName, Action<IDebugPageBuilder> configure)
        {
            if (_isAnimating) return;
            if (_currentPage == null) return;

            var page = new GenericDebugPage(pageName, configure);
            page.Configure(new DebugPageBuilder(page, _pagePool));
            OnNavigate(page);
        }

        /// <summary>
        /// 初期化完了後に動的にページを登録する。既に登録済みなら無視。
        /// </summary>
        public void RegisterPage(string pageName, Func<DebugPage> factory)
        {
            _pagePool.Register(pageName, factory);
        }

        /// <summary>
        /// 指定ページ名がプールに登録済みか返す。
        /// </summary>
        internal bool IsPageRegistered(string pageName) => _pagePool.Contains(pageName);

        /// <summary> 前のページへ戻る </summary>
        internal void Back()
        {
            if (_isAnimating) return;
            if (_history.Count == 0) return;

            _isAnimating = true;

            var prevPage = _history.Pop();
            var currentPage = _currentPage;

            _currentPage = prevPage;
            Label = prevPage.name;

            // アニメーション完了後にプールへ返却（スクロールリセットはページが画面外に出てから）
            SlidePage(currentPage, PagePosition.In, PagePosition.OutR, AnimationDuration, () =>
            {
                _pagePool.Return(currentPage);
            });
            SlidePage(prevPage, PagePosition.OutL, PagePosition.In, AnimationDuration, () =>
            {
                _isAnimating = false;
                SetBackButtonVisibility();
            });
        }

        /// <summary>
        /// ページ遷移時処理
        /// </summary>
        private void OnNavigate(DebugPage targetPage)
        {
            if (targetPage.parent != _contentContainer)
            {
                _contentContainer.Add(targetPage);
            }

            // ラベル更新
            Label = targetPage.name;

            var prevPage = _currentPage;
            _currentPage = targetPage;

            // アニメーション
            _isAnimating = true;
            if (prevPage.name == targetPage.name)
            {
                // 同一名ナビゲーション: 履歴にpushせず、アニメーション完了後にプールへ返却
                SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration, () =>
                {
                    _pagePool.Return(prevPage);
                });
            }
            else
            {
                _history.Push(prevPage);
                SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration);
            }
            SlidePage(targetPage, PagePosition.OutR, PagePosition.In, AnimationDuration, () =>
            {
                _isAnimating = false;
                SetBackButtonVisibility();
            });
        }


        /// <summary> 指定したページをスライドさせずに即座に表示する </summary>
        private void ShowPageImmediately(DebugPage page, PagePosition position)
        {
            page.style.left = new StyleLength(new Length((float)position, LengthUnit.Percent));
        }

        /// <summary> 指定したページをスライドさせる </summary>
        private void SlidePage(DebugPage page, PagePosition from, PagePosition to, float duration, Action onComplete = null)
        {
            DebugMenuAnimator.Slide(page, this, (float)from, (float)to, duration, onComplete);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _positionController = new DebugMenuPositionController(this, _header);
            _positionController.RestoreOrDefault();
        }

        public void SetHidden()
        {
            style.display = DisplayStyle.None;
            style.opacity = 0f;
        }

        private void SetBackButtonVisibility()
        {
            _backButton.style.visibility = _history.Count > 0
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }
}
