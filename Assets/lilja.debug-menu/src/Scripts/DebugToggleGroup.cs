using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ToggleButtonGroup と同様の構造で複数選択が可能なカスタムトグルグループ。
    /// 選択肢を choices 属性ではなく、子要素 DebugToggleGroupItem として配置する。
    ///
    /// UXML での使い方:
    ///   &lt;debug:DebugToggleGroup label="有効機能"&gt;
    ///     &lt;debug:DebugToggleGroupItem text="オプションA" /&gt;
    ///     &lt;debug:DebugToggleGroupItem text="オプションB" /&gt;
    ///   &lt;/debug:DebugToggleGroup&gt;
    /// </summary>
    [UxmlElement]
    public partial class DebugToggleGroup : VisualElement
    {
        public static readonly string ussClassName = "debug-toggle-group";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string itemsUssClassName = ussClassName + "__items";

        private readonly Label _labelElement;
        private readonly VisualElement _itemsContainer;
        private readonly List<DebugToggleGroupItem> _items = new();

        public override VisualElement contentContainer => _itemsContainer;

        // ----------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------

        [UxmlAttribute]
        public string label
        {
            get => _labelElement.text;
            set => _labelElement.text = value;
        }

        /// <summary>現在チェックされている項目のテキスト一覧。</summary>
        public IReadOnlyList<string> value
        {
            get
            {
                var selected = new List<string>();
                foreach (var item in _items)
                    if (item.value) selected.Add(item.Text);
                return selected.AsReadOnly();
            }
        }

        // ----------------------------------------------------------------
        // Events
        // ----------------------------------------------------------------

        public event Action<IReadOnlyList<string>> onValueChanged;

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public DebugToggleGroup() : this(string.Empty) { }

        public DebugToggleGroup(string label)
        {
            AddToClassList(ussClassName);
            AddToClassList("c-control-size");

            _labelElement = new Label(label);
            _labelElement.AddToClassList(labelUssClassName);
            hierarchy.Add(_labelElement);

            _itemsContainer = new VisualElement();
            _itemsContainer.AddToClassList(itemsUssClassName);
            hierarchy.Add(_itemsContainer);
        }

        // ----------------------------------------------------------------
        // Item registration (called by DebugToggleGroupItem via Attach/Detach)
        // ----------------------------------------------------------------

        internal void RegisterItem(DebugToggleGroupItem item)
        {
            if (_items.Contains(item)) return;
            _items.Add(item);
            item.onValueChanged += OnItemValueChanged;
        }

        internal void UnregisterItem(DebugToggleGroupItem item)
        {
            if (!_items.Contains(item)) return;
            item.onValueChanged -= OnItemValueChanged;
            _items.Remove(item);
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        /// <summary>指定したテキストの項目を選択状態にする（イベントは発火しない）。</summary>
        public void SetValueWithoutNotify(IEnumerable<string> selectedTexts)
        {
            var selected = new HashSet<string>(selectedTexts);
            foreach (var item in _items)
                item.SetValueWithoutNotify(selected.Contains(item.Text));
        }

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------

        private void OnItemValueChanged(bool _) => onValueChanged?.Invoke(value);
    }
}
