using System;
using UnityEngine;

namespace qoooo.Parameters
{
    [Serializable]
    public sealed class OneshotParameter : IValue<bool>, IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [NonSerialized] private bool _value;
        [NonSerialized] private bool _initialized;
        [NonSerialized] private bool _releaseNextFrame;
        public OneshotParameter() { }
        public OneshotParameter(string id, string displayName) { _id=id; _displayName=displayName; }
        public string Id => _id; public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public MidiParameterKind Kind => MidiParameterKind.Oneshot;
        public bool Value { get { EnsureInitialized(); return _value; } }
        public event Action<bool> Changed;
        public bool TrySetValue(bool value) { EnsureInitialized(); if (_value == value) return false; _value=value; Changed?.Invoke(value); return true; }
        /// <summary>UIなど、押下・離上がない入力から一フレームだけ発火する。</summary>
        public void Trigger() { TrySetValue(true); _releaseNextFrame = true; }
        public void BeginFrame() { if (_releaseNextFrame) { _releaseNextFrame = false; TrySetValue(false); } }
        public void InitializeFromDefault() { _value=false; _releaseNextFrame=false; _initialized=true; }
        public void ResetToDefault() { _releaseNextFrame=false; TrySetValue(false); }
        public void OnBeforeSerialize() { } public void OnAfterDeserialize() { _value=false; _releaseNextFrame=false; _initialized=false; }
        private void EnsureInitialized() { if (!_initialized) InitializeFromDefault(); }
    }
}
