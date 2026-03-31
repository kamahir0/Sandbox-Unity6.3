using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// 複数のコントロールクラスで共有される USS クラス名
    /// </summary>
    public static class DebugMenuUssClass
    {
        public const string ControlSize = "c-control-size";
        public const string Button = "c-button";
        public const string Input = "c-input";
    }

    /// <summary>
    /// ナビゲーションボタン（iOS設定アプリ風）
    /// 背景色はページと同一、上下のセパレーターで矩形を可視化する。
    /// テキストラベルと右端矢印アイコンを子要素として保持する。
    /// </summary>
    [UxmlElement]
    public partial class DebugNavigationButton : Button
    {
        // クラス
        private const string UssClassName = "c-nav-button";
        private const string LabelUssClassName = UssClassName + "__label";
        private const string IconUssClassName = UssClassName + "__icon";

        public DebugNavigationButton() : this(string.Empty) { }

        public DebugNavigationButton(string text) : base()
        {
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);

            var label = new Label(text);
            label.AddToClassList(LabelUssClassName);
            Add(label);

            var icon = new VisualElement();
            icon.AddToClassList(IconUssClassName);
            Add(icon);
        }
    }

    /// <summary>
    /// デバッグメニュー用のプライマリボタン
    /// </summary>
    [UxmlElement]
    public partial class DebugButton : Button
    {
        // クラス
        private const string UssClassName = DebugMenuUssClass.Button;
        private const string PrimaryUssClassName = UssClassName + "--primary";

        public DebugButton() : this(string.Empty) { }

        public DebugButton(string text) : base()
        {
            this.text = text;
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);
            AddToClassList(PrimaryUssClassName);
        }
    }

    /// <summary>
    /// デバッグメニュー用のセカンダリボタン
    /// </summary>
    [UxmlElement]
    public partial class DebugSecondaryButton : Button
    {
        // クラス
        private const string UssClassName = DebugMenuUssClass.Button;
        private const string SecondaryUssClassName = UssClassName + "--secondary";

        public DebugSecondaryButton() : this(string.Empty) { }

        public DebugSecondaryButton(string text) : base()
        {
            this.text = text;
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);
            AddToClassList(SecondaryUssClassName);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Input);
        }
    }

    /// <summary>
    /// デバッグメニュー用のラベル
    /// </summary>
    [UxmlElement]
    public partial class DebugLabel : Label
    {
        // クラス
        private const string UssClassName = "c-label";

        public DebugLabel() : this(string.Empty) { }

        public DebugLabel(string text) : base(text)
        {
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);
        }
    }

    /// <summary>
    /// デバッグメニュー用のフォールドアウト
    /// </summary>
    [UxmlElement]
    public partial class DebugFoldout : Foldout
    {
        // クラス
        private const string UssClassName = "c-foldout";

        public DebugFoldout() : this(string.Empty) { }

        public DebugFoldout(string label) : base()
        {
            text = label;
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);
        }
    }

    /// <summary>
    /// デバッグメニュー用のラジオボタングループ
    /// </summary>
    [UxmlElement]
    public partial class DebugRadioButtonGroup : RadioButtonGroup
    {
        // クラス
        private const string UssClassName = "c-radio-group";

        public DebugRadioButtonGroup() : this(string.Empty) { }

        public DebugRadioButtonGroup(string label) : base(label)
        {
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(UssClassName);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Input);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Input);
        }
    }
}
