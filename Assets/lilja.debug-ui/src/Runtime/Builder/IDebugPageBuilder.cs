using System;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    public interface IDebugPageBuilder
    {
        void VisualElement(VisualElement visualElement);
        IDebugPageBuilder CreateChildBuilder(VisualElement parent);

        /// <summary>
        /// ページをプールに登録する。NavigationButton から利用される。
        /// </summary>
        void RegisterPage(string pageName, Func<DebugPage> factory);
    }

    internal sealed class DebugPageBuilder : IDebugPageBuilder
    {
        private readonly VisualElement _parent;
        private readonly DebugPageCache _pageCache;

        public DebugPageBuilder(VisualElement parent, DebugPageCache pageCache)
        {
            _parent = parent;
            _pageCache = pageCache;
        }

        public void VisualElement(VisualElement visualElement)
        {
            _parent.Add(visualElement);
        }

        public IDebugPageBuilder CreateChildBuilder(VisualElement parent)
        {
            return new DebugPageBuilder(parent, _pageCache);
        }

        public void RegisterPage(string pageName, Func<DebugPage> factory)
        {
            _pageCache.Register(pageName, factory);
        }
    }
}
