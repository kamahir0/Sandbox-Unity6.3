using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// IUIBuilder の具象実装。
    /// 各 Add メソッドは要素の生成ファクトリを登録し、Build() 呼び出し時に一括生成する。
    /// </summary>
    public sealed class UIBuilder : IUIBuilder
    {
        private readonly List<Func<VisualElement>> _factories = new();

        // ----------------------------------------------------------------
        // Label
        // ----------------------------------------------------------------

        public IUIBuilder AddLabel(string text)
        {
            _factories.Add(() => new MainLabel(text));
            return this;
        }

        // ----------------------------------------------------------------
        // Buttons
        // ----------------------------------------------------------------

        public IUIBuilder AddButton(string text, Action onClick)
        {
            _factories.Add(() =>
            {
                var btn = new SecondaryButton(text);
                if (onClick != null) btn.clicked += onClick;
                return btn;
            });
            return this;
        }

        public IUIBuilder AddPrimaryButton(string text, Action onClick)
        {
            _factories.Add(() =>
            {
                var btn = new PrimaryButton(text);
                if (onClick != null) btn.clicked += onClick;
                return btn;
            });
            return this;
        }

        public IUIBuilder AddDangerButton(string text, Action onClick)
        {
            _factories.Add(() =>
            {
                var btn = new Button { text = text };
                btn.AddToClassList("c-control-size");
                btn.AddToClassList("c-button");
                btn.AddToClassList("c-button--danger");
                if (onClick != null) btn.clicked += onClick;
                return btn;
            });
            return this;
        }

        // ----------------------------------------------------------------
        // Input fields
        // ----------------------------------------------------------------

        public IUIBuilder AddTextField(string label, string initialValue = "", Action<string> onValueChanged = null)
        {
            _factories.Add(() =>
            {
                var field = new MainTextField(label) { value = initialValue };
                if (onValueChanged != null)
                    field.RegisterValueChangedCallback(e => onValueChanged(e.newValue));
                return field;
            });
            return this;
        }

        public IUIBuilder AddIntegerField(string label, int initialValue = 0, Action<int> onValueChanged = null)
        {
            _factories.Add(() =>
            {
                var field = new IntegerField(label) { value = initialValue };
                field.AddToClassList("c-control-size");
                field.AddToClassList("c-input");
                if (onValueChanged != null)
                    field.RegisterValueChangedCallback(e => onValueChanged(e.newValue));
                return field;
            });
            return this;
        }

        public IUIBuilder AddFloatField(string label, float initialValue = 0f, Action<float> onValueChanged = null)
        {
            _factories.Add(() =>
            {
                var field = new FloatField(label) { value = initialValue };
                field.AddToClassList("c-control-size");
                field.AddToClassList("c-input");
                if (onValueChanged != null)
                    field.RegisterValueChangedCallback(e => onValueChanged(e.newValue));
                return field;
            });
            return this;
        }

        // ----------------------------------------------------------------
        // Toggle
        // ----------------------------------------------------------------

        public IUIBuilder AddToggle(string label, bool initialValue = false, Action<bool> onValueChanged = null)
        {
            _factories.Add(() =>
            {
                var toggle = new Toggle(label) { value = initialValue };
                toggle.AddToClassList("c-control-size");
                toggle.AddToClassList("c-toggle");
                if (onValueChanged != null)
                    toggle.RegisterValueChangedCallback(e => onValueChanged(e.newValue));
                return toggle;
            });
            return this;
        }

        // ----------------------------------------------------------------
        // Foldout
        // ----------------------------------------------------------------

        public IUIBuilder AddFoldout(string label, IUIBuilder innerBuilder)
        {
            _factories.Add(() =>
            {
                var foldout = new MainFoldout { text = label };
                // innerBuilder の内容をそのまま Foldout のコンテンツとして積む
                foldout.Add(innerBuilder.Build());
                return foldout;
            });
            return this;
        }

        // ----------------------------------------------------------------
        // Build
        // ----------------------------------------------------------------

        /// <summary>
        /// 登録された全要素を生成してコンテナに積み、返す。
        /// </summary>
        public VisualElement Build()
        {
            var container = new VisualElement();
            foreach (var factory in _factories)
                container.Add(factory());
            return container;
        }
    }
}
