using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class VjPreferencesController : MonoBehaviour, IVjPreferencesSaver
    {
        [SerializeField] private VjOutputSettingsController outputSettings;

        public VjOutputSettingsController OutputSettings
        {
            get => outputSettings;
            set => outputSettings = value;
        }

        private void Awake()
        {
            if (outputSettings != null && VjPreferencesStore.TryLoad(out var preferences))
                outputSettings.ApplyPreferences(preferences);
        }

        public void SavePreferences(VjPreferences preferences)
        {
            if (outputSettings == null || preferences == null) return;
            outputSettings.ApplyPreferences(preferences);
            VjPreferencesStore.Save(outputSettings.CurrentPreferences);
        }
    }
}
