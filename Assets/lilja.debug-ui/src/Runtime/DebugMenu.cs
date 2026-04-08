using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    public static class DebugMenu
    {
        private static DebugMenuWindow _window;
        private static DebugMenuRoot _menuRoot;
        private static int _animVersion;

        internal static DebugPageCache CurrentCache => _window?.PageCache;

        public static void Initialize(DebugPage rootPage, PanelSettings panelSettings = null)
        {
            var go = new GameObject("[DebugMenu]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var uiDoc = go.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings != null
                ? panelSettings
                : DebugMenuResources.LoadDefaultPanelSettings();
            Initialize(uiDoc, rootPage);
        }

        public static void Initialize(UIDocument uiDocument, DebugPage rootPage)
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();

            var menuRoot = new DebugMenuRoot();
            root.Add(menuRoot);
            _menuRoot = menuRoot;

            var window = new DebugMenuWindow(rootPage);
            menuRoot.Add(window);
            _window = window;

            // 初期状態は即時非表示
            window.SetHidden();
            menuRoot.pickingMode = PickingMode.Ignore;

            // 矩形外タップで閉じる
            menuRoot.SetupOutsideTapHandler(() => _window, Hide);
        }

        public static void Show()
        {
            if (_window == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Position;
            _window.style.translate = StyleKeyword.None;

            var version = ++_animVersion;
            DebugMenuAnimator.AnimateScaleOpacity(
                _window,
                scaleFrom: DebugMenuSettings.HideScale, scaleTo: 1f,
                opacityFrom: 0f, opacityTo: 1f,
                duration: DebugMenuSettings.ShowDuration,
                easing: DebugMenuAnimator.EaseOutCubic,
                shouldCancel: () => _animVersion != version,
                onComplete: null
            );
        }

        public static void Hide()
        {
            if (_window == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Ignore;

            var version = ++_animVersion;
            DebugMenuAnimator.AnimateScaleOpacity(
                _window,
                scaleFrom: 1f, scaleTo: DebugMenuSettings.HideScale,
                opacityFrom: 1f, opacityTo: 0f,
                duration: DebugMenuSettings.HideDuration,
                easing: DebugMenuAnimator.EaseInCubic,
                shouldCancel: () => _animVersion != version,
                onComplete: _window.SetHidden
            );
        }

        public static void ResetPosition()
        {
            if (_window != null)
            {
                _window.ResetPosition();
            }
            else
            {
                DebugMenuPositionController.ClearSavedPosition();
            }
        }

        public static void NavigateTo(string pageName)
        {
            if (_window == null) return;
            if (!_window.IsPageRegistered(pageName))
            {
                Debug.LogError($"[DebugMenuManager] Page '{pageName}' is not registered. Use NavigationButton or RegisterPage to register it first.");
                return;
            }
            _window.Navigate(pageName);
        }

        public static void Back()
        {
            _window?.Back();
        }

        public static void BackToRoot()
        {
            _window?.BackToRoot();
        }

        public static void NavigateToTemp(string pageName, Action<IDebugUIBuilder> configure)
        {
            _window?.NavigateTemp(pageName, configure);
        }

        public static DebugPage GetPage(string pageName)
        {
            return _window?.GetPage(pageName);
        }

        public static T GetPage<T>() where T : DebugPage
        {
            return _window?.GetPage(typeof(T).Name) as T;
        }
    }
}
