using UnityEngine.UIElements;

namespace Lilja.DebugMenu
{
    /// <summary>
    /// 画面全体に広がり、子要素を中央寄せする共通ルートコンテナ。
    /// UIDocument.rootVisualElement の直下に配置して使用する。
    ///
    /// UXML での使い方:
    ///   &lt;DebugMenuRoot&gt; ... &lt;/DebugMenuRoot&gt;
    /// </summary>
    [UxmlElement]
    public partial class DebugMenuRoot : VisualElement
    {
        public DebugMenuRoot()
        {
            AddToClassList("t-root");
            AddToClassList("l-screen");
            AddToClassList("u-center-content");
            AddToClassList("u-bg-transparent");
        }
    }
}
