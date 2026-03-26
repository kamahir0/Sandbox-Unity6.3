using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public class DebugMenuManager
    {
        public static DebugMenuFrame Frame;
        private static DebugMenuRoot _menuRoot;

        public static void Initialize(UIDocument uiDocument, DebugPage rootPage)
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();

            // DebugMenuRoot
            var menuRoot = new DebugMenuRoot();
            root.Add(menuRoot);
            _menuRoot = menuRoot;

            // DebugMenuFrame
            var frame = new DebugMenuFrame(rootPage);
            frame.AddToClassList("c-menu-frame--default-size");
            menuRoot.Add(frame);

            Frame = frame;

            // 初期状態は非表示
            Hide();

            // 矩形外タップで閉じる
            menuRoot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (Frame != null && !Frame.worldBound.Contains(evt.position))
                {
                    Hide();
                }
            }, TrickleDown.TrickleDown);
        }

        public static void Show()
        {
            if (Frame == null || _menuRoot == null) return;
            Frame.style.display = DisplayStyle.Flex;
            _menuRoot.pickingMode = PickingMode.Position;
        }

        public static void Hide()
        {
            if (Frame == null || _menuRoot == null) return;
            Frame.style.display = DisplayStyle.None;
            _menuRoot.pickingMode = PickingMode.Ignore;
        }
    }
}
