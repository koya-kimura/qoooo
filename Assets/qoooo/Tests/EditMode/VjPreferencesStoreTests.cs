using NUnit.Framework;
using UnityEngine;

namespace Qoooo.VJ.Tests
{
    public sealed class VjPreferencesStoreTests
    {
        private bool _hadPreviousValue;
        private string _previousValue;

        [SetUp]
        public void SetUp()
        {
            _hadPreviousValue = PlayerPrefs.HasKey(VjPreferencesStore.PlayerPrefsKey);
            if (_hadPreviousValue)
                _previousValue = PlayerPrefs.GetString(VjPreferencesStore.PlayerPrefsKey);
            PlayerPrefs.DeleteKey(VjPreferencesStore.PlayerPrefsKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadPreviousValue)
                PlayerPrefs.SetString(VjPreferencesStore.PlayerPrefsKey, _previousValue);
            else
                PlayerPrefs.DeleteKey(VjPreferencesStore.PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void SaveAndLoad_RoundTripsOutputPreferences()
        {
            var expected = new VjPreferences
            {
                outputWidth = 1280,
                outputHeight = 720,
                outputName = "Test Sender",
                outputMode = VjOutputMode.Syphon
            };

            VjPreferencesStore.Save(expected);
            var loaded = VjPreferencesStore.TryLoad(out var actual);

            Assert.That(loaded, Is.True);
            Assert.That(actual.outputWidth, Is.EqualTo(expected.outputWidth));
            Assert.That(actual.outputHeight, Is.EqualTo(expected.outputHeight));
            Assert.That(actual.outputName, Is.EqualTo(expected.outputName));
            Assert.That(actual.outputMode, Is.EqualTo(expected.outputMode));
        }

        [Test]
        public void TryLoad_ReturnsFalse_WhenNoPreferencesExist()
        {
            Assert.That(VjPreferencesStore.TryLoad(out _), Is.False);
        }
    }
}
