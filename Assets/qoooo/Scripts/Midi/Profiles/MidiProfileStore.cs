using System;
using System.Collections.Generic;
using PrefsGUI;
using qoooo.Midi.Runtime;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using UnityEngine;

namespace qoooo.Midi.Profiles
{
    /// <summary>Trip26のAPC mappingと保存対象ParameterをPrefsへ保存する。</summary>
    // mappingとParameter stateのprofile snapshot、dirty state、Prefs recordを所有する。
    // Prepare/Commit/Abortは明示的なQoooo Saveだけに参加し、autosaveではない。
    public sealed class MidiProfileStore : MonoBehaviour, IPrefsSaveParticipant
    {
        private const string MappingKey = "qoooo.midi.trip26.mapping.v2";
        private const string StateKey = "qoooo.midi.trip26.state";
        private static readonly HashSet<string> RetiredParameterIds = new(StringComparer.Ordinal)
        {
            "poster.model-layout",
            "poster.typography-layout",
            "text.layout",
            "text.rebuild",
            "text.transform-mode",
            "text.blink",
            "text.animation-speed"
        };
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
            if(IsCompatible(mapping))
            {
                mapping=Copy(mapping);
                RemoveRetiredBindings(mapping);
                MigrateLiveCameraLayout(mapping, _sceneDefaultMapping);
                AddMissingBindings(mapping, _sceneDefaultMapping);
                _controller.ImportMapping(mapping);
            }
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
        private static void RemoveRetiredBindings(ApcMidiMappingData mapping)
        {
            mapping.Toggles?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.Radios?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.Oneshots?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.Momentaries?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.States?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.Sequences?.RemoveAll(binding => IsRetired(binding?.ParameterId));
            mapping.Floats?.RemoveAll(binding => IsRetired(binding?.ParameterId));
        }
        private static void AddMissingBindings(ApcMidiMappingData mapping, ApcMidiMappingData sceneDefaults)
        {
            mapping.Toggles ??= new List<Midi.Bindings.ToggleMidiBinding>();
            mapping.Radios ??= new List<Midi.Bindings.RadioMidiBinding>();
            AddMissing(mapping.Toggles, sceneDefaults.Toggles, binding => binding.ParameterId);
            AddMissing(mapping.Radios, sceneDefaults.Radios, binding => binding.ParameterId);
        }
        private static void MigrateLiveCameraLayout(ApcMidiMappingData mapping, ApcMidiMappingData sceneDefaults)
        {
            var motion = mapping.Radios?.Find(binding => binding?.ParameterId == "camera.live.motion-mode");
            var legacy = motion?.OptionButtons?.Count is 5 or 24
                         || HasLegacyToggle(mapping, "camera.live.enabled", 7, 0, 0)
                         || HasLegacyToggle(mapping, "layer.live-camera.visible", 1, 0, 0);
            if(!legacy)return;

            mapping.Toggles?.RemoveAll(binding => IsLiveCameraLayoutParameter(binding?.ParameterId));
            mapping.Radios?.RemoveAll(binding => IsLiveCameraLayoutParameter(binding?.ParameterId));
            AddMissingBindings(mapping, sceneDefaults);
        }
        private static bool HasLegacyToggle(ApcMidiMappingData mapping, string parameterId,
            int page, int row, int column)
        {
            var binding=mapping.Toggles?.Find(value => value?.ParameterId==parameterId);
            return binding?.Button.Assigned==true && binding.Button.Page==page
                   && binding.Button.Row==row && binding.Button.Column==column;
        }
        private static bool IsLiveCameraLayoutParameter(string parameterId)
        {
            return parameterId is "camera.live.enabled" or "camera.live.motion-enabled" or "camera.live.flip-x"
                or "camera.live.motion-mode" or "camera.live.framing" or "camera.live.look"
                or "layer.live-camera.visible" or "layer.poster.visible";
        }
        private static void AddMissing<T>(List<T> target, IEnumerable<T> sceneDefaults, Func<T, string> parameterId)
            where T : class
        {
            foreach(var binding in sceneDefaults ?? Array.Empty<T>())
            {
                var id=binding==null?null:parameterId(binding);
                if(!ShouldMergeSceneDefault(id)
                    || target.Exists(value => value != null && parameterId(value)==id))continue;
                target.Add(JsonUtility.FromJson<T>(JsonUtility.ToJson(binding)));
            }
        }
        private static bool ShouldMergeSceneDefault(string parameterId)
        {
            return !string.IsNullOrEmpty(parameterId)
                   && (parameterId.StartsWith("camera.live.",StringComparison.Ordinal)
                       || parameterId is "layer.live-camera.visible" or "layer.poster.visible");
        }
        private static bool IsRetired(string parameterId) => RetiredParameterIds.Contains(parameterId ?? string.Empty);
        private static bool IsCompatible(ApcMidiMappingData value) => value != null && value.HasSavedData && value.SchemaVersion==1 && value.ProfileId=="trip26";
        private static bool IsCompatible(MidiParameterStateData value) => value != null && value.HasSavedData && value.SchemaVersion==1 && value.ProfileId=="trip26";
        private static T Copy<T>(T value) where T : new() => value==null ? new T() : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
