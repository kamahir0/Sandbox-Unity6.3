using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.DebugUI
{
    /// <summary>
    /// デバッグメニュー用のページ
    /// </summary>
    [UxmlElement]
    public partial class DebugPage : VisualElement
    {
        #region Virtual

        public virtual void Configure(IDebugPageBuilder builder) { }

        /// <summary>
        /// ページが表示された直後に呼ばれる（スライドアニメーション完了後）
        /// </summary>
        public virtual void OnPageShown() { }

        /// <summary>
        /// ページが非表示になった直後に呼ばれる（スライドアニメーション完了後・プール返却前）
        /// </summary>
        public virtual void OnPageHidden() { }

        #endregion

        // UI
        private readonly ScrollView _scrollView;

        /// <inheritdoc/>
        public override VisualElement contentContainer => _scrollView.contentContainer;

        // クラス
        private const string UssClassName = "c-page";
        private const string ScrollViewUssClassName = "c-scroll-view";
        private const string SurfaceUssClassName = "t-surface";

        public DebugPage()
        {
            AddToClassList(SurfaceUssClassName);
            AddToClassList(UssClassName);

            // 画面外右端に絶対配置（ナビゲーション時にスライドで表示される）
            style.position = Position.Absolute;
            style.left = new StyleLength(new Length(100, LengthUnit.Percent));
            style.top = 0;
            style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            // スクロールビュー
            _scrollView = new ScrollView();
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
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
    }
}
