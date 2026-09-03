using UnityEngine;
using UnityEngine.UIElements;

namespace Qoooo.VJ.UI
{
    internal static class VjRuntimePanelFactory
    {
        public static UIDocument CreateDocument(
            Transform parent,
            string objectName,
            PanelSettings panelSettings,
            float sortingOrder,
            bool activate = true)
        {
            var panelObject = new GameObject(objectName);
            panelObject.SetActive(false);
            panelObject.transform.SetParent(parent, false);

            var document = panelObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = sortingOrder;
            panelObject.SetActive(activate);
            return document;
        }

        public static void DestroyOwned(Object panelObject)
        {
            if (UnityEngine.Application.isPlaying)
            {
                if (panelObject != null) Object.Destroy(panelObject);
            }
            else
            {
                if (panelObject != null) Object.DestroyImmediate(panelObject);
            }
        }
    }
}
