using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public interface IDebugPageBuilder
    {
        void VisualElement(VisualElement visualElement);
        IDebugPageBuilder CreateChildBuilder(VisualElement parent);
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

        public IDebugPageBuilder CreateChildBuilder(VisualElement parent)
        {
            return new DebugPageBuilder(parent, PagePool);
        }
    }
}
