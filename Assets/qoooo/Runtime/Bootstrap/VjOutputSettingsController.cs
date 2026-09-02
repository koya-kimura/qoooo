using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class VjOutputSettingsController : MonoBehaviour, IVjOutputSettingsController
    {
        [SerializeField] private FinalCompositeRenderer compositor;
        [SerializeField] private TextureOutputController output;

        public FinalCompositeRenderer Compositor
        {
            get => compositor;
            set => compositor = value;
        }

        public TextureOutputController Output
        {
            get => output;
            set => output = value;
        }

        public VjPreferences CurrentPreferences => compositor != null && output != null
            ? new VjPreferences
            {
                outputWidth = compositor.Settings.outputWidth,
                outputHeight = compositor.Settings.outputHeight,
                outputName = output.OutputName,
                outputMode = output.Mode
            }
            : new VjPreferences();

        public void ApplyPreferences(VjPreferences preferences)
        {
            if (preferences == null || compositor == null || output == null) return;

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
    }
}
