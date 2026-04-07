using System;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    /// <summary>
    /// ボタンの系統を表す列挙型
    /// </summary>
    public enum ButtonType
    {
        /// <summary>プライマリ（決定・適用など）: 青</summary>
        Primary,
        /// <summary>セカンダリ（キャンセル・戻るなど）: 白</summary>
        Secondary,
        /// <summary>デンジャー（削除・リセットなど破壊的操作）: 赤</summary>
        Danger,
    }

    public static class IDebugUIBuilderExtensions
    {
        public static void Button(this IDebugUIBuilder builder, string text, ButtonType buttonType = ButtonType.Primary)
        {
            builder.VisualElement(buttonType switch
            {
                ButtonType.Secondary => new DebugSecondaryButton(text),
                ButtonType.Danger => new DebugDangerButton(text),
                _ => new DebugButton(text)
            });
        }

        public static void NavigationButton<T>(this IDebugUIBuilder builder, StyleBackground? icon = null)
            where T : DebugPage, new()
        {
            builder.NavigationButton(typeof(T).Name, () => new T(), icon);
        }

        public static void NavigationButton<T>(this IDebugUIBuilder builder, string pageName, Func<T> pageFactory, StyleBackground? icon = null)
            where T : DebugPage
        {
            builder.RegisterPage(pageName, () => pageFactory());

            var button = new DebugNavigationButton(pageName, icon);
            button.clicked += () => DebugMenu.NavigateTo(pageName);
            builder.VisualElement(button);
        }

        public static void NavigationButton(this IDebugUIBuilder builder, string pageName, Action<IDebugUIBuilder> configure, StyleBackground? icon = null)
        {
            builder.NavigationButton(pageName, () => new GenericDebugPage(pageName, configure), icon);
        }

        public static void Foldout(this IDebugUIBuilder builder, string text, Action<IDebugUIBuilder> configure)
        {
            var foldout = new DebugFoldout(text);
            configure(builder.CreateChildBuilder(foldout));
            builder.VisualElement(foldout);
        }

        public static void HorizontalScope(this IDebugUIBuilder builder, Action<IDebugUIBuilder> configure)
        {
            var row = new VisualElement();
            row.AddToClassList(DebugMenuUssClass.HorizontalScope);
            configure(new HorizontalScopeBuilder(builder.CreateChildBuilder(row)));
            builder.VisualElement(row);
        }

        private sealed class HorizontalScopeBuilder : IDebugUIBuilder
        {
            private readonly IDebugUIBuilder _inner;

            public HorizontalScopeBuilder(IDebugUIBuilder inner) => _inner = inner;

            public void VisualElement(VisualElement visualElement)
            {
                visualElement.style.flexBasis = new StyleLength(new Length(0));
                visualElement.style.flexGrow = 1f;
                _inner.VisualElement(visualElement);
            }

            public IDebugUIBuilder CreateChildBuilder(VisualElement parent)
                => _inner.CreateChildBuilder(parent);

            public void RegisterPage(string pageName, Func<DebugPage> factory)
                => _inner.RegisterPage(pageName, factory);
        }

    }
}
