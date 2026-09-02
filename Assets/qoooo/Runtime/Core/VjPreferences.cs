using System;
using UnityEngine;

namespace Qoooo.VJ
{
    public enum VjOutputMode
    {
        Auto,
        Disabled,
        Spout,
        Syphon
    }

    [Serializable]
    public sealed class VjPreferences
    {
        public int outputWidth = 1920;
        public int outputHeight = 1080;
        public string outputName = "qoooo VJ";
        public VjOutputMode outputMode = VjOutputMode.Auto;

        public VjPreferences Copy()
        {
            return new VjPreferences
            {
                outputWidth = outputWidth,
                outputHeight = outputHeight,
                outputName = outputName,
                outputMode = outputMode
            };
        }
    }

    public interface IVjOutputSettingsController
    {
        VjPreferences CurrentPreferences { get; }
        void ApplyPreferences(VjPreferences preferences);
    }

    public interface IVjPreferencesSaver
    {
        void SavePreferences(VjPreferences preferences);
    }

    public interface IVjControlPanel
    {
        void Initialize(
            IVjOutputSettingsController settingsController,
            IVjPreferencesSaver preferencesSaver);
    }

    public static class VjPreferencesStore
    {
        public const string PlayerPrefsKey = "qoooo.vj.preferences.v1";

        public static void Save(VjPreferences preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(preferences));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out VjPreferences preferences)
        {
            preferences = null;
            if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return false;

            try
            {
                preferences = JsonUtility.FromJson<VjPreferences>(
                    PlayerPrefs.GetString(PlayerPrefsKey));
                return preferences != null;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
