#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem;

namespace qoooo.Editor
{
    [InitializeOnLoad]
    internal static class ProjectWideInputActionsInitializer
    {
        private const string AssetPath = "Assets/InputSystem_Actions.inputactions";

        static ProjectWideInputActionsInitializer()
        {
            EditorApplication.delayCall += EnsureConfigured;
        }

        private static void EnsureConfigured()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (actions != null && !ReferenceEquals(InputSystem.actions, actions))
            {
                InputSystem.actions = actions;
            }
        }
    }
}
#endif