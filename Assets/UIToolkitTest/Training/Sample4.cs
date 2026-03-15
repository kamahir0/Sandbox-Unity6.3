using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// IUIBuilder を用いてコードのみでUIを動的に構築するサンプル。
    /// "何を表示するか" の記述（UIBuilder）と
    /// "どう配置するか" の記述（window/scrollView の構造）が分離されている点に注目。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Sample4 : MonoBehaviour
    {
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();

            // --- 構造レイヤー（Sample3 と同じ。IUIBuilder の関心外） ---
            root.AddToClassList("t-root");
            root.AddToClassList("l-screen");
            root.AddToClassList("u-center-content");
            root.AddToClassList("u-bg-transparent");

            var window = new AppWindow("Sample4: IUIBuilder デモ");
            window.style.width = 900;
            window.style.maxWidth = Length.Percent(95);
            root.Add(window);

            var scrollView = new MainScrollView();
            window.Add(scrollView);

            // --- コンテンツレイヤー（IUIBuilder が担当） ---

            // メインコンテンツを UIBuilder で組み立て
            IUIBuilder mainBuilder = new UIBuilder()
                .AddTextField("ユーザー名", "", v => Debug.Log($"ユーザー名: {v}"))
                .AddIntegerField("年齢", 20, v => Debug.Log($"年齢: {v}"))
                .AddFoldout("詳細設定", new UIBuilder()
                    .AddFloatField("身長 (cm)", 170f, v => Debug.Log($"身長: {v}"))
                    .AddToggle("メルマガ受信", true, v => Debug.Log($"メルマガ: {v}"))
                    .AddDangerButton("アカウント削除", () => Debug.Log("削除")))
                .AddButton("キャンセル", () => Debug.Log("キャンセル"))
                .AddPrimaryButton("決定", () => Debug.Log("決定"));

            scrollView.Add(mainBuilder.Build());
        }
    }
}
