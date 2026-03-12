using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// カスタムコントロールを用いて、カプセル化されたデザインシステムUIを構築するサンプルクラス
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Sample3 : MonoBehaviour
    {
        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // 既存のコンテンツをクリア
            root.Clear();

            // 1. Root Layer (t-root, l-screen)
            root.AddToClassList("t-root");
            root.AddToClassList("l-screen");
            root.AddToClassList("u-center-content");
            root.AddToClassList("u-bg-transparent");

            // 2. Window Layer (l-window, t-surface)
            var window = new VisualElement();
            window.AddToClassList("l-window");
            window.AddToClassList("t-surface");
            window.style.width = 900; 
            window.style.maxWidth = Length.Percent(95);
            root.Add(window);

            // 3. ScrollView Layer
            // コンテンツをスクロール可能にする
            var scrollView = new MainScrollView();
            window.Add(scrollView);

            // 4. Components (Added to scrollView)
            
            // Header
            var header = new MainLabel("Encapsulated UI (Sample3)");
            scrollView.Add(header);

            // TextField
            var textField = new MainTextField("ユーザー名");
            scrollView.Add(textField);

            // Foldout
            var foldout = new MainFoldout();
            foldout.text = "詳細設定 (MainFoldout)";
            
            scrollView.Add(foldout);
            
            // Secondary Button inside Foldout
            var innerButton = new SecondaryButton("内部ボタン");
            foldout.Add(innerButton);

            // Primary Button
            var primaryButton = new PrimaryButton("決定");
            scrollView.Add(primaryButton);
        }
    }
}
