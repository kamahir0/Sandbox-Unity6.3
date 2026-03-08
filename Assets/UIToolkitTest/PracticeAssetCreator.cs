using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

namespace UIToolkitTest
{
    [ExecuteInEditMode]
    public class PracticeAssetCreator : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying) return;
            CreatePanelSettings();
            DestroyImmediate(gameObject);
        }

        public static void CreatePanelSettings()
        {
            var path = "Assets/UIToolkitTest/PracticePanelSettings.asset";
            if (System.IO.File.Exists(Application.dataPath + "/UIToolkitTest/PracticePanelSettings.asset"))
            {
                Debug.Log("Asset already exists.");
                return;
            }
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, path);
            AssetDatabase.SaveAssets();
            Debug.Log("PracticePanelSettings.asset created successfully.");
        }
    }
}
