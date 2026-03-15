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
            root.Clear();

            // 1. Root Layer
            var appRoot = new AppRoot();
            root.Add(appRoot);

            // 2. Window Layer
            var window = new AppWindow("Sample3: カスタムコントロール");
            window.style.width = 900;
            window.style.maxWidth = Length.Percent(95);
            appRoot.Add(window);

            // 3. ScrollView Layer
            var scrollView = new MainScrollView();
            window.Add(scrollView);

            // 4. Components

            var textField = new MainTextField("ユーザー名");
            scrollView.Add(textField);

            var foldout = new MainFoldout();
            foldout.text = "詳細設定 (MainFoldout)";
            scrollView.Add(foldout);

            var innerButton = new SecondaryButton("内部ボタン");
            foldout.Add(innerButton);

            var primaryButton = new PrimaryButton("決定");
            scrollView.Add(primaryButton);
        }
    }
}
