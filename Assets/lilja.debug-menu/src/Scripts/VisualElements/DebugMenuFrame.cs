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
        private readonly VisualElement _pageContainer;

        // ナビゲーション
        private readonly DebugPageCache _pageCache = new();
        private readonly Stack<string> _history = new();
        private string _currentPageName;
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

        public DebugMenuFrame() : this(null) { }

        public DebugMenuFrame(DebugPage rootPage)
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

            // コンテンツエリア
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(contentUssClassName);
            hierarchy.Add(_contentContainer);

            // ページコンテナ
            _pageContainer = new VisualElement();
            _pageContainer.AddToClassList("c-page-stack");
            _contentContainer.Add(_pageContainer);

            // ルートページ
            if (rootPage != null)
            {
                RegisterPage(rootPage.name, rootPage);
                ShowPageImmediately(rootPage, PagePosition.In);
                rootPage.Configure(new DebugPageBuilder(rootPage, _pageCache));
            }

            DebugMenuManager.Frame = this;
        }

        /// <summary>
        /// ページを登録する
        /// </summary>
        public void RegisterPage(string pageName, DebugPage page)
        {
            _pageCache.Add(pageName, page);

            // 画面外で待機
            page.style.position = Position.Absolute;
            page.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.top = 0;
            page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            _pageContainer.Add(page);
        }

        /// <summary>
        /// 指定したページへ遷移する
        /// </summary>
        public void Navigate(string pageName)
        {
            if (_isAnimating) return;
            if (!_pageCache.TryGet(pageName, out var page)) return;
            if (pageName == _currentPageName) return;

            var prevName = _currentPageName;
            _currentPageName = pageName;
            Label = pageName;

            // 初期表示はアニメーションなし
            if (prevName == null)
            {
                page.Configure(new DebugPageBuilder(page, _pageCache));
                page.style.left = new StyleLength(new Length(0, LengthUnit.Percent));
                return;
            }

            _isAnimating = true;

            // 現在ページを左へスライドアウト
            if (_pageCache.TryGet(prevName, out var prevPage))
            {
                prevPage.Configure(new DebugPageBuilder(prevPage, _pageCache));
                SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration, null);
            }

            // 次ページを右からスライドイン
            page.Configure(new DebugPageBuilder(page, _pageCache));
            SlidePage(page, PagePosition.OutR, PagePosition.In, AnimationDuration, () =>
            {
                _history.Push(prevName);
                _isAnimating = false;
            });
        }

        /// <summary> 前のページへ戻る </summary>
        private void Back()
        {
            if (_isAnimating) return;
            if (_history.Count == 0) return;

            _isAnimating = true;

            var prevName = _history.Pop();
            var currentName = _currentPageName;
            _currentPageName = prevName;

            if (!_pageCache.TryGet(prevName, out var prevPage)) return;
            if (!_pageCache.TryGet(currentName, out var currentPage)) return;

            Label = prevName;

            // 現在ページを右へスライドアウト
            SlidePage(currentPage, PagePosition.In, PagePosition.OutR, AnimationDuration, null);

            // 前ページを左からスライドイン
            SlidePage(prevPage, PagePosition.OutL, PagePosition.In, AnimationDuration, () =>
            {
                _isAnimating = false;
            });
        }

        /// <summary> 指定したページをスライドさせずに即座に表示する </summary>
        private void ShowPageImmediately(DebugPage page, PagePosition position)
        {
            page.style.left = new StyleLength(new Length((float)position, LengthUnit.Percent));
        }

        /// <summary> 指定したページをスライドさせる </summary>
        private void SlidePage(DebugPage page, PagePosition from, PagePosition to, float duration, Action onComplete)
        {
            var leftStart = (float)from;
            var leftEnd = (float)to;

            page.style.left = new StyleLength(new Length(leftStart, LengthUnit.Percent));

            float elapsed = 0f;
            schedule.Execute(timer =>
            {
                elapsed += timer.deltaTime / 1000f;
                var t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));
                page.style.left = new StyleLength(new Length(Mathf.Lerp(leftStart, leftEnd, t), LengthUnit.Percent));

                if (elapsed >= duration)
                {
                    page.style.left = new StyleLength(new Length(leftEnd, LengthUnit.Percent));
                    onComplete?.Invoke();
                }
            }).Every(0).Until(() => elapsed >= duration);
        }

        private static float EaseInOutCubic(float t) =>
            t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
