using System;
using System.Collections.Generic;
using PrefsGUI;
using qoooo.Parameters;
using qoooo.View;
using UnityEngine;

namespace qoooo.Midi
{
    /// <summary>Trip26のAPC mappingと保存対象ParameterをPrefsへ保存する。</summary>
    public sealed class MidiProfileStore : MonoBehaviour, IPrefsSaveParticipant
    {
        private const string MappingKey = "qoooo.midi.trip26.mapping";
        private const string StateKey = "qoooo.midi.trip26.state";
        [SerializeField] private ApcMiniMk2ParameterController _controller;
        [SerializeField] private MidiParameterRegistry _registry;
        private readonly PrefsAny<ApcMidiMappingData> _mappingPrefs = new(MappingKey);
        private readonly PrefsAny<MidiParameterStateData> _statePrefs = new(StateKey);
        private ApcMidiMappingData _sceneDefaultMapping;
        private MidiParameterStateData _lastSavedState;
        private ApcMidiMappingData _lastSavedMapping;
        private bool _initialized;
        public bool IsDirty { get; private set; }
        public event Action DirtyChanged;
        public void Configure(ApcMiniMk2ParameterController controller, MidiParameterRegistry registry) { _controller=controller; _registry=registry; }
        public void Initialize()
        {
            if(_initialized || _controller==null || _registry==null)return;
            _sceneDefaultMapping=Copy(_controller.ExportMapping());
            var mapping=_mappingPrefs.Get();
            if(IsCompatible(mapping)) _controller.ImportMapping(mapping);
            var state=_statePrefs.Get();
            if(IsCompatible(state)) ApplyState(state);
            _lastSavedMapping=Copy(IsCompatible(mapping) ? mapping : _sceneDefaultMapping);
            _lastSavedState=Copy(IsCompatible(state) ? state : CaptureState());
            Subscribe(); _initialized=true; SetDirty(false);
            Debug.Log($"[MIDI] Profile loaded. Mapping: {(IsCompatible(mapping) ? "Prefs" : "Scene defaults")}; State: {(IsCompatible(state) ? "Prefs" : "defaults")}.",this);
        }
        public void PrepareSave()
        {
            if(!_initialized)return;
            _mappingPrefs.Set(CaptureMapping()); _statePrefs.Set(CaptureState());
        }
        public void CommitSave()
        {
            if(!_initialized)return;
            _lastSavedMapping=CaptureMapping(); _lastSavedState=CaptureState(); SetDirty(false);
            Debug.Log("[MIDI] Profile staged for Prefs.Save.",this);
        }
        public void AbortSave() { }
        public void Revert()
        {
            if(!_initialized)return; _controller.ImportMapping(Copy(_lastSavedMapping)); ApplyState(Copy(_lastSavedState)); SetDirty(false);
            Debug.Log("[MIDI] Reverted to last saved profile.",this);
        }
        public void ResetMapping()
        {
            if(!_initialized)return; _controller.ImportMapping(Copy(_sceneDefaultMapping)); MarkDirty();
            Debug.Log("[MIDI] Mapping reset to scene defaults. Press Save to persist it.",this);
        }
        public void ResetValues()
        {
            if(!_initialized)return; foreach(var parameter in _registry.Parameters) parameter.ResetToDefault(); MarkDirty();
            Debug.Log("[MIDI] Parameter values reset to scene defaults. Press Save to persist them.",this);
        }
        private void Subscribe()
        {
            _controller.MappingChanged += MarkDirty;
            foreach(var parameter in _registry.Parameters)
            {
                switch(parameter)
                {
                    case BoolParameter value: value.Changed += _ => MarkDirty(); break;
                    case RadioParameter value: value.Changed += _ => MarkDirty(); break;
                    case StateParameter value: value.Changed += _ => MarkDirty(); break;
                    case FloatParameter value: value.Changed += _ => MarkDirty(); break;
                    case SequenceParameter value: value.Changed += MarkDirty; break;
                }
            }
        }
        private ApcMidiMappingData CaptureMapping() => Copy(_controller.ExportMapping());
        private MidiParameterStateData CaptureState()
        {
            var result=new MidiParameterStateData { HasSavedData = true };
            foreach(var parameter in _registry.Parameters)
            {
                var value=new MidiParameterValueData { Id=parameter.Id, Kind=(int)parameter.Kind };
                switch(parameter)
                {
                    case BoolParameter p: value.BoolValue=p.Value; break;
                    case RadioParameter p: value.IntValue=p.Value; break;
                    case StateParameter p: value.IntValue=p.Value; break;
                    case FloatParameter p: value.FloatValue=p.Value; break;
                    case SequenceParameter p: value.Steps=new List<bool>(p.Steps); value.Division=(int)p.Division; break;
                    default: continue; // Oneshot/Momentary は transient
                }
                result.Values.Add(value);
            }
            return result;
        }
        private void ApplyState(MidiParameterStateData state)
        {
            foreach(var entry in state?.Values ?? new List<MidiParameterValueData>())
            {
                if(entry==null || !_registry.TryGet(entry.Id,out var parameter) || (int)parameter.Kind!=entry.Kind)continue;
                switch(parameter)
                {
                    case BoolParameter p: p.TrySetValue(entry.BoolValue); break;
                    case RadioParameter p: p.TrySetValue(entry.IntValue); break;
                    case StateParameter p: p.TrySetValue(entry.IntValue); break;
                    case FloatParameter p: p.TrySetValue(entry.FloatValue); break;
                    case SequenceParameter p:
                        p.TrySetDivision((SequenceDivision)entry.Division);
                        for(var i=0;i<entry.Steps.Count && i<p.StepCount;i++)p.TrySetStep(i,entry.Steps[i]);
                        break;
                }
            }
        }
        private void MarkDirty() => SetDirty(true);
        private void SetDirty(bool value) { if(IsDirty==value)return; IsDirty=value; DirtyChanged?.Invoke(); }
        private static bool IsCompatible(ApcMidiMappingData value) => value != null && value.HasSavedData && value.SchemaVersion==1 && value.ProfileId=="trip26";
        private static bool IsCompatible(MidiParameterStateData value) => value != null && value.HasSavedData && value.SchemaVersion==1 && value.ProfileId=="trip26";
        private static T Copy<T>(T value) where T : new() => value==null ? new T() : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
