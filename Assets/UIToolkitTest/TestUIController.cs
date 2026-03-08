using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkitTest
{
    /// <summary>
    /// UIToolkitのテストUIを制御するコントローラークラス
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    internal class TestUIController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Label _outputLabel;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

            // 各種UI要素の取得とイベント登録
            var button = root.Q<Button>("test-button");
            button.clicked += () => LogInteraction("Button Clicked!");

            var toggle = root.Q<Toggle>("test-toggle");
            toggle.RegisterValueChangedCallback(evt => LogInteraction($"Toggle: {evt.newValue}"));

            var textField = root.Q<TextField>("test-textfield");
            textField.RegisterValueChangedCallback(evt => LogInteraction($"Text: {evt.newValue}"));

            var slider = root.Q<Slider>("test-slider");
            slider.RegisterValueChangedCallback(evt => LogInteraction($"Slider: {evt.newValue:F1}"));

            var dropdown = root.Q<DropdownField>("test-dropdown");
            dropdown.RegisterValueChangedCallback(evt => LogInteraction($"Dropdown: {evt.newValue}"));

            _outputLabel = root.Q<Label>("output-label");
        }

        private void LogInteraction(string message)
        {
            Debug.Log($"[UI Test] {message}");
            if (_outputLabel != null)
            {
                _outputLabel.text = $"Interaction Log: {message}";
            }
        }
    }
}
