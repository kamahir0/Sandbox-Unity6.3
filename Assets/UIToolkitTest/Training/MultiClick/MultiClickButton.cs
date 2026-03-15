using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitTest.Training.MultiClick
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MultiClickButton : MonoBehaviour
    {
        private const string ButtonName = "multi-click-button";
        private const string PressedClass = "multi-click-button--pressed";

        [SerializeField, Min(0.01f)] private float thresholdSeconds = 0.5f;
        [SerializeField, Min(2)] private int requiredClicks = 3;

        private Button _button;
        private int _clickCount;
        private float _firstClickTime;

        public event Action OnMultiClick;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            _button = root.Q<Button>(ButtonName);

            if (_button == null)
            {
                Debug.LogError($"[MultiClickButton] Button '{ButtonName}' not found in UXML.");
                return;
            }

            _button.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _button.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _button.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _button.clicked += OnClicked;

            OnMultiClick += HandleMultiClick;
        }

        private void OnDisable()
        {
            if (_button == null) return;

            _button.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _button.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _button.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _button.clicked -= OnClicked;

            OnMultiClick -= HandleMultiClick;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _button.AddToClassList(PressedClass);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _button.RemoveFromClassList(PressedClass);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _button.RemoveFromClassList(PressedClass);
        }

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
                OnMultiClick?.Invoke();
            }
        }

        private static void HandleMultiClick()
        {
            Debug.Log("[MultiClickButton] Multi-click triggered!");
        }
    }
}
