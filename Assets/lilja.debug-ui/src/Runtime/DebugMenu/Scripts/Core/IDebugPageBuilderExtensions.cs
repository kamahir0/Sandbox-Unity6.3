using System;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    public static class IDebugPageBuilderExtensions
    {
        /// <summary>
        /// 子要素を横並びにするスコープを作る。スコープ内に追加した要素には flex-grow: 1 が自動付与される。
        /// </summary>
        public static void HorizontalScope(this IDebugPageBuilder builder, Action<IDebugPageBuilder> configure)
        {
            var row = new VisualElement();
            row.AddToClassList(DebugMenuUssClass.HorizontalScope);
            configure(new HorizontalScopeBuilder(builder.CreateChildBuilder(row)));
            builder.VisualElement(row);
        }

        /// <summary>
        /// HorizontalScope 内専用のビルダー。追加する要素に flex-grow: 1 を自動付与する。
        /// </summary>
        private sealed class HorizontalScopeBuilder : IDebugPageBuilder
        {
            private readonly IDebugPageBuilder _inner;

            public HorizontalScopeBuilder(IDebugPageBuilder inner) => _inner = inner;

            public void VisualElement(VisualElement visualElement)
            {
                visualElement.style.flexBasis = new StyleLength(new Length(0));
                visualElement.style.flexGrow = 1f;
                _inner.VisualElement(visualElement);
            }

            public IDebugPageBuilder CreateChildBuilder(VisualElement parent)
                => _inner.CreateChildBuilder(parent);

            public void RegisterPage(string pageName, Func<DebugPage> factory)
                => _inner.RegisterPage(pageName, factory);
        }


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

        public static void NavigationButton<T>(this IDebugPageBuilder builder, StyleBackground? icon = null)
            where T : DebugPage, new()
        {
            builder.NavigationButton(typeof(T).Name, () => new T(), icon);
        }

        public static void NavigationButton<T>(this IDebugPageBuilder builder, string pageName, Func<T> pageFactory, StyleBackground? icon = null)
            where T : DebugPage
        {
            builder.RegisterPage(pageName, () => pageFactory());

            var button = new DebugNavigationButton(pageName, icon);
            button.clicked += () => DebugMenu.NavigateTo(pageName);
            builder.VisualElement(button);
        }

        /// <summary>
        /// ラムダ式でUIを構成する汎用ページへのナビゲーションボタンを追加する。
        /// </summary>
        public static void NavigationButton(this IDebugPageBuilder builder, string pageName, Action<IDebugPageBuilder> configure, StyleBackground? icon = null)
        {
            builder.NavigationButton(pageName, () => new GenericDebugPage(pageName, configure), icon);
        }
    }
}
