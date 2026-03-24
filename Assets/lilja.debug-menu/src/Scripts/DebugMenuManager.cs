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
            var frame = new DebugMenuFrame();
            frame.AddToClassList("c-menu-frame--default-size");
            menuRoot.Add(frame);
        }
    }
}