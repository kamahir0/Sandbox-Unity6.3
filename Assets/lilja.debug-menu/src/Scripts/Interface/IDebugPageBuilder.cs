using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public interface IDebugPageBuilder
    {
        void VisualElement(VisualElement visualElement);

        DebugPageCache PageCache { get; }
    }

    public static class IDebugPageBuilderExtensions
    {
        public static void Button(this IDebugPageBuilder builder, string text)
        {
            var button = new DebugButton(text);
            builder.VisualElement(button);
        }

        public static void Foldout(this IDebugPageBuilder builder, string text, Action<IDebugPageBuilder> configure)
        {
            var foldout = new DebugFoldout(text);
            var pageCache = new DebugPageCache();
            var innerBuilder = new DebugPageBuilder(foldout, pageCache);
            configure(innerBuilder);
            builder.PageCache.Merge(pageCache);
            builder.VisualElement(foldout);
        }

        public static void NavigationButton<T>(this IDebugPageBuilder builder)
            where T : DebugPage, new()
        {
            builder.NavigationButton(typeof(T).Name, () => new T());
        }

        public static void NavigationButton<T>(this IDebugPageBuilder builder, string pageName, Func<T> pageFactory)
            where T : DebugPage
        {
            var button = new DebugButton();
            if (!builder.PageCache.Contains(pageName))
            {
                builder.PageCache.Add(pageName, pageFactory());
            }
            button.clicked += () => DebugMenuManager.Frame.Navigate(pageName);
            builder.VisualElement(button);
        }
    }

    internal sealed class DebugPageBuilder : IDebugPageBuilder
    {
        public VisualElement Parent { get; }
        public DebugPageCache PageCache { get; }

        public DebugPageBuilder(VisualElement parent, DebugPageCache pageCache)
        {
            Parent = parent;
            PageCache = pageCache;
        }

        public void VisualElement(VisualElement visualElement)
        {
            Parent.Add(visualElement);
        }
    }

    public sealed class DebugPageCache
    {
        private readonly Dictionary<string, DebugPage> _dictionary = new();

        public void Add(string pageName, DebugPage page)
        {
            try
            {
                _dictionary.Add(pageName, page);
            }
            catch { }
        }

        public bool Contains(string pageName) => _dictionary.ContainsKey(pageName);

        public bool TryGet(string pageName, out DebugPage page) => _dictionary.TryGetValue(pageName, out page);

        public void Merge(DebugPageCache other)
        {
            foreach (var (pageName, page) in other._dictionary)
            {
                _dictionary[pageName] = page;
            }
        }
    }
}