using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using Qoooo.VJ.UI;
using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FinalCompositeRenderer))]
    [RequireComponent(typeof(VjPreviewPresenter))]
    [RequireComponent(typeof(TextureOutputController))]
    public sealed class VjApplication : MonoBehaviour, IVjPreferencesController
    {
        private FinalCompositeRenderer _compositor;
        private VjPreviewPresenter _preview;
        private TextureOutputController _output;

        public VjPreferences CurrentPreferences => new()
        {
            outputWidth = _compositor.Settings.outputWidth,
            outputHeight = _compositor.Settings.outputHeight,
            outputName = _output.OutputName,
            outputMode = _output.Mode
        };

        private void Awake()
        {
            _compositor = GetComponent<FinalCompositeRenderer>();
            _preview = GetComponent<VjPreviewPresenter>();
            _output = GetComponent<TextureOutputController>();

            _preview.Source = _compositor;
            _output.Source = _compositor;

            if (TryLoadPreferences(out var preferences))
                ApplyPreferences(preferences);

            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IVjControlPanel controlPanel)
                    controlPanel.Initialize(this);
            }
        }

        private void LateUpdate()
        {
            _compositor.RenderNow();
            _output.Publish(_compositor.FinalTexture);
        }

        public void ApplyPreferences(VjPreferences preferences)
        {
            if (preferences == null) return;

            _compositor.Settings.outputWidth = Mathf.Clamp(
                preferences.outputWidth,
                VjRuntimeSettings.MinDimension,
                VjRuntimeSettings.MaxDimension);
            _compositor.Settings.outputHeight = Mathf.Clamp(
                preferences.outputHeight,
                VjRuntimeSettings.MinDimension,
                VjRuntimeSettings.MaxDimension);
            _output.OutputName = preferences.outputName;
            _output.Mode = preferences.outputMode;

            _compositor.RenderNow();
            _output.Publish(_compositor.FinalTexture);
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
