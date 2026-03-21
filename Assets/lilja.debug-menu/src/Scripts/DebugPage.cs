using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニュー用のページ
    /// </summary>
    [UxmlElement]
    public partial class DebugPage : VisualElement
    {
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
    }
}
