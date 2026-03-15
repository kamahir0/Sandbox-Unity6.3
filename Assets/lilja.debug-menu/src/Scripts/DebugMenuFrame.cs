using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// ヘッダー付きのデバッグメニュー用ウィンドウコンポーネント。
    /// l-window + t-surface の構造を内包し、ヘッダーとコンテンツエリアを分離する。
    ///
    /// UXML での使い方:
    ///   &lt;DebugMenuFrame label="タイトル" /&gt;
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuFrame : VisualElement
    {
        public static readonly string ussClassName        = "c-window";
        public static readonly string headerUssClassName  = ussClassName + "__header";
        public static readonly string titleUssClassName   = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";

        private readonly Label         _titleLabel;
        private readonly VisualElement _contentContainer;

        public override VisualElement contentContainer => _contentContainer;

        [UxmlAttribute]
        public string label
        {
            get => _titleLabel.text;
            set => _titleLabel.text = value;
        }

        public DebugMenuFrame() : this(string.Empty) { }

        public DebugMenuFrame(string label)
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
    }
}
