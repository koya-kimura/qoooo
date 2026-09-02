using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FinalCompositeRenderer))]
    [RequireComponent(typeof(TextureOutputController))]
    public sealed class VjPreferencesController : MonoBehaviour, IVjPreferencesController
    {
        [SerializeField] private FinalCompositeRenderer compositor;
        [SerializeField] private TextureOutputController output;

        public VjPreferences CurrentPreferences => new()
        {
            outputWidth = compositor.Settings.outputWidth,
            outputHeight = compositor.Settings.outputHeight,
            outputName = output.OutputName,
            outputMode = output.Mode
        };

        private void Awake()
        {
            if (compositor == null) compositor = GetComponent<FinalCompositeRenderer>();
            if (output == null) output = GetComponent<TextureOutputController>();

            if (TryLoadPreferences(out var preferences))
                ApplyPreferences(preferences);

            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IVjControlPanel controlPanel)
                    controlPanel.Initialize(this);
            }
        }

        public void ApplyPreferences(VjPreferences preferences)
        {
            if (preferences == null) return;

            compositor.Settings.outputWidth = Mathf.Clamp(
                preferences.outputWidth,
                VjRuntimeSettings.MinDimension,
                VjRuntimeSettings.MaxDimension);
            compositor.Settings.outputHeight = Mathf.Clamp(
                preferences.outputHeight,
                VjRuntimeSettings.MinDimension,
                VjRuntimeSettings.MaxDimension);
            output.OutputName = preferences.outputName;
            output.Mode = preferences.outputMode;

            compositor.RenderNow();
            output.Publish(compositor.FinalTexture);
        }

        public void SavePreferences(VjPreferences preferences)
        {
            ApplyPreferences(preferences);
            VjPreferencesStore.Save(CurrentPreferences);
        }

        public bool TryLoadPreferences(out VjPreferences preferences)
            => VjPreferencesStore.TryLoad(out preferences);
    }
}
