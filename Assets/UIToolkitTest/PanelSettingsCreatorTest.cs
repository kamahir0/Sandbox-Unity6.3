using UnityEditor;
using UnityEngine;
using NUnit.Framework;

namespace UIToolkitTest
{
    public class PanelSettingsCreatorTest
    {
        [Test]
        public void CreatePanelSettings()
        {
            var path = "Assets/UIToolkitTest/PracticePanelSettings.asset";
            var ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            AssetDatabase.CreateAsset(ps, path);
            AssetDatabase.SaveAssets();
            Debug.Log("PracticePanelSettings.asset created via Test.");
        }
    }
}
