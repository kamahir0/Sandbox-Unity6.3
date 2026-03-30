using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニューのフレーム
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuFrame : VisualElement
    {
        // クラス
        public static readonly string ussClassName = "c-menu-frame";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string backButtonUssClassName = ussClassName + "__back-button";
        public static readonly string backButtonIconUssClassName = ussClassName + "__back-button-icon";
        public static readonly string headerSpacerUssClassName = ussClassName + "__header-spacer";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";

        // UI
        private readonly Button _backButton;
        private readonly Label _label;
        private readonly VisualElement _contentContainer;

        // ナビゲーション
        private readonly DebugPagePool _pagePool = new();
        private readonly Stack<DebugPage> _history = new();
        private DebugPage _currentPage;
        private bool _isAnimating;

        private const float AnimationDuration = 0.4f;

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
        public DebugMenuFrame()
        {
            AddToClassList(ussClassName);
            AddToClassList("t-surface");

            // ヘッダー
            var header = new VisualElement();
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            // バックボタン
            _backButton = new Button();
            _backButton.AddToClassList(backButtonUssClassName);
            var backButtonIcon = new VisualElement();
            backButtonIcon.AddToClassList(backButtonIconUssClassName);
            backButtonIcon.pickingMode = PickingMode.Ignore;
            _backButton.Add(backButtonIcon);
            _backButton.clicked += Back;
            header.Add(_backButton);

            // タイトルラベル
            _label = new Label();
            _label.AddToClassList(titleUssClassName);
            header.Add(_label);

            // スペーサー
            // NOTE: バックボタンと同幅のスペーサーでタイトルを視覚的に中央寄せ
            var spacer = new VisualElement();
            spacer.AddToClassList(headerSpacerUssClassName);
            spacer.pickingMode = PickingMode.Ignore;
            header.Add(spacer);

            // コンテンツエリア（ページスタックを兼ねる）
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(contentUssClassName);
            _contentContainer.AddToClassList("c-page-stack");
            hierarchy.Add(_contentContainer);
        }

        /// <summary>
        /// コンストラクタ（ランタイム）
        /// </summary>
        public DebugMenuFrame(DebugPage rootPage) : this()
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

            EnsureInDom(rootPage);
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
        public void RegisterPage(string name, Func<DebugPage> factory)
        {
            _pagePool.Register(name, factory);
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
            // DOMに追加
            EnsureInDom(targetPage);

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

        /// <summary> ページがまだ _contentContainer に追加されていなければ追加し、画面外に配置する。 </summary>
        private void EnsureInDom(DebugPage page)
        {
            if (page.parent == _contentContainer) return;

            page.style.position = Position.Absolute;
            page.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.top = 0;
            page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            _contentContainer.Add(page);
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
