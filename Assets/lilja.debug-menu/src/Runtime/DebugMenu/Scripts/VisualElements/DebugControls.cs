using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ナビゲーションボタン（iOS設定アプリ風）
    /// 背景色はページと同一、上下のセパレーターで矩形を可視化する。
    /// テキストラベルと右端矢印アイコンを子要素として保持する。
    /// </summary>
    [UxmlElement]
    public partial class DebugNavigationButton : Button
    {
        public DebugNavigationButton() : this(string.Empty) { }

        public DebugNavigationButton(string text) : base()
        {
            AddToClassList("c-control-size");
            AddToClassList("c-nav-button");

            var label = new Label(text);
            label.AddToClassList("c-nav-button__label");
            Add(label);

            var icon = new VisualElement();
            icon.AddToClassList("c-nav-button__icon");
            Add(icon);
        }
    }

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
        public DebugFoldout() : this(string.Empty) { }

        public DebugFoldout(string label) : base()
        {
            text = label;
            AddToClassList("c-control-size");
            AddToClassList("c-foldout");
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
