using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// DebugMenuWindow のページナビゲーションを担う。
    /// 履歴スタック・ページプール・アニメーション制御を一括管理し、
    /// View 側（DebugMenuWindow）へはコールバックで通知する。
    /// </summary>
    internal sealed class DebugPageNavigator
    {
        private readonly DebugPagePool _pagePool = new();
        private readonly Stack<DebugPage> _history = new();

        /// <summary>アニメーションスケジュールの基点となる VisualElement（Window 自身）</summary>
        private readonly VisualElement _animationScheduler;

        /// <summary>ページコンテンツを格納するコンテナ</summary>
        private readonly VisualElement _contentContainer;

        /// <summary>ウィンドウタイトルを更新するコールバック</summary>
        private readonly Action<string> _onLabelChanged;

        /// <summary>バックボタン可視性を更新するコールバック（true = 表示）</summary>
        private readonly Action<bool> _onBackVisibilityChanged;

        private DebugPage _currentPage;
        private bool _isAnimating;
        private int _animVersion;

        private const float AnimationDuration = 0.4f;

        private enum PagePosition
        {
            In = 0,
            OutL = -100,
            OutR = 100
        }

        /// <summary>
        /// ページプールへの参照（外部から RegisterPage するために公開）
        /// </summary>
        public DebugPagePool PagePool => _pagePool;

        public DebugPageNavigator(
            VisualElement contentContainer,
            VisualElement animationScheduler,
            Action<string> onLabelChanged,
            Action<bool> onBackVisibilityChanged)
        {
            _contentContainer = contentContainer;
            _animationScheduler = animationScheduler;
            _onLabelChanged = onLabelChanged;
            _onBackVisibilityChanged = onBackVisibilityChanged;
        }

        /// <summary>
        /// ルートページを初期化する。ウィンドウ生成時に一度だけ呼ぶ。
        /// </summary>
        public void InitRootPage(DebugPage rootPage)
        {
            if (rootPage == null) return;

            // name が未設定なら型名をフォールバックとして使用
            if (string.IsNullOrEmpty(rootPage.name))
            {
                rootPage.name = rootPage.GetType().Name;
            }

            _currentPage = rootPage;
            _onLabelChanged(rootPage.name);

            _pagePool.PreparePage(rootPage);
            _contentContainer.Add(rootPage);
            ShowPageImmediately(rootPage, PagePosition.In);
            rootPage.OnPageShown();
            NotifyBackVisibility();
        }

        /// <summary>
        /// 登録済みページへ遷移する
        /// </summary>
        public void Navigate(string pageName)
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
        public void NavigateTemp(string pageName, Action<IDebugPageBuilder> configure)
        {
            if (_isAnimating) return;
            if (_currentPage == null) return;

            var page = new GenericDebugPage(pageName, configure);
            _pagePool.PreparePage(page);
            OnNavigate(page);
        }

        /// <summary>
        /// 履歴を全て破棄してルートページへ戻る
        /// </summary>
        public void BackToRoot()
        {
            if (_isAnimating) return;
            if (_history.Count == 0) return;

            ++_animVersion;
            _isAnimating = true;

            // history を top→bottom 順の配列に変換（末尾がルートページ）
            var historyArray = _history.ToArray();
            var rootPage = historyArray[^1];
            _history.Clear();

            // ルート以外の中間ページをプールへ返却
            for (var i = 0; i < historyArray.Length - 1; i++)
            {
                historyArray[i].OnPageHidden();
                _pagePool.Return(historyArray[i]);
            }

            var currentPage = _currentPage;
            _currentPage = rootPage;
            _onLabelChanged(rootPage.name);

            // Back と同じ方向のアニメーション
            SlidePage(currentPage, PagePosition.In, PagePosition.OutR, AnimationDuration, () =>
            {
                currentPage.OnPageHidden();
                _pagePool.Return(currentPage);
            });
            SlidePage(rootPage, PagePosition.OutL, PagePosition.In, AnimationDuration, () =>
            {
                rootPage.OnPageShown();
                _isAnimating = false;
                NotifyBackVisibility();
            });
        }

        /// <summary>
        /// 前のページへ戻る
        /// </summary>
        public void Back()
        {
            if (_isAnimating) return;
            if (_history.Count == 0) return;

            ++_animVersion;
            _isAnimating = true;

            var prevPage = _history.Pop();
            var currentPage = _currentPage;

            _currentPage = prevPage;
            _onLabelChanged(prevPage.name);

            // アニメーション完了後にプールへ返却（スクロールリセットはページが画面外に出てから）
            SlidePage(currentPage, PagePosition.In, PagePosition.OutR, AnimationDuration, () =>
            {
                currentPage.OnPageHidden();
                _pagePool.Return(currentPage);
            });
            SlidePage(prevPage, PagePosition.OutL, PagePosition.In, AnimationDuration, () =>
            {
                prevPage.OnPageShown();
                _isAnimating = false;
                NotifyBackVisibility();
            });
        }

        /// <summary>
        /// 指定ページ名がプールに登録済みか返す
        /// </summary>
        public bool IsPageRegistered(string pageName) => _pagePool.Contains(pageName);

        // ── プライベート ────────────────────────────────────────

        private void OnNavigate(DebugPage targetPage)
        {
            if (targetPage.parent != _contentContainer)
            {
                _contentContainer.Add(targetPage);
            }

            _onLabelChanged(targetPage.name);

            var prevPage = _currentPage;
            _currentPage = targetPage;

            ++_animVersion;
            _isAnimating = true;
            if (prevPage.name == targetPage.name)
            {
                // 同一名ナビゲーション: 履歴にpushせず、アニメーション完了後にプールへ返却
                SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration, () =>
                {
                    prevPage.OnPageHidden();
                    _pagePool.Return(prevPage);
                });
            }
            else
            {
                _history.Push(prevPage);
                SlidePage(prevPage, PagePosition.In, PagePosition.OutL, AnimationDuration, () =>
                {
                    prevPage.OnPageHidden();
                });
            }

            SlidePage(targetPage, PagePosition.OutR, PagePosition.In, AnimationDuration, () =>
            {
                targetPage.OnPageShown();
                _isAnimating = false;
                NotifyBackVisibility();
            });
        }

        private void ShowPageImmediately(DebugPage page, PagePosition position)
        {
            page.style.left = new StyleLength(new Length((float)position, LengthUnit.Percent));
        }

        private void SlidePage(DebugPage page, PagePosition from, PagePosition to, float duration, Action onComplete = null)
        {
            var version = _animVersion;
            DebugMenuAnimator.Slide(page, _animationScheduler, (float)from, (float)to, duration,
                shouldCancel: () => _animVersion != version,
                onComplete);
        }

        private void NotifyBackVisibility()
        {
            _onBackVisibilityChanged(_history.Count > 0);
        }
    }
}
