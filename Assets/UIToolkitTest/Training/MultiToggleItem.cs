using System;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// MultiToggleGroup 内で使用するカスタムトグル項目。
    ///
    /// 標準の Toggle はチェックボックスとラベルが内部で結合しており、
    /// 両者の間隔を USS から制御することが困難なため、
    /// 「チェックボックス専用の Toggle」と「テキスト Label」を
    /// 独立した兄弟要素として並べる構造を採用する。
    ///
    /// 間隔は CSS 変数 --multi-toggle-item-gap で USS から調整可能。
    ///   例) .my-container { --multi-toggle-item-gap: 4px; }
    /// </summary>
    public class MultiToggleItem : VisualElement
    {
        public static readonly string ussClassName         = "multi-toggle-item";
        public static readonly string checkboxUssClassName = ussClassName + "__checkbox";
        public static readonly string labelUssClassName    = ussClassName + "__label";

        private readonly Toggle _checkbox;
        private readonly Label  _textLabel;

        public bool value => _checkbox.value;

        public event Action<bool> onValueChanged;

        public MultiToggleItem(string text)
        {
            AddToClassList(ussClassName);

            // Toggle はチェックボックス部分のみとして使用。
            // 内蔵ラベルは USS で非表示にするため、テキストは渡さない。
            _checkbox = new Toggle();
            _checkbox.AddToClassList(checkboxUssClassName);
            _checkbox.RegisterValueChangedCallback(e => onValueChanged?.Invoke(e.newValue));
            Add(_checkbox);

            // テキスト表示は独立した Label で行う。
            // クリックでチェックボックスをトグルし、ユーザー体験を Toggle と同等にする。
            _textLabel = new Label(text);
            _textLabel.AddToClassList(labelUssClassName);
            _textLabel.RegisterCallback<ClickEvent>(_ => _checkbox.value = !_checkbox.value);
            Add(_textLabel);
        }

        public void SetValueWithoutNotify(bool v) => _checkbox.SetValueWithoutNotify(v);
    }
}
