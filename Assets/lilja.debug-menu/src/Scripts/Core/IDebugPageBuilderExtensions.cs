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
            var innerPool = new DebugPagePool();
            var innerBuilder = new DebugPageBuilder(foldout, innerPool);
            configure(innerBuilder);
            builder.PagePool.Merge(innerPool);
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
            if (!builder.PagePool.Contains(pageName))
            {
                // 循環防止マーカー設置
                builder.PagePool.Reserve(pageName);

                // ファクトリ登録: 生成時に必ず name をセット
                builder.PagePool.RegisterFactory(pageName, () =>
                {
                    var p = pageFactory();
                    p.name = pageName;
                    return p;
                });

                // 初回インスタンス作成 → name設定 → Configure → プールに追加
                var page = pageFactory();
                page.name = pageName;
                page.Configure(new DebugPageBuilder(page, builder.PagePool));
                builder.PagePool.Add(pageName, page);
            }

            var button = new DebugButton(pageName);
            button.clicked += () => DebugMenuManager.Frame.Navigate(pageName);
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
