using System;
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
        public static readonly string ussClassName = "c-menu-frame";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string backButtonUssClassName = ussClassName + "__back-button";
        public static readonly string backButtonIconUssClassName = ussClassName + "__back-button-icon";
        public static readonly string headerSpacerUssClassName = ussClassName + "__header-spacer";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";

        private readonly Button _backButton;
        private readonly Label _titleLabel;
        private readonly VisualElement _contentContainer;

        public override VisualElement contentContainer => _contentContainer;

        /// <summary>バックボタンがクリックされたときに発火するイベント。</summary>
        public event Action BackClicked;

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

            _backButton = new Button(() => BackClicked?.Invoke());
            _backButton.AddToClassList(backButtonUssClassName);
            var backButtonIcon = new VisualElement();
            backButtonIcon.AddToClassList(backButtonIconUssClassName);
            backButtonIcon.pickingMode = PickingMode.Ignore;
            _backButton.Add(backButtonIcon);
            header.Add(_backButton);

            _titleLabel = new Label(label);
            _titleLabel.AddToClassList(titleUssClassName);
            header.Add(_titleLabel);

            // バックボタンと同幅のスペーサーでタイトルを視覚的に中央寄せ
            var spacer = new VisualElement();
            spacer.AddToClassList(headerSpacerUssClassName);
            spacer.pickingMode = PickingMode.Ignore;
            header.Add(spacer);

            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(contentUssClassName);
            hierarchy.Add(_contentContainer);
        }
    }
}
