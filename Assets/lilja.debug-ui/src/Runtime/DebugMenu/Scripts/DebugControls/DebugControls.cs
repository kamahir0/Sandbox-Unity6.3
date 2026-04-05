using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    /// <summary>
    /// デバッグメニュー全体で使用する USS クラス名の一元管理
    /// </summary>
    public static class DebugMenuUssClass
    {
        public const string ControlSize = "c-control-size";
        public const string Input = "c-input";
        public const string HorizontalScope = "c-h-scope";

        public static class Button
        {
            public const string Root = "c-button";
            public const string Primary = Root + "--primary";
            public const string Secondary = Root + "--secondary";
            public const string Danger = Root + "--danger";
        }

        public static class NavigationButton
        {
            public const string Root = "c-nav-button";
            public const string Label = Root + "__label";
            public const string LeftIcon = Root + "__left-icon";
            public const string Icon = Root + "__icon";
        }

        public static class Label
        {
            public const string Root = "c-label";
        }

        public static class Foldout
        {
            public const string Root = "c-foldout";
        }

        public static class RadioButtonGroup
        {
            public const string Root = "c-radio-group";
        }

        public static class ToggleGroup
        {
            public const string Root = "c-toggle-group";
            public const string LabelElement = Root + "__label";
            public const string Items = Root + "__items";
        }

        public static class ToggleGroupItem
        {
            public const string Root = "c-toggle-group-item";
            public const string Checked = Root + "--checked";
            public const string Checkbox = Root + "__checkbox";
            public const string LabelElement = Root + "__label";
        }
    }

    /// <summary>
    /// ナビゲーションボタン（iOS設定アプリ風）
    /// 背景色はページと同一、上下のセパレーターで矩形を可視化する。
    /// テキストラベルと右端矢印アイコンを子要素として保持する。
    /// </summary>
    [UxmlElement]
    public partial class DebugNavigationButton : Button
    {
        public DebugNavigationButton() : this(string.Empty) { }

        public DebugNavigationButton(string text, StyleBackground? leftIcon = null) : base()
        {
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.NavigationButton.Root);

            if (leftIcon.HasValue)
            {
                var iconLeft = new VisualElement();
                iconLeft.AddToClassList(DebugMenuUssClass.NavigationButton.LeftIcon);
                iconLeft.style.backgroundImage = leftIcon.Value;
                iconLeft.pickingMode = PickingMode.Ignore;
                Add(iconLeft);
            }

            var label = new Label(text);
            label.AddToClassList(DebugMenuUssClass.NavigationButton.Label);
            Add(label);

            var icon = new VisualElement();
            icon.AddToClassList(DebugMenuUssClass.NavigationButton.Icon);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Button.Root);
            AddToClassList(DebugMenuUssClass.Button.Primary);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Button.Root);
            AddToClassList(DebugMenuUssClass.Button.Secondary);
        }
    }

    /// <summary>
    /// デバッグメニュー用のデンジャーボタン（削除・リセットなど破壊的操作用）
    /// </summary>
    [UxmlElement]
    public partial class DebugDangerButton : Button
    {
        public DebugDangerButton() : this(string.Empty) { }

        public DebugDangerButton(string text) : base()
        {
            this.text = text;
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Button.Root);
            AddToClassList(DebugMenuUssClass.Button.Danger);
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
        public DebugLabel() : this(string.Empty) { }

        public DebugLabel(string text) : base(text)
        {
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Label.Root);
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
            value = false;
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.Foldout.Root);
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
            AddToClassList(DebugMenuUssClass.ControlSize);
            AddToClassList(DebugMenuUssClass.RadioButtonGroup.Root);
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
