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

            // UIBuilder で表示したときに見えるよう、初期は 0% としておく。
            // ランタイムでナビゲーションされる際は Animator 側で遷移前の座標(100% / -100%)が再設定されるため問題ない。
            style.position = Position.Absolute;
            style.left = 0;
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
