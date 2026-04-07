using System;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    public interface IDebugUIBuilder
    {
        void VisualElement(VisualElement visualElement);
        IDebugUIBuilder CreateChildBuilder(VisualElement parent);

        /// <summary>
        /// ページをプールに登録する。NavigationButton から利用される。
        /// </summary>
        void RegisterPage(string pageName, Func<DebugPage> factory);
    }

    internal sealed class DebugUIBuilder : IDebugUIBuilder
    {
        private readonly VisualElement _parent;
        private readonly DebugPageCache _pageCache;

        public DebugUIBuilder(VisualElement parent, DebugPageCache pageCache)
        {
            _parent = parent;
            _pageCache = pageCache;
        }

        public void VisualElement(VisualElement visualElement)
        {
            _parent.Add(visualElement);
        }

        public IDebugUIBuilder CreateChildBuilder(VisualElement parent)
        {
            return new DebugUIBuilder(parent, _pageCache);
        }

        public void RegisterPage(string pageName, Func<DebugPage> factory)
        {
            _pageCache.Register(pageName, factory);
        }
    }
}
