using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// ヘッダー付きの共通ウィンドウコンポーネント。
    /// l-window + t-surface の構造を内包し、ヘッダーとコンテンツエリアを分離する。
    ///
    /// UXML での使い方:
    ///   xmlns:training="Lilja.Training" を宣言したうえで
    ///   &lt;training:AppWindow label="タイトル" /&gt;
    /// </summary>
    public class AppWindow : VisualElement
    {
        public static readonly string ussClassName        = "c-window";
        public static readonly string headerUssClassName  = ussClassName + "__header";
        public static readonly string titleUssClassName   = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";

        private readonly Label         _titleLabel;
        private readonly VisualElement _contentContainer;

        public override VisualElement contentContainer => _contentContainer;

        public string label
        {
            get => _titleLabel.text;
            set => _titleLabel.text = value;
        }

        public AppWindow() : this(string.Empty) { }

        public AppWindow(string label)
        {
            AddToClassList(ussClassName);
            AddToClassList("t-surface");

            var header = new VisualElement();
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            _titleLabel = new Label(label);
            _titleLabel.AddToClassList(titleUssClassName);
            header.Add(_titleLabel);

            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(contentUssClassName);
            hierarchy.Add(_contentContainer);
        }

        public new class UxmlFactory : UxmlFactory<AppWindow, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _label =
                new() { name = "label", defaultValue = string.Empty };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((AppWindow)ve).label = _label.GetValueFromBag(bag, cc);
            }
        }
    }
}
