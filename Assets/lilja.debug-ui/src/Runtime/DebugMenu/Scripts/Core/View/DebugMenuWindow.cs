using System;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    /// <summary>
    /// デバッグメニューのウィンドウ
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuWindow : VisualElement
    {
        // UI
        private Label _label;
        private VisualElement _header;
        private Button _backButton;
        private Button _backToRootButton;
        private VisualElement _contentContainer;

        private DebugPageNavigator _navigator;
        private DebugMenuPositionController _positionController;

        // クラス
        private const string UssClassName = "c-menu-window";
        private const string HeaderUssClassName = UssClassName + "__header";
        private const string BackButtonUssClassName = UssClassName + "__back-button";
        private const string BackButtonIconUssClassName = UssClassName + "__back-button-icon";
        private const string BackToRootButtonUssClassName = UssClassName + "__back-to-root-button";
        private const string BackToRootButtonIconUssClassName = UssClassName + "__back-to-root-button-icon";
        private const string TitleUssClassName = UssClassName + "__title";
        private const string ContentUssClassName = UssClassName + "__content";
        private const string DefaultSizeUssClassName = UssClassName + "--default-size";
        private const string SurfaceUssClassName = "t-surface";
        private const string PageStackUssClassName = "c-page-stack";

        /// <inheritdoc/>
        public override VisualElement contentContainer => _contentContainer;

        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>
        /// コンストラクタ（UXML / デフォルト）
        /// </summary>
        public DebugMenuWindow()
        {
            AddToClassList(UssClassName);
            AddToClassList(SurfaceUssClassName);
            AddToClassList(DefaultSizeUssClassName);

            BuildHeader();
            BuildContent();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        /// <summary>
        /// コンストラクタ（ランタイム起動用）
        /// </summary>
        public DebugMenuWindow(DebugPage rootPage) : this()
        {
            _navigator = new DebugPageNavigator(
                _contentContainer,
                this,
                label => Label = label,
                SetBackButtonVisibility);

            _navigator.InitRootPage(rootPage);
        }

        // ── ナビゲーション公開 API ──────────────────────────────────────

        /// <summary>
        /// 指定したページへ遷移する
        /// </summary>
        internal void Navigate(string pageName)
            => _navigator?.Navigate(pageName);

        /// <summary>
        /// GenericDebugPage を即席生成して遷移する。事前登録不要。
        /// </summary>
        internal void NavigateTemp(string pageName, Action<IDebugPageBuilder> configure)
            => _navigator?.NavigateTemp(pageName, configure);

        /// <summary>
        /// 初期化完了後に動的にページを登録する。既に登録済みなら無視。
        /// </summary>
        public void RegisterPage(string pageName, Func<DebugPage> factory)
            => _navigator?.PagePool.Register(pageName, factory);

        /// <summary>
        /// 指定ページ名がプールに登録済みか返す。
        /// </summary>
        internal bool IsPageRegistered(string pageName)
            => _navigator?.IsPageRegistered(pageName) ?? false;

        /// <summary> 前のページへ戻る </summary>
        internal void Back() => _navigator?.Back();

        /// <summary> 履歴を全て破棄してルートページへ戻る </summary>
        internal void BackToRoot() => _navigator?.BackToRoot();

        // ── UI 構築 ────────────────────────────────────────────────────

        /// <summary>
        /// ヘッダー領域（バックボタン・タイトル・スペーサー）を構築する
        /// </summary>
        private void BuildHeader()
        {
            _header = new VisualElement();
            _header.AddToClassList(HeaderUssClassName);
            hierarchy.Add(_header);

            // バックボタン
            _backButton = new Button();
            _backButton.AddToClassList(BackButtonUssClassName);
            var backButtonIcon = new VisualElement();
            backButtonIcon.AddToClassList(BackButtonIconUssClassName);
            backButtonIcon.pickingMode = PickingMode.Ignore;
            _backButton.Add(backButtonIcon);
            ButtonInteractionHelper.Register(_backButton);  // clickable 置き換えのため clicked += より先に呼ぶ
            _backButton.clicked += Back;
            _header.Add(_backButton);

            // タイトルラベル
            _label = new Label();
            _label.AddToClassList(TitleUssClassName);
            _header.Add(_label);

            // バックトゥルートボタン（バックボタンと対になる位置でタイトルを視覚的に中央寄せ）
            _backToRootButton = new Button();
            _backToRootButton.AddToClassList(BackToRootButtonUssClassName);
            var backToRootButtonIcon = new VisualElement();
            backToRootButtonIcon.AddToClassList(BackToRootButtonIconUssClassName);
            backToRootButtonIcon.pickingMode = PickingMode.Ignore;
            _backToRootButton.Add(backToRootButtonIcon);
            ButtonInteractionHelper.Register(_backToRootButton);  // clickable 置き換えのため clicked += より先に呼ぶ
            _backToRootButton.clicked += BackToRoot;
            _header.Add(_backToRootButton);
        }

        /// <summary>
        /// コンテンツエリア（ページスタック）を構築する
        /// </summary>
        private void BuildContent()
        {
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(ContentUssClassName);
            _contentContainer.AddToClassList(PageStackUssClassName);
            hierarchy.Add(_contentContainer);
        }

        // ── View 状態反映 ──────────────────────────────────────────────

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _positionController = new DebugMenuPositionController(this, _header);
            _positionController.RestoreOrDefault();
        }

        /// <summary>
        /// 非表示状態にする（Manager の Show/Hide アニメーション後に呼ばれる）
        /// </summary>
        public void SetHidden()
        {
            style.display = DisplayStyle.None;
            style.opacity = 0f;
        }

        private void SetBackButtonVisibility(bool visiblity)
        {
            var v = visiblity ? Visibility.Visible : Visibility.Hidden;
            _backButton.style.visibility = v;
            _backToRootButton.style.visibility = v;
        }
    }
}
