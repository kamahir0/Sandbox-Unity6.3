using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public class DebugMenuManager
    {
        private static DebugMenuFrame _frame;
        private static DebugMenuRoot _menuRoot;
        private static int _animVersion;

        private const float ShowDuration = 0.2f;
        private const float HideDuration = 0.15f;
        private const float HideScale = 0.9f;

        /// <summary>
        /// UIDocument を自動生成して初期化する簡易版。
        /// PanelSettings を省略するとパッケージ付属のデフォルトを使用する。
        /// </summary>
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

            var frame = new DebugMenuFrame(rootPage);
            menuRoot.Add(frame);
            _frame = frame;

            // 初期状態は即時非表示（アニメーションなし）
            frame.SetHidden();

            // 矩形外タップで閉じる（DebugMenuRoot に委譲）
            menuRoot.SetupOutsideTapHandler(() => _frame, Hide);
        }

        public static void Show()
        {
            if (_frame == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Position;
            _frame.style.display = DisplayStyle.Flex;

            var version = ++_animVersion;
            DebugMenuAnimator.AnimateScaleOpacity(
                _frame,
                scaleFrom: HideScale, scaleTo: 1f,
                opacityFrom: 0f, opacityTo: 1f,
                duration: ShowDuration,
                easing: DebugMenuAnimator.EaseOutCubic,
                shouldCancel: () => _animVersion != version,
                onComplete: null
            );
        }

        public static void Hide()
        {
            if (_frame == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Ignore;

            var version = ++_animVersion;
            DebugMenuAnimator.AnimateScaleOpacity(
                _frame,
                scaleFrom: 1f, scaleTo: HideScale,
                opacityFrom: 1f, opacityTo: 0f,
                duration: HideDuration,
                easing: DebugMenuAnimator.EaseInCubic,
                shouldCancel: () => _animVersion != version,
                onComplete: _frame.SetHidden
            );
        }

        /// <summary>
        /// 事前登録済みのページへナビゲートする。未登録の場合は LogError を出して何もしない。
        /// </summary>
        public static void NavigateTo(string pageName)
        {
            if (_frame == null) return;
            if (!_frame.IsPageRegistered(pageName))
            {
                Debug.LogError($"[DebugMenuManager] Page '{pageName}' is not registered. Use NavigationButton or RegisterPage to register it first.");
                return;
            }
            _frame.Navigate(pageName);
        }

        public static void Back()
        {
            _frame?.Back();
        }

        /// <summary>
        /// GenericDebugPage を即席生成してナビゲートする。事前登録不要。
        /// 主に動的コンテンツや GenericDebugPage を使うケース向け。
        /// </summary>
        public static void NavigateToTemp(string pageName, Action<IDebugPageBuilder> configure)
        {
            _frame?.NavigateTemp(pageName, configure);
        }

    }
}
