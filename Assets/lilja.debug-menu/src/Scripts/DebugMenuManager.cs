using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public class DebugMenuManager
    {
        public static DebugMenuFrame Frame;

        public static void Initialize(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();

            // DebugMenuRoot
            var menuRoot = new DebugMenuRoot();
            root.Add(menuRoot);

            // DebugMenuFrame
            var frame = new DebugMenuFrame(new RootPage());
            frame.AddToClassList("c-menu-frame--default-size");
            menuRoot.Add(frame);

            Frame = frame;
        }
    }

    public class RootPage : DebugPage
    {
        public override void Configure(IDebugPageBuilder builder)
        {
            builder.Button("次へ2");
            builder.Foldout("折り畳み", b =>
            {
                b.Button("次へ3");
            });
            builder.Button("次へ2");
            builder.NavigationButton<MockBuilder.Page1>();
        }
    }
}