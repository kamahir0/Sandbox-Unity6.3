using UnityEngine;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// C#コードのみでデザインシステム準拠のUIを構築するサンプルクラス
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Sample3 : MonoBehaviour
    {
        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // 既存のコンテンツをクリア（必要に応じて）
            root.Clear();

            // 1. Root Layer (t-root, l-screen)
            // 文字色の継承と画面全体のレイアウトを定義
            root.AddToClassList("t-root");
            root.AddToClassList("l-screen");
            root.AddToClassList("u-center-content");
            root.AddToClassList("u-bg-transparent");

            // 2. Window Layer (l-window, t-surface)
            // 実体のあるパネル
            var window = new VisualElement();
            window.AddToClassList("l-window");
            window.AddToClassList("t-surface");
            root.Add(window);

            // 3. Components
            
            // Header
            var header = new Label("Code Generated UI");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            header.style.fontSize = 30;
            window.Add(header);

            // TextField
            var textField = new TextField("ユーザー名");
            textField.AddToClassList("c-control-size");
            textField.AddToClassList("c-input");
            window.Add(textField);

            // Foldout
            var foldout = new Foldout();
            foldout.text = "詳細設定 (Foldout)";
            foldout.AddToClassList("c-foldout");
            
            // Button inside Foldout
            var innerButton = new Button();
            innerButton.text = "内部ボタン";
            innerButton.AddToClassList("c-control-size");
            innerButton.AddToClassList("c-button");
            innerButton.AddToClassList("c-button--secondary");
            foldout.Add(innerButton);
            
            window.Add(foldout);

            // Primary Button
            var primaryButton = new Button();
            primaryButton.text = "決定";
            primaryButton.AddToClassList("c-control-size");
            primaryButton.AddToClassList("c-button");
            primaryButton.AddToClassList("c-button--primary");
            window.Add(primaryButton);
        }
    }
}
