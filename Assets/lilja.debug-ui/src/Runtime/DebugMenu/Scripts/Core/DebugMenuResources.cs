using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    internal static class DebugMenuResources
    {
        private const string DefaultPanelSettingsPath = "DebugMenu/DebugMenuPanelSettings";
        private const string OpenButtonVisualTreePath = "DebugMenu/DebugMenuOpenButton";

        public static PanelSettings LoadDefaultPanelSettings()
            => Resources.Load<PanelSettings>(DefaultPanelSettingsPath);

        public static VisualTreeAsset LoadOpenButtonVisualTree()
            => Resources.Load<VisualTreeAsset>(OpenButtonVisualTreePath);
    }
}
