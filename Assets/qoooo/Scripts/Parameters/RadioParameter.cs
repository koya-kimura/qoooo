using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Parameters
{
    [Serializable]
    public sealed class RadioParameter : IValue<int>, IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private List<string> _options = new();
        [SerializeField] private int _defaultValue;
        [NonSerialized] private int _value;
        [NonSerialized] private bool _initialized;
        public RadioParameter() { }
        public RadioParameter(string id, string displayName, IEnumerable<string> options, int defaultValue = 0)
        { _id = id; _displayName = displayName; _options = options == null ? new List<string>() : new List<string>(options); _defaultValue = defaultValue; }
        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public MidiParameterKind Kind => MidiParameterKind.Radio;
        public IReadOnlyList<string> Options => _options;
        public int Value { get { EnsureInitialized(); return _value; } }
        public event Action<int> Changed;
        public bool IsValid => _options != null && _options.Count > 0;
        public bool TrySetValue(int value)
        {
            EnsureInitialized(); var normalized = Normalize(value);
            if (_value == normalized) return false;
            _value = normalized; Changed?.Invoke(_value); return true;
        }
        public bool ReplaceOptions(IEnumerable<string> options, int defaultValue = 0)
        {
            EnsureInitialized();
            var selectedOption = _value >= 0 && _value < _options.Count ? _options[_value] : null;
            var nextOptions = options == null ? new List<string>() : new List<string>(options);
            for (var index = 0; index < nextOptions.Count; index++)
                if (string.IsNullOrWhiteSpace(nextOptions[index])) nextOptions[index] = $"Option {index}";

            var optionsChanged = _options.Count != nextOptions.Count;
            if (!optionsChanged)
                for (var index = 0; index < _options.Count; index++)
                    if (_options[index] != nextOptions[index]) { optionsChanged = true; break; }

            _options = nextOptions;
            _defaultValue = Normalize(defaultValue);
            var selectedIndex = selectedOption == null ? -1 : _options.IndexOf(selectedOption);
            var nextValue = selectedIndex >= 0 ? selectedIndex : Normalize(_value);
            var valueChanged = _value != nextValue;
            _value = nextValue;
            if (valueChanged) Changed?.Invoke(_value);
            return optionsChanged || valueChanged;
        }
        public void InitializeFromDefault() { EnsureOptions(); _value = Normalize(_defaultValue); _initialized = true; }
        public void ResetToDefault() { EnsureInitialized(); TrySetValue(_defaultValue); }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { _options ??= new List<string>(); _initialized = false; _value = default; }
        private int Normalize(int value) => _options.Count == 0 ? 0 : Mathf.Clamp(value, 0, _options.Count - 1);
        private void EnsureOptions() { _options ??= new List<string>(); for (var i = 0; i < _options.Count; i++) if (string.IsNullOrWhiteSpace(_options[i])) _options[i] = $"Option {i}"; }
        private void EnsureInitialized() { if (!_initialized) InitializeFromDefault(); }
    }
}
