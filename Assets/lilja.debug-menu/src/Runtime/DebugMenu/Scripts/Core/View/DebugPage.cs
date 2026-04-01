using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// デバッグメニュー用のページ
    /// </summary>
    [UxmlElement]
    public partial class DebugPage : VisualElement
    {
        #region Virtual
        public virtual void Configure(IDebugPageBuilder builder) { }
        #endregion

        // UI
        private readonly ScrollView _scrollView;

        /// <inheritdoc/>
        public override VisualElement contentContainer => _scrollView.contentContainer;

        // クラス
        private const string UssClassName = "c-page";
        private const string ScrollViewUssClassName = "c-scroll-view";
        private const string SurfaceUssClassName = "t-surface";

        public DebugPage() : base()
        {
            AddToClassList(SurfaceUssClassName);
            AddToClassList(UssClassName);

            // スクロースビュー
            _scrollView = new ScrollView();
            _scrollView.AddToClassList(ScrollViewUssClassName);
            hierarchy.Add(_scrollView);
        }

        /// <summary>
        /// スクロール位置をリセットする
        /// </summary>
        public void ResetScrollPosition()
        {
            _scrollView.scrollOffset = Vector2.zero;
        }

        /// <summary>
        /// コンテナへ追加される前の初期レイアウトを設定する。画面外右端に絶対配置する。
        /// </summary>
        internal void SetLayout()
        {
            style.position = Position.Absolute;
            style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            style.top = 0;
            style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        }
    }
}
