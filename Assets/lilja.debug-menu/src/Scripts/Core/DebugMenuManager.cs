using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public class DebugMenuManager
    {
        public static DebugMenuFrame Frame;
        private static DebugMenuRoot _menuRoot;
        private static int _animVersion;

        private const float ShowDuration = 0.2f;
        private const float HideDuration = 0.15f;
        private const float HideScale = 0.9f;

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

            // 初期状態は即時非表示（アニメーションなし）
            ApplyHiddenState();

            // 矩形外タップで閉じる
            menuRoot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (Frame != null && !Frame.worldBound.Contains(evt.position))
                    Hide();
            }, TrickleDown.TrickleDown);
        }

        public static void Show()
        {
            if (Frame == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Position;
            Frame.style.display = DisplayStyle.Flex;

            Animate(
                scaleFrom: HideScale, scaleTo: 1f,
                opacityFrom: 0f, opacityTo: 1f,
                duration: ShowDuration,
                easing: EaseOutCubic,
                onComplete: null
            );
        }

        public static void Hide()
        {
            if (Frame == null || _menuRoot == null) return;

            _menuRoot.pickingMode = PickingMode.Ignore;

            Animate(
                scaleFrom: 1f, scaleTo: HideScale,
                opacityFrom: 1f, opacityTo: 0f,
                duration: HideDuration,
                easing: EaseInCubic,
                onComplete: ApplyHiddenState
            );
        }

        private static void ApplyHiddenState()
        {
            Frame.style.display = DisplayStyle.None;
            Frame.style.opacity = 0f;
            Frame.style.scale = new Scale(new Vector3(HideScale, HideScale, 1f));
        }

        private static void Animate(
            float scaleFrom, float scaleTo,
            float opacityFrom, float opacityTo,
            float duration, Func<float, float> easing,
            Action onComplete)
        {
            var version = ++_animVersion;
            var elapsed = 0f;

            Frame.style.scale = new Scale(new Vector3(scaleFrom, scaleFrom, 1f));
            Frame.style.opacity = opacityFrom;

            Frame.schedule.Execute(timer =>
            {
                if (_animVersion != version) return;

                elapsed += timer.deltaTime / 1000f;
                var t = easing(Mathf.Clamp01(elapsed / duration));

                var s = Mathf.Lerp(scaleFrom, scaleTo, t);
                Frame.style.scale = new Scale(new Vector3(s, s, 1f));
                Frame.style.opacity = Mathf.Lerp(opacityFrom, opacityTo, t);

                if (elapsed >= duration)
                {
                    Frame.style.scale = new Scale(new Vector3(scaleTo, scaleTo, 1f));
                    Frame.style.opacity = opacityTo;
                    onComplete?.Invoke();
                }
            }).Every(0).Until(() => elapsed >= duration || _animVersion != version);
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInCubic(float t) => t * t * t;
    }
}
