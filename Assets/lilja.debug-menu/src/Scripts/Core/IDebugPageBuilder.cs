using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public interface IDebugPageBuilder
    {
        void VisualElement(VisualElement visualElement);
    }

    internal sealed class DebugPageBuilder : IDebugPageBuilder
    {
        public VisualElement Parent { get; }
        internal DebugPagePool PagePool { get; }

        public DebugPageBuilder(VisualElement parent, DebugPagePool pagePool)
        {
            Parent = parent;
            PagePool = pagePool;
        }

        public void VisualElement(VisualElement visualElement)
        {
            Parent.Add(visualElement);
        }
    }
}
