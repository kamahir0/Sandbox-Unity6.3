using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    [UxmlElement]
    public partial class DebugPage : VisualElement
    {
        private readonly ScrollView _scrollView;

        public override VisualElement contentContainer => _scrollView.contentContainer;

        public DebugPage() : base()
        {
            _scrollView = new ScrollView();
            _scrollView.AddToClassList("c-scroll-view");
            hierarchy.Add(_scrollView);
        }
    }
}
