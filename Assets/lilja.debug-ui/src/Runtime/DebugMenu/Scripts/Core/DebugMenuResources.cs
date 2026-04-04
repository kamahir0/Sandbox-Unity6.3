using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    internal static class DebugMenuResources
    {
        private const string DefaultPanelSettingsPath = "LiljaDebugMenu/DefaultPanelSettings";
        private const string OpenButtonVisualTreePath = "LiljaDebugMenu/DebugMenuOpenButton";

        public static PanelSettings LoadDefaultPanelSettings()
            => Resources.Load<PanelSettings>(DefaultPanelSettingsPath);

        public static VisualTreeAsset LoadOpenButtonVisualTree()
            => Resources.Load<VisualTreeAsset>(OpenButtonVisualTreePath);
    }
}
