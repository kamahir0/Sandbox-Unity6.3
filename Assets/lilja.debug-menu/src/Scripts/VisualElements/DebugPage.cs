using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニュー用のページ
    /// </summary>
    [UxmlElement]
    public partial class DebugPage : VisualElement
    {
        #region Virtual
        public virtual void Configure(IDebugPageBuilder builder) { }
        #endregion

        // UI
        private readonly ScrollView _scrollView;

        /// <inheritdoc/>
        public override VisualElement contentContainer => _scrollView.contentContainer;

        public DebugPage() : base()
        {
            AddToClassList("t-surface");
            AddToClassList("c-page");

            // スクロースビュー
            _scrollView = new ScrollView();
            _scrollView.AddToClassList("c-scroll-view");
            hierarchy.Add(_scrollView);
        }

        /// <summary>
        /// スクロール位置をリセットする
        /// </summary>
        public void ResetScrollPosition()
        {
            _scrollView.scrollOffset = Vector2.zero;
        }
    }
}
