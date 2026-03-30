using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    public enum DebugMenuButtonPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        CenterLeft,
        CenterRight,
    }

    /// <summary>
    /// 指定回数タップしたらデバッグメニューを開くボタン。
    /// UIDocument コンポーネントと同じ GameObject に配置し、
    /// Source UXML に DebugMenuOpenButton.uxml を設定して使う。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DebugMenuOpenButton : MonoBehaviour
    {
        private const string ButtonName = "debug-menu-open-button";
        private const string PressedClass = "c-open-button--pressed";
        private const string OverlayClass = "c-open-button__overlay";

        [Header("ボタン設定")]
        [SerializeField] private DebugMenuButtonPosition buttonPosition = DebugMenuButtonPosition.BottomLeft;
        [SerializeField, Min(0.01f)] private float thresholdSeconds = 0.5f;
        [SerializeField, Min(2)] private int requiredClicks = 3;

        private Button _button;
        private int _clickCount;
        private float _firstClickTime;

        private void Awake()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc.panelSettings == null) uiDoc.panelSettings = Resources.Load<PanelSettings>("DebugMenu/PanelSettings");
            if (uiDoc.visualTreeAsset == null) uiDoc.visualTreeAsset = Resources.Load<VisualTreeAsset>("DebugMenu/DebugMenuOpenButton");
        }

        private void Reset()
        {
            var uiDoc = GetComponent<UIDocument>();
            uiDoc.visualTreeAsset = Resources.Load<VisualTreeAsset>("DebugMenu/DebugMenuOpenButton");
            uiDoc.panelSettings = Resources.Load<PanelSettings>("DebugMenu/PanelSettings");
        }

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            _button = root.Q<Button>(ButtonName);

            if (_button == null)
            {
                Debug.LogError($"[DebugMenuOpenButton] Button '{ButtonName}' not found. UXML に DebugMenuOpenButton.uxml を設定してください。");
                return;
            }

            ApplyButtonPosition(root);

            _button.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _button.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _button.clicked += OnClicked;
        }

        private void OnDisable()
        {
            if (_button == null) return;

            _button.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _button.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _button.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _button.clicked -= OnClicked;
        }

        private void OnPointerDown(PointerDownEvent evt) => _button.AddToClassList(PressedClass);
        private void OnPointerUp(PointerUpEvent evt) => _button.RemoveFromClassList(PressedClass);
        private void OnPointerLeave(PointerLeaveEvent evt) => _button.RemoveFromClassList(PressedClass);

        private void OnClicked()
        {
            var now = Time.unscaledTime;

            if (_clickCount == 0 || now - _firstClickTime > thresholdSeconds)
            {
                _clickCount = 1;
                _firstClickTime = now;
                return;
            }

            _clickCount++;

            if (_clickCount >= requiredClicks)
            {
                _clickCount = 0;
                DebugMenuManager.Show();
            }
        }

        private void ApplyButtonPosition(VisualElement root)
        {
            var overlay = root.Q<VisualElement>(className: OverlayClass);
            if (overlay == null) return;

            var (justify, align) = buttonPosition switch
            {
                DebugMenuButtonPosition.TopLeft => (Justify.FlexStart, Align.FlexStart),
                DebugMenuButtonPosition.TopRight => (Justify.FlexStart, Align.FlexEnd),
                DebugMenuButtonPosition.BottomLeft => (Justify.FlexEnd, Align.FlexStart),
                DebugMenuButtonPosition.BottomRight => (Justify.FlexEnd, Align.FlexEnd),
                DebugMenuButtonPosition.CenterLeft => (Justify.Center, Align.FlexStart),
                DebugMenuButtonPosition.CenterRight => (Justify.Center, Align.FlexEnd),
                _ => (Justify.FlexEnd, Align.FlexStart),
            };

            overlay.style.justifyContent = justify;
            overlay.style.alignItems = align;
        }
    }
}
