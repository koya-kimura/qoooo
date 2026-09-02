using UnityEngine;
using UnityEngine.UIElements;

namespace Qoooo.VJ.UI
{
    internal static class VjRuntimePanelFactory
    {
        public static UIDocument CreateDocument(
            Transform parent,
            string objectName,
            float sortingOrder,
            out PanelSettings ownedPanelSettings,
            bool activate = true)
        {
            var panelObject = new GameObject(objectName);
            panelObject.SetActive(false);
            panelObject.transform.SetParent(parent, false);

            ownedPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            ownedPanelSettings.name = $"{objectName} Panel Settings";
            ownedPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ownedPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
            ownedPanelSettings.sortingOrder = sortingOrder;
            ownedPanelSettings.themeStyleSheet =
                Resources.Load<ThemeStyleSheet>("VjRuntimeTheme");

            var document = panelObject.AddComponent<UIDocument>();
            document.panelSettings = ownedPanelSettings;
            panelObject.SetActive(activate);
            return document;
        }

        public static void DestroyOwned(Object panelObject, Object panelSettings)
        {
            if (Application.isPlaying)
            {
                if (panelObject != null) Object.Destroy(panelObject);
                if (panelSettings != null) Object.Destroy(panelSettings);
            }
            else
            {
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                if (panelSettings != null) Object.DestroyImmediate(panelSettings);
            }
        }
    }
}
