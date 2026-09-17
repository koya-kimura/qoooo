using System;
using UnityEngine;

namespace qoooo.Parameters
{
    [Serializable]
    public sealed class StateParameter : IValue<int>, IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(1)] private int _stateCount = 1;
        [SerializeField] private int _defaultValue;
        [NonSerialized] private int _value;
        [NonSerialized] private bool _initialized;
        public StateParameter() { }
        public StateParameter(string id, string displayName, int stateCount, int defaultValue = 0)
        { _id=id; _displayName=displayName; _stateCount=stateCount; _defaultValue=defaultValue; }
        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public MidiParameterKind Kind => MidiParameterKind.State;
        public int StateCount => Mathf.Max(1, _stateCount);
        public int Value { get { EnsureInitialized(); return _value; } }
        public event Action<int> Changed;
        public bool TrySetValue(int value) { EnsureInitialized(); var next = Mathf.Clamp(value, 0, StateCount - 1); if (_value == next) return false; _value=next; Changed?.Invoke(_value); return true; }
        public bool Next() => TrySetValue((Value + 1) % StateCount);
        public void InitializeFromDefault() { _stateCount=Mathf.Max(1,_stateCount); _value=Mathf.Clamp(_defaultValue,0,_stateCount-1); _initialized=true; }
        public void ResetToDefault() { EnsureInitialized(); TrySetValue(_defaultValue); }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { _initialized=false; _value=default; }
        private void EnsureInitialized() { if (!_initialized) InitializeFromDefault(); }
    }
}
