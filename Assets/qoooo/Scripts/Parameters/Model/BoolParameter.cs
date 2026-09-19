using System;
using UnityEngine;

namespace qoooo.Parameters.Model
{
    [Serializable]
    public sealed class BoolParameter : IValue<bool>, IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _defaultValue;
        [NonSerialized] private bool _value;
        [NonSerialized] private bool _initialized;

        public BoolParameter() { }
        public BoolParameter(string id, string displayName, bool defaultValue = false)
        { _id = id; _displayName = displayName; _defaultValue = defaultValue; }
        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public MidiParameterKind Kind => MidiParameterKind.Toggle;
        public bool Value { get { EnsureInitialized(); return _value; } }
        public event Action<bool> Changed;
        public bool TrySetValue(bool value)
        {
            EnsureInitialized();
            if (_value == value) return false;
            _value = value; Changed?.Invoke(_value); return true;
        }
        public void InitializeFromDefault() { _value = _defaultValue; _initialized = true; }
        public void ResetToDefault() { EnsureInitialized(); TrySetValue(_defaultValue); }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { _initialized = false; _value = default; }
        private void EnsureInitialized() { if (!_initialized) InitializeFromDefault(); }
    }
}
