using System;
using UnityEngine;

namespace qoooo.Parameters
{
    [Serializable]
    public sealed class MomentaryParameter : IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [NonSerialized] private bool _triggered;
        [NonSerialized] private int _triggerCount;
        public MomentaryParameter() { }
        public MomentaryParameter(string id, string displayName) { _id=id; _displayName=displayName; }
        public string Id => _id; public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public MidiParameterKind Kind => MidiParameterKind.Momentary;
        public bool WasTriggeredThisFrame => _triggered; public int TriggerCountThisFrame => _triggerCount;
        public event Action Triggered;
        public void Trigger() { _triggered=true; _triggerCount++; Triggered?.Invoke(); }
        public void BeginFrame() { _triggered=false; _triggerCount=0; }
        public void InitializeFromDefault() => BeginFrame(); public void ResetToDefault() => BeginFrame();
        public void OnBeforeSerialize() { } public void OnAfterDeserialize() => BeginFrame();
    }
}
