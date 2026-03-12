using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// デザインシステム上の Primary Button コンポーネント
    /// </summary>
    public class PrimaryButton : Button
    {
        public PrimaryButton() : this(string.Empty) { }

        public PrimaryButton(string text) : base()
        {
            this.text = text;
            AddToClassList("c-control-size");
            AddToClassList("c-button");
            AddToClassList("c-button--primary");
        }

        public new class UxmlFactory : UxmlFactory<PrimaryButton, UxmlTraits> { }
    }

    /// <summary>
    /// デザインシステム上の Secondary Button コンポーネント
    /// </summary>
    public class SecondaryButton : Button
    {
        public SecondaryButton() : this(string.Empty) { }

        public SecondaryButton(string text) : base()
        {
            this.text = text;
            AddToClassList("c-control-size");
            AddToClassList("c-button");
            AddToClassList("c-button--secondary");
        }

        public new class UxmlFactory : UxmlFactory<SecondaryButton, UxmlTraits> { }
    }

    /// <summary>
    /// デザインシステム上のメインテキストフィールド
    /// </summary>
    public class MainTextField : TextField
    {
        public MainTextField() : this(string.Empty) { }

        public MainTextField(string label) : base(label)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-input");
        }

        public new class UxmlFactory : UxmlFactory<MainTextField, UxmlTraits> { }
    }

    /// <summary>
    /// デザインシステム上のメインラベル
    /// </summary>
    public class MainLabel : Label
    {
        public MainLabel() : this(string.Empty) { }

        public MainLabel(string text) : base(text)
        {
            AddToClassList("c-control-size");
            AddToClassList("c-label");
        }

        public new class UxmlFactory : UxmlFactory<MainLabel, UxmlTraits> { }
    }

    /// <summary>
    /// デザインシステム上のメインフォールアウト
    /// </summary>
    public class MainFoldout : Foldout
    {
        public MainFoldout() : base()
        {
            AddToClassList("c-foldout");
        }

        public new class UxmlFactory : UxmlFactory<MainFoldout, UxmlTraits> { }
    }
}
