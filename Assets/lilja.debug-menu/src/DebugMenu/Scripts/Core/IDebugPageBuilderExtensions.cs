using System;

namespace Lilja.DebugMenu
{
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
            configure(builder.CreateChildBuilder(foldout));
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
            var b = (DebugPageBuilder)builder;
            b.PagePool.Register(pageName, () => pageFactory());

            var button = new DebugButton(pageName);
            button.clicked += () => DebugMenuManager.NavigateTo(pageName);
            builder.VisualElement(button);
        }

        /// <summary>
        /// ラムダ式でUIを構成する汎用ページへのナビゲーションボタンを追加する。
        /// </summary>
        public static void NavigationButton(this IDebugPageBuilder builder, string pageName, Action<IDebugPageBuilder> configure)
        {
            builder.NavigationButton(pageName, () => new GenericDebugPage(pageName, configure));
        }
    }
}
