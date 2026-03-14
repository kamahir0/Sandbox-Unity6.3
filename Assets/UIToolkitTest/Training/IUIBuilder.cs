using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Lilja.Training
{
    /// <summary>
    /// デザインシステムに基づいたUI要素を動的に追加するビルダーインターフェース。
    /// 各メソッドは自身を返すため、メソッドチェーンで記述できる。
    /// </summary>
    public interface IUIBuilder
    {
        IUIBuilder AddLabel(string text);

        IUIBuilder AddButton(string text, Action onClick);
        IUIBuilder AddPrimaryButton(string text, Action onClick);
        IUIBuilder AddDangerButton(string text, Action onClick);

        IUIBuilder AddTextField(string label, string initialValue = "", Action<string> onValueChanged = null);
        IUIBuilder AddIntegerField(string label, int initialValue = 0, Action<int> onValueChanged = null);
        IUIBuilder AddFloatField(string label, float initialValue = 0f, Action<float> onValueChanged = null);

        IUIBuilder AddToggle(string label, bool initialValue = false, Action<bool> onValueChanged = null);

        /// <summary>
        /// RadioButtonGroup と同外観で複数選択が可能なカスタムトグルグループを追加する。
        /// </summary>
        IUIBuilder AddMultiToggleGroup(string label, IEnumerable<string> choices, Action<IReadOnlyList<string>> onValueChanged = null);

        /// <summary>
        /// Foldout を追加する。innerBuilder に折りたたみ内部のコンテンツを渡す。
        /// </summary>
        IUIBuilder AddFoldout(string label, IUIBuilder innerBuilder);

        /// <summary>
        /// 登録された要素を構築し、コンテナ VisualElement として返す。
        /// </summary>
        VisualElement Build();
    }
}
