using System;
using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// 画面全体に広がり、子要素を中央寄せする共通ルートコンテナ
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuRoot : VisualElement
    {
        // クラス
        private const string UssClassName = "t-root";
        private const string ScreenLayoutUssClassName = "l-screen";
        private const string CenterContentUssClassName = "u-center-content";
        private const string BgTransparentUssClassName = "u-bg-transparent";

        public DebugMenuRoot()
        {
            AddToClassList(UssClassName);
            AddToClassList(ScreenLayoutUssClassName);
            AddToClassList(CenterContentUssClassName);
            AddToClassList(BgTransparentUssClassName);
        }

        /// <summary>
        /// 矩形外タップ検知を設定する。フレーム外をタップしたときに onOutsideTap を呼び出す。
        /// </summary>
        internal void SetupOutsideTapHandler(Func<VisualElement> getFrame, Action onOutsideTap)
        {
            RegisterCallback<PointerDownEvent>(evt =>
            {
                var frame = getFrame();
                if (frame != null && !frame.worldBound.Contains(evt.position))
                {
                    onOutsideTap();
                }
            }, TrickleDown.TrickleDown);
        }
    }
}
