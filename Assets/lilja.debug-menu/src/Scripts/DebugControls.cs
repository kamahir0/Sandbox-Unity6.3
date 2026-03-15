using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニュー用のプライマリボタン
    /// </summary>
    [UxmlElement]
    public partial class DebugButton : Button
    {
        public DebugButton() : this(string.Empty) { }

        public DebugButton(string text) : base()
        {
            this.text = text;
            AddToClassList("c-control-size");
            AddToClassList("c-button");
            AddToClassList("c-button--primary");
        }
    }

    /// <summary>
    /// デバッグメニュー用のセカンダリボタン
    /// </summary>
    [UxmlElement]
    public partial class DebugSecondaryButton : Button
    {
        public DebugSecondaryButton() : this(string.Empty) { }

        public DebugSecondaryButton(string text) : base()
        {
            this.text = text;
            AddToClassList("c-control-size");
            AddToClassList("c-button");
            AddToClassList("c-button--secondary");
        }
    }

    /// <summary>
    /// デバッグメニュー用のテキストフィールド
    /// </summary>
    [UxmlElement]
    public partial class DebugTextField : TextField
    {
        public DebugTextField() : this(string.Empty) { }

        public DebugTextField(string label) : base(label)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-input");
        }
    }

    /// <summary>
    /// デバッグメニュー用のラベル
    /// </summary>
    [UxmlElement]
    public partial class DebugLabel : Label
    {
        public DebugLabel() : this(string.Empty) { }

        public DebugLabel(string text) : base(text)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-label");
        }
    }

    /// <summary>
    /// デバッグメニュー用のフォールドアウト
    /// </summary>
    [UxmlElement]
    public partial class DebugFoldout : Foldout
    {
        public DebugFoldout() : base()
        {
            AddToClassList("c-control-size");
        }
    }

    /// <summary>
    /// デバッグメニュー用のスクロールビュー
    /// </summary>
    [UxmlElement]
    public partial class DebugScrollView : ScrollView
    {
        public DebugScrollView() : base()
        {
            AddToClassList("c-scroll-view");
        }
    }

    /// <summary>
    /// デバッグメニュー用のラジオボタングループ
    /// </summary>
    [UxmlElement]
    public partial class DebugRadioButtonGroup : RadioButtonGroup
    {
        public DebugRadioButtonGroup() : this(string.Empty) { }

        public DebugRadioButtonGroup(string label) : base(label)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-radio-group");
        }
    }

    /// <summary>
    /// デバッグメニュー用の整数フィールド
    /// </summary>
    [UxmlElement]
    public partial class DebugIntegerField : IntegerField
    {
        public DebugIntegerField() : this(string.Empty) { }

        public DebugIntegerField(string label) : base(label)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-input");
        }
    }

    /// <summary>
    /// デバッグメニュー用の浮動小数点フィールド
    /// </summary>
    [UxmlElement]
    public partial class DebugFloatField : FloatField
    {
        public DebugFloatField() : this(string.Empty) { }

        public DebugFloatField(string label) : base(label)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-input");
        }
    }
}
