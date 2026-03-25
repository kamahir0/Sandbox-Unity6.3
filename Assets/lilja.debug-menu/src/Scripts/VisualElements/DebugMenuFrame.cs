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

                // DOM に追加して即座表示
                EnsureInDom(rootPage);
                ShowPageImmediately(rootPage, PagePosition.In);
            }

            UpdateBackButtonVisibility();
            DebugMenuManager.Frame = this;
        }

        /// <summary>
        /// 指定したページへ遷移する
        /// </summary>
        public void Navigate(string pageName)
        {
            if (_isAnimating) return;
            if (_currentPage == null) return;

            // プールから借用、なければファクトリで新規生成
            if (!_pagePool.TryRent(pageName, out var targetPage))
            {
                targetPage = _pagePool.CreateNew(pageName);
                if (targetPage == null) return;
            }

            EnsureInDom(targetPage);

            var prevPage = _currentPage;
            _currentPage = targetPage;
            Label = pageName;

            // 同一名ナビゲーション: 履歴にpushせずプールに返却
            if (prevPage.name == pageName)
            {
                _pagePool.Return(prevPage);
            }
            else
            {
                _history.Push(prevPage);
            }

            // アニメーション
            _isAnimating = true;
            SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration, null);
            SlidePage(targetPage, PagePosition.OutR, PagePosition.In, AnimationDuration, () =>
            {
                _isAnimating = false;
                UpdateBackButtonVisibility();
            });
        }

        /// <summary>
        /// 初期化完了後に動的にページを登録する。既に登録済みなら無視。
        /// </summary>
        public void RegisterPage(string name, Func<DebugPage> factory)
        {
            if (_pagePool.Contains(name)) return;

            _pagePool.Reserve(name);
            _pagePool.RegisterFactory(name, () =>
            {
                var p = factory();
                p.name = name;
                return p;
            });

            var page = factory();
            page.name = name;
            page.Configure(new DebugPageBuilder(page, _pagePool));
            _pagePool.Add(name, page);
        }

        /// <summary> 前のページへ戻る </summary>
        private void Back()
        {
            if (_isAnimating) return;
            if (_history.Count == 0) return;

            _isAnimating = true;

            var prevPage = _history.Pop();
            var currentPage = _currentPage;

            // 現在ページをプールに返却（スクロールリセット含む）
            _pagePool.Return(currentPage);

            _currentPage = prevPage;
            Label = prevPage.name;

            // アニメーション
            SlidePage(currentPage, PagePosition.In, PagePosition.OutR, AnimationDuration, null);
            SlidePage(prevPage, PagePosition.OutL, PagePosition.In, AnimationDuration, () =>
            {
                _isAnimating = false;
                UpdateBackButtonVisibility();
            });
        }

        /// <summary>
        /// ページがまだ _pageContainer に追加されていなければ追加し、画面外に配置する。
        /// </summary>
        private void EnsureInDom(DebugPage page)
        {
            if (page.parent == _pageContainer) return;

            page.style.position = Position.Absolute;
            page.style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.top = 0;
            page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            _pageContainer.Add(page);
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

        private void UpdateBackButtonVisibility()
        {
            _backButton.style.visibility = _history.Count > 0
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private static float EaseInOutCubic(float t) =>
            t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
