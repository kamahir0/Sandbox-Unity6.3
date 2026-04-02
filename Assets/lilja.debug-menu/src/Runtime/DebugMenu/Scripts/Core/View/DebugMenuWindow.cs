using System;
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
        private DebugPageNavigator _navigator;

        // 位置コントロール
        private DebugMenuPositionController _positionController;

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
            AddToClassList(DebugMenuWindowUssClass.Root);
            AddToClassList(DebugMenuWindowUssClass.Surface);
            AddToClassList(DebugMenuWindowUssClass.DefaultSize);

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
                visible => SetBackButtonVisibility(visible));

            _navigator.InitRootPage(rootPage);
        }

        // ── ナビゲーション公開 API ──────────────────────────────────────

        /// <summary>
        /// 指定したページへ遷移する
        /// </summary>
        internal void Navigate(string pageName) => _navigator?.Navigate(pageName);

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

        // ── UI 構築 ────────────────────────────────────────────────────

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

        private void SetBackButtonVisibility(bool visible)
        {
            _backButton.style.visibility = visible
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }
}
