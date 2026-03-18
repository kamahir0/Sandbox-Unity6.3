using System;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// DebugToggleGroup 内で使用するトグル項目。
    /// Unity 標準の Toggle を使わずカスタム VisualElement で実装することで
    /// em ベースのサイズスケール問題を回避している。
    ///
    /// DebugToggleGroup の子として UXML に配置して使用する:
    ///   &lt;debug:DebugToggleGroup label="設定"&gt;
    ///     &lt;debug:DebugToggleGroupItem text="オプションA" /&gt;
    ///   &lt;/debug:DebugToggleGroup&gt;
    ///
    /// DebugToggleGroup 外でも単体使用可能。
    /// </summary>
    [UxmlElement]
    public partial class DebugToggleGroupItem : VisualElement
    {
        public static readonly string ussClassName = "debug-toggle-group-item";
        public static readonly string checkedUssClassName = ussClassName + "--checked";
        public static readonly string checkboxUssClassName = ussClassName + "__checkbox";
        public static readonly string labelUssClassName = ussClassName + "__label";

        private readonly Toggle _checkbox;
        private readonly Label _textLabel;
        private bool _value;

        // 親グループへの参照。Attach/Detach 時に使用する
        private DebugToggleGroup _registeredGroup;

        // ----------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------

        [UxmlAttribute]
        public string Text
        {
            get => _textLabel.text;
            set => _textLabel.text = value;
        }

        public bool value
        {
            get => _value;
            set => SetValue(value, notify: true);
        }

        // ----------------------------------------------------------------
        // Events
        // ----------------------------------------------------------------

        public event Action<bool> onValueChanged;

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public DebugToggleGroupItem() : this(string.Empty) { }

        public DebugToggleGroupItem(string text)
        {
            AddToClassList(ussClassName);
            focusable = true;

            // チェックボックスインジケーター（Unity標準 Toggle でチェックマーク画像を使用）
            _checkbox = new Toggle();
            _checkbox.AddToClassList(checkboxUssClassName);
            _checkbox.pickingMode = PickingMode.Ignore;
            _checkbox.focusable = false;
            Add(_checkbox);

            // テキストラベル
            _textLabel = new Label();
            _textLabel.AddToClassList(labelUssClassName);
            _textLabel.pickingMode = PickingMode.Ignore;
            Add(_textLabel);
            Text = text;

            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        public void SetValueWithoutNotify(bool v) => SetValue(v, notify: false);

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------

        private void SetValue(bool v, bool notify)
        {
            _value = v;
            _checkbox.SetValueWithoutNotify(v);
            EnableInClassList(checkedUssClassName, v);
            if (notify) onValueChanged?.Invoke(v);
        }

        private void OnClick(ClickEvent e) => value = !_value;

        private void OnAttach(AttachToPanelEvent e)
        {
            _registeredGroup = GetFirstAncestorOfType<DebugToggleGroup>();
            _registeredGroup?.RegisterItem(this);
        }

        private void OnDetach(DetachFromPanelEvent e)
        {
            // DetachFromPanel 時は既に hierarchy から外れている可能性があるため
            // 登録時に保持した参照を使う
            _registeredGroup?.UnregisterItem(this);
            _registeredGroup = null;
        }
    }
}
