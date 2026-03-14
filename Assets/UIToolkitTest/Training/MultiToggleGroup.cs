using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// RadioButtonGroup と同様の外観（ラベル左・選択肢縦並び）で
    /// 複数選択が可能なカスタムトグルグループ。
    ///
    /// UXML での使い方:
    ///   xmlns:training="Lilja.Training" を宣言したうえで
    ///   &lt;training:MultiToggleGroup label="通知方法" choices="メール,SMS,プッシュ" class="c-control-size" /&gt;
    /// </summary>
    public class MultiToggleGroup : VisualElement
    {
        // USS クラス名（BEM 命名）
        public static readonly string ussClassName      = "multi-toggle-group";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string itemsUssClassName = ussClassName + "__items";

        private readonly Label         _labelElement;
        private readonly VisualElement _itemsContainer;
        private readonly List<MultiToggleItem> _items = new();
        private          List<string>          _choices = new();

        // ----------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------

        public string label
        {
            get => _labelElement.text;
            set => _labelElement.text = value;
        }

        public IReadOnlyList<string> choices => _choices.AsReadOnly();

        /// <summary>現在チェックされている選択肢の一覧。</summary>
        public IReadOnlyList<string> value
        {
            get
            {
                var selected = new List<string>();
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].value)
                        selected.Add(_choices[i]);
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

        public MultiToggleGroup() : this(string.Empty) { }

        public MultiToggleGroup(string label)
        {
            AddToClassList(ussClassName);

            _labelElement = new Label(label);
            _labelElement.AddToClassList(labelUssClassName);
            Add(_labelElement);

            _itemsContainer = new VisualElement();
            _itemsContainer.AddToClassList(itemsUssClassName);
            Add(_itemsContainer);
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        /// <summary>選択肢リストを設定し、項目を再生成する。</summary>
        public void SetChoices(IEnumerable<string> choices)
        {
            _choices = new List<string>(choices);
            RebuildItems();
        }

        /// <summary>指定した選択肢を選択状態にする（イベントは発火しない）。</summary>
        public void SetValueWithoutNotify(IEnumerable<string> selectedChoices)
        {
            var selected = new HashSet<string>(selectedChoices);
            for (int i = 0; i < _items.Count; i++)
                _items[i].SetValueWithoutNotify(selected.Contains(_choices[i]));
        }

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------

        private void RebuildItems()
        {
            _itemsContainer.Clear();
            _items.Clear();

            foreach (var choice in _choices)
            {
                var item = new MultiToggleItem(choice);
                item.onValueChanged += _ => onValueChanged?.Invoke(value);
                _items.Add(item);
                _itemsContainer.Add(item);
            }
        }

        // ----------------------------------------------------------------
        // UXML
        // ----------------------------------------------------------------

        public new class UxmlFactory : UxmlFactory<MultiToggleGroup, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _label =
                new() { name = "label", defaultValue = string.Empty };

            // RadioButtonGroup と同じカンマ区切り形式
            private readonly UxmlStringAttributeDescription _choices =
                new() { name = "choices", defaultValue = string.Empty };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var group = (MultiToggleGroup)ve;
                group.label = _label.GetValueFromBag(bag, cc);
                var choicesStr = _choices.GetValueFromBag(bag, cc);
                if (!string.IsNullOrWhiteSpace(choicesStr))
                    group.SetChoices(choicesStr.Split(','));
            }
        }
    }
}
