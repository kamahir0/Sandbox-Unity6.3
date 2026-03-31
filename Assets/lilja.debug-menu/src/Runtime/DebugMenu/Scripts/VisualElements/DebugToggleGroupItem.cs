using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// DebugToggleGroup 用のチェックボックス
    /// </summary>
    [UxmlElement]
    public partial class DebugToggleGroupItem : VisualElement, INotifyValueChanged<bool>
    {
        // UI
        private readonly Toggle _checkbox;
        private readonly Label _label;

        private bool _value;
        private DebugToggleGroup _registeredGroup;

        // クラス
        private const string UssClassName = "c-toggle-group-item";
        private const string CheckedUssClassName = UssClassName + "--checked";
        private const string CheckboxUssClassName = UssClassName + "__checkbox";
        private const string LabelUssClassName = UssClassName + "__label";

        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                var previous = _value;
                SetValueWithoutNotify(value);
                using var evt = ChangeEvent<bool>.GetPooled(previous, _value);
                evt.target = this;
                SendEvent(evt);
            }
        }

        /// <inheritdoc/>
        bool INotifyValueChanged<bool>.value
        {
            get => Value;
            set => Value = value;
        }

        public DebugToggleGroupItem() : this(string.Empty) { }

        public DebugToggleGroupItem(string text)
        {
            AddToClassList(UssClassName);
            focusable = true;

            // チェックボックス
            _checkbox = new Toggle();
            _checkbox.AddToClassList(CheckboxUssClassName);
            _checkbox.pickingMode = PickingMode.Ignore;
            _checkbox.focusable = false;
            Add(_checkbox);

            // ラベル
            _label = new Label();
            _label.AddToClassList(LabelUssClassName);
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);
            Label = text;

            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        /// <inheritdoc/>
        public void SetValueWithoutNotify(bool v)
        {
            _value = v;
            _checkbox.SetValueWithoutNotify(v);
            EnableInClassList(CheckedUssClassName, v);
        }

        private void OnClick(ClickEvent e)
        {
            Value = !_value;
        }

        private void OnAttach(AttachToPanelEvent e)
        {
            _registeredGroup = GetFirstAncestorOfType<DebugToggleGroup>();
            _registeredGroup?.RegisterItem(this);
        }

        private void OnDetach(DetachFromPanelEvent e)
        {
            _registeredGroup?.UnregisterItem(this);
            _registeredGroup = null;
        }
    }
}
