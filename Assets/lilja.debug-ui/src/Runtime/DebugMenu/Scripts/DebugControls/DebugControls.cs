using System;
using UnityEngine;
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
    /// Clickable のサブクラス。ProcessDownEvent / ProcessUpEvent をフックして
    /// ボタン押下・解放の通知を外部に公開する。
    /// Clickable は PointerDownEvent 内で StopImmediatePropagation() を呼ぶため
    /// 通常の RegisterCallback では押下を検知できず、このサブクラスで対処する。
    /// </summary>
    internal class InteractiveClickable : Clickable
    {
        public event Action OnPressed;
        public event Action OnReleased;

        public InteractiveClickable() : base((Action)null) { }

        protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            OnPressed?.Invoke();
            base.ProcessDownEvent(evt, localPosition, pointerId);
        }

        protected override void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            base.ProcessUpEvent(evt, localPosition, pointerId);
            OnReleased?.Invoke();
        }
    }

    /// <summary>
    /// ボタンのホバー・アクティブ状態の背景色をインラインスタイルで管理するヘルパー。
    /// Unity のデフォルトテーマが background-color を上書きするため USS では制御できず、
    /// C# から直接 style.backgroundColor をセットして対応する。
    /// ホバー色・アクティブ色は USS カスタムプロパティ --hover-color / --active-color で定義する。
    /// 呼び出し前に button.clicked への購読を済ませないこと（clickable 置き換えで失われる）。
    /// </summary>
    internal static class ButtonInteractionHelper
    {
        static readonly CustomStyleProperty<Color> s_HoverColor  = new("--hover-color");
        static readonly CustomStyleProperty<Color> s_ActiveColor = new("--active-color");

        internal static void Register(Button button)
        {
            Color hoverColor  = default;
            Color activeColor = default;
            bool  isOver      = false;

            // Clickable を置き換えてプレス/リリースをフック
            var clickable = new InteractiveClickable();
            button.clickable = clickable;

            button.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                e.customStyle.TryGetValue(s_HoverColor,  out hoverColor);
                e.customStyle.TryGetValue(s_ActiveColor, out activeColor);
            });

            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                isOver = true;
                button.style.backgroundColor = hoverColor;
            });

            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                isOver = false;
                button.style.backgroundColor = StyleKeyword.Null;
            });

            clickable.OnPressed += () =>
            {
                button.style.backgroundColor = activeColor;
            };

            clickable.OnReleased += () =>
            {
                button.style.backgroundColor = isOver ? (StyleColor)hoverColor : StyleKeyword.Null;
            };
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

            ButtonInteractionHelper.Register(this);
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
            ButtonInteractionHelper.Register(this);
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
            ButtonInteractionHelper.Register(this);
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
            ButtonInteractionHelper.Register(this);
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
