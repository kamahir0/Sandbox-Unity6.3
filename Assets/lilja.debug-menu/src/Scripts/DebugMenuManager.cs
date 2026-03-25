using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public class DebugMenuManager
    {
        public static DebugMenuFrame Frame;

        public static void Initialize(UIDocument uiDocument, DebugPage rootPage)
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();

            // DebugMenuRoot
            var menuRoot = new DebugMenuRoot();
            root.Add(menuRoot);

            // DebugMenuFrame
            var frame = new DebugMenuFrame(rootPage);
            frame.AddToClassList("c-menu-frame--default-size");
            menuRoot.Add(frame);

            Frame = frame;
        }
    }
}