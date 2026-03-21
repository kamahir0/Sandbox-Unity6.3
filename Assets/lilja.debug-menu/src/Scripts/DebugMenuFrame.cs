using System;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニューのフレーム
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuFrame : VisualElement
    {
        // クラス
        public static readonly string ussClassName = "c-menu-frame";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string backButtonUssClassName = ussClassName + "__back-button";
        public static readonly string backButtonIconUssClassName = ussClassName + "__back-button-icon";
        public static readonly string headerSpacerUssClassName = ussClassName + "__header-spacer";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string contentUssClassName = ussClassName + "__content";

        // UI
        private readonly Button _backButton;
        private readonly Label _label;
        private readonly VisualElement _contentContainer;

        /// <inheritdoc/>
        public override VisualElement contentContainer => _contentContainer;

        /// <summary> バックボタンクリック時 </summary>
        public event Action BackClicked
        {
            add => _backButton.clicked += value;
            remove => _backButton.clicked -= value;
        }

        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        public DebugMenuFrame()
        {
            AddToClassList(ussClassName);
            AddToClassList("t-surface");

            // ヘッダー
            var header = new VisualElement();
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            // バックボタン
            _backButton = new Button();
            _backButton.AddToClassList(backButtonUssClassName);
            var backButtonIcon = new VisualElement();
            backButtonIcon.AddToClassList(backButtonIconUssClassName);
            backButtonIcon.pickingMode = PickingMode.Ignore;
            _backButton.Add(backButtonIcon);
            header.Add(_backButton);

            // タイトルラベル
            _label = new Label();
            _label.AddToClassList(titleUssClassName);
            header.Add(_label);

            // スペーサー
            // バックボタンと同幅のスペーサーでタイトルを視覚的に中央寄せ
            var spacer = new VisualElement();
            spacer.AddToClassList(headerSpacerUssClassName);
            spacer.pickingMode = PickingMode.Ignore;
            header.Add(spacer);

            // コンテンツエリア
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList(contentUssClassName);
            hierarchy.Add(_contentContainer);
        }
    }
}
