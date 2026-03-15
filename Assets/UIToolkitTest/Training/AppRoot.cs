using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// 画面全体に広がり、子要素を中央寄せする共通ルートコンテナ。
    /// UIDocument.rootVisualElement の直下に配置して使用する。
    ///
    /// UXML での使い方:
    ///   xmlns:training="Lilja.Training" を宣言したうえで
    ///   &lt;training:AppRoot&gt; ... &lt;/training:AppRoot&gt;
    /// </summary>
    public class AppRoot : VisualElement
    {
        public AppRoot()
        {
            AddToClassList("t-root");
            AddToClassList("l-screen");
            AddToClassList("u-center-content");
            AddToClassList("u-bg-transparent");
        }

        public new class UxmlFactory : UxmlFactory<AppRoot, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }
    }
}
