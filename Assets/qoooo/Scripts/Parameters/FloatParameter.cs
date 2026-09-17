using System;
using UnityEngine;

namespace qoooo.Parameters
{
    [Serializable]
    public sealed class FloatParameter : IValue<float>, IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private float _defaultValue;
        [SerializeField] private float _min;
        [SerializeField] private float _max = 1f;
        [NonSerialized] private float _value;
        [NonSerialized] private bool _initialized;
        public FloatParameter() { }
        public FloatParameter(string id,string displayName,float min,float max,float defaultValue=0f) { _id=id;_displayName=displayName;_min=min;_max=max;_defaultValue=defaultValue; }
        public string Id=>_id; public string DisplayName=>string.IsNullOrWhiteSpace(_displayName)?_id:_displayName; public MidiParameterKind Kind=>MidiParameterKind.Float;
        public float Min=>_min; public float Max=>_max; public float Value { get { EnsureInitialized(); return _value; } }
        public float NormalizedValue => Mathf.InverseLerp(Min, Max, Value);
        public event Action<float> Changed;
        public bool TrySetValue(float value) { EnsureInitialized(); var next=Normalize(value); if (Mathf.Approximately(_value,next)) return false; _value=next; Changed?.Invoke(_value); return true; }
        public bool TrySetNormalizedValue(float value) => TrySetValue(Mathf.Lerp(Min,Max,Mathf.Clamp01(value)));
        public void InitializeFromDefault() { NormalizeRange(); _value=Normalize(_defaultValue); _initialized=true; }
        public void ResetToDefault() { EnsureInitialized(); TrySetValue(_defaultValue); }
        public void OnBeforeSerialize() { } public void OnAfterDeserialize() { _initialized=false; _value=default; }
        private float Normalize(float value) => Mathf.Clamp(float.IsFinite(value)?value:0f,_min,_max);
        private void NormalizeRange() { if (!float.IsFinite(_min)) _min=0f; if (!float.IsFinite(_max)||_max<=_min) _max=_min+1f; }
        private void EnsureInitialized() { if (!_initialized) InitializeFromDefault(); }
    }
}
