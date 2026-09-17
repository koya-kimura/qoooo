using System;
using System.Collections.Generic;
using qoooo.Parameters;
using qoooo.Timing;
using qoooo.Util;
using UnityEngine;

namespace qoooo.Midi
{
    /// <summary>APC mini mk2 の物理eventを型付きParameterへ適用する。値の所有はしない。</summary>
    [DefaultExecutionOrder(-90)]
    public sealed class ApcMiniMk2ParameterController : MonoBehaviour
    {
        [SerializeField] private MinisMidiInput _input;
        [SerializeField] private MidiParameterRegistry _parameters;
        private IBpmSource _bpmSource;
        [SerializeField] private List<ToggleMidiBinding> _toggles = new();
        [SerializeField] private List<RadioMidiBinding> _radios = new();
        [SerializeField] private List<OneshotMidiBinding> _oneshots = new();
        [SerializeField] private List<MomentaryMidiBinding> _momentaries = new();
        [SerializeField] private List<StateMidiBinding> _states = new();
        [SerializeField] private List<SequenceMidiBinding> _sequences = new();
        [SerializeField] private List<FloatMidiBinding> _floats = new();
        [SerializeField] private List<FaderButtonFunction> _faderButtonFunctions = new()
        {
            FaderButtonFunction.Mute, FaderButtonFunction.Random, FaderButtonFunction.Mute,
            FaderButtonFunction.Random, FaderButtonFunction.Mute, FaderButtonFunction.Random,
            FaderButtonFunction.Mute, FaderButtonFunction.Random, FaderButtonFunction.Mute
        };
        [SerializeField] private SequenceDivision _faderRandomDivision = SequenceDivision.Quarter;
        [SerializeField] private bool _logResolvedInput = true;

        private readonly Dictionary<int, Action> _buttons = new();
        private readonly Dictionary<int, FloatParameter> _faders = new();
        private readonly Dictionary<int, OneshotParameter> _pressedOneshots = new();
        private readonly bool[] _pickupArmed = new bool[ApcMiniMk2Constants.FaderCount];
        private readonly float[] _previousPhysicalFader = new float[ApcMiniMk2Constants.FaderCount];
        private readonly FaderButtonMode[] _faderModes = new FaderButtonMode[ApcMiniMk2Constants.FaderCount];
        private long _previousRandomStep = long.MinValue;
        private int _page;
        private bool _configured;
        private string _listeningToggleParameterId;
        private MidiParameterKind _listeningKind;
        private int _listeningSlotIndex;
        private float _listenFaderBaseline;
        private bool _hasListenFaderBaseline;
        public int CurrentPage => _page;
        public FaderButtonMode GetFaderMode(int index)
            => index < 0 || index >= _faderModes.Length ? FaderButtonMode.Normal : _faderModes[index];
        public FaderButtonFunction GetFaderButtonFunction(int index)
        {
            _faderButtonFunctions=NormalizeFunctions(_faderButtonFunctions);
            return index < 0 || index >= _faderButtonFunctions.Count ? FaderButtonFunction.Mute : _faderButtonFunctions[index];
        }
        public SequenceDivision FaderRandomDivision => _faderRandomDivision;
        public void SetFaderButtonFunction(int index,FaderButtonFunction value)
        {
            if(index<0 || index>=ApcMiniMk2Constants.FaderButtonCount)return;
            _faderButtonFunctions=NormalizeFunctions(_faderButtonFunctions);
            if(_faderButtonFunctions[index]==value)return;
            _faderButtonFunctions[index]=value;
            if(_faderModes[index]!=FaderButtonMode.Normal){_faderModes[index]=FaderButtonMode.Normal;_pickupArmed[index]=true;}
            MappingChanged?.Invoke();
        }
        public void SetFaderRandomDivision(SequenceDivision value)
        {
            if(!System.Enum.IsDefined(typeof(SequenceDivision),value))value=SequenceDivision.Quarter;
            if(_faderRandomDivision==value)return;
            _faderRandomDivision=value; _previousRandomStep=long.MinValue; MappingChanged?.Invoke();
        }
        public bool IsListening => !string.IsNullOrEmpty(_listeningToggleParameterId);
        public string ListeningParameterId => _listeningToggleParameterId;
        public event Action MappingChanged;

        public ApcMidiMappingData ExportMapping()
        {
            return new ApcMidiMappingData {
                HasSavedData = true,
                Toggles = Copy(_toggles), Radios = Copy(_radios), Oneshots = Copy(_oneshots), Momentaries = Copy(_momentaries),
                States = Copy(_states), Sequences = Copy(_sequences), Floats = Copy(_floats),
                FaderButtonFunctions = Copy(_faderButtonFunctions), FaderRandomDivision = (int)_faderRandomDivision
            };
        }

        public void ImportMapping(ApcMidiMappingData value)
        {
            if (value == null) return;
            _toggles = Copy(value.Toggles); _radios = Copy(value.Radios); _oneshots = Copy(value.Oneshots); _momentaries = Copy(value.Momentaries);
            _states = Copy(value.States); _sequences = Copy(value.Sequences); _floats = Copy(value.Floats);
            _faderButtonFunctions = NormalizeFunctions(Copy(value.FaderButtonFunctions));
            _faderRandomDivision = System.Enum.IsDefined(typeof(SequenceDivision), value.FaderRandomDivision) ? (SequenceDivision)value.FaderRandomDivision : SequenceDivision.Quarter;
            ResetFaderModes(); CancelListen(); Rebuild(); MappingChanged?.Invoke();
        }

        public IReadOnlyList<ToggleMidiBinding> ToggleBindings => _toggles;

        public bool BeginToggleListen(string parameterId)
        {
            return BeginButtonListen(parameterId, MidiParameterKind.Toggle, 0);
        }

        public bool BeginButtonListen(string parameterId, MidiParameterKind kind, int slotIndex)
        {
            if (kind == MidiParameterKind.Float || !TryParameter(parameterId, kind, out _) || slotIndex < 0) return false;
            _listeningToggleParameterId = parameterId; _listeningKind = kind; _listeningSlotIndex = slotIndex; _hasListenFaderBaseline = false;
            Debug.Log($"[MIDI] Button listen started for '{parameterId}' slot {slotIndex}. Select an APC page then press a grid pad.", this);
            return true;
        }

        public bool BeginFaderListen(string parameterId)
        {
            if (!TryParameter(parameterId, MidiParameterKind.Float, out _)) return false;
            _listeningToggleParameterId = parameterId; _listeningKind = MidiParameterKind.Float; _listeningSlotIndex = 0; _hasListenFaderBaseline = false;
            Debug.Log($"[MIDI] Fader listen started for '{parameterId}'. Move an APC fader at least 0.05.", this);
            return true;
        }

        public void CancelListen()
        {
            if (!IsListening) return;
            Debug.Log($"[MIDI] Listen cancelled for '{_listeningToggleParameterId}'.", this);
            _listeningToggleParameterId = null;
            _hasListenFaderBaseline = false;
        }

        public bool ClearToggleBinding(string parameterId)
        {
            return ClearButtonBinding(parameterId, MidiParameterKind.Toggle, 0);
        }

        public bool ClearButtonBinding(string parameterId, MidiParameterKind kind, int slotIndex)
        {
            var slot = GetButtonSlotReference(parameterId, kind, slotIndex, false);
            if (slot == null || !slot.Value.Assigned) return false;
            SetButtonSlot(parameterId, kind, slotIndex, default, false);
            Rebuild(); MappingChanged?.Invoke();
            Debug.Log($"[MIDI] Cleared pad mapping for '{parameterId}' slot {slotIndex}.", this);
            return true;
        }

        public bool ClearFaderBinding(string parameterId)
        {
            foreach(var binding in _floats)
            {
                if(binding == null || binding.ParameterId != parameterId || !binding.Assigned) continue;
                binding.Assigned=false; Rebuild(); MappingChanged?.Invoke(); return true;
            }
            return false;
        }

        public bool TryGetButtonSlot(string parameterId, MidiParameterKind kind, int slotIndex, out ApcButtonSlot slot)
        {
            var value=GetButtonSlotReference(parameterId,kind,slotIndex,false);
            if(value.HasValue) { slot=value.Value; return true; } slot=default; return false;
        }

        public bool TryGetFaderIndex(string parameterId, out int index)
        {
            foreach(var binding in _floats) if(binding != null && binding.ParameterId == parameterId && binding.Assigned) { index=binding.FaderIndex; return true; }
            index=-1; return false;
        }

        public void Configure(MinisMidiInput input, MidiParameterRegistry parameters, IBpmSource bpmSource)
        { _input=input; _parameters=parameters; _bpmSource=bpmSource; _configured=true; }

        private void OnEnable()
        {
            if (_input != null) Subscribe();
        }

        private void Start()
        {
            if (_input == null) Debug.LogError("[MIDI] APC Controller requires MinisMidiInput.", this);
            if (_parameters == null) Debug.LogError("[MIDI] APC Controller requires MidiParameterRegistry.", this);
            if (!_configured) Rebuild();
            if (_input != null) Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe(); ReleaseOneshots(); _buttons.Clear(); _faders.Clear();
        }

        public void Rebuild()
        {
            ReleaseOneshots(); _buttons.Clear(); _faders.Clear();
            if (_parameters == null) return;
            AddButtons(_toggles, MidiParameterKind.Toggle, (p, _) => { var value=(BoolParameter)p; return () => value.TrySetValue(!value.Value); });
            AddButtons(_oneshots, MidiParameterKind.Oneshot, (p, _) => { var value=(OneshotParameter)p; return () => value.TrySetValue(true); });
            AddButtons(_momentaries, MidiParameterKind.Momentary, (p, _) => { var value=(MomentaryParameter)p; return value.Trigger; });
            AddButtons(_states, MidiParameterKind.State, (p, _) => { var value=(StateParameter)p; return () => value.Next(); });
            foreach (var binding in _radios ?? new List<RadioMidiBinding>())
            {
                if (!TryParameter(binding?.ParameterId, MidiParameterKind.Radio, out var raw)) continue;
                var value=(RadioParameter)raw;
                for(var index=0; index<(binding.OptionButtons?.Count??0); index++) { var slot=binding.OptionButtons[index]; var captured=index; AddButton(slot, () => value.TrySetValue(captured)); }
            }
            foreach (var binding in _sequences ?? new List<SequenceMidiBinding>())
            {
                if (!TryParameter(binding?.ParameterId, MidiParameterKind.Sequence, out var raw)) continue;
                var value=(SequenceParameter)raw;
                for(var index=0; index<(binding.StepButtons?.Count??0); index++) { var slot=binding.StepButtons[index]; var captured=index; AddButton(slot, () => value.ToggleStep(captured)); }
            }
            foreach(var binding in _floats ?? new List<FloatMidiBinding>())
            {
                if(binding == null || !binding.Assigned || binding.FaderIndex < 0 || binding.FaderIndex >= ApcMiniMk2Constants.FaderCount || !TryParameter(binding.ParameterId,MidiParameterKind.Float,out var raw)) continue;
                if(_faders.ContainsKey(binding.FaderIndex)) { Debug.LogWarning($"[MIDI] Fader {binding.FaderIndex} is assigned more than once; later binding is ignored.",this); continue; }
                var parameter=(FloatParameter)raw;
                _faders.Add(binding.FaderIndex,parameter); _pickupArmed[binding.FaderIndex]=true;
            }
            Debug.Log($"[MIDI] APC mapping active: {_buttons.Count} pad slots, {_faders.Count} faders.",this);
        }

        private void Subscribe() { _input.NoteOn -= HandleNoteOn; _input.NoteOff -= HandleNoteOff; _input.ControlChange -= HandleControlChange; _input.ConnectionChanged -= HandleInputConnectionChanged; _input.NoteOn += HandleNoteOn; _input.NoteOff += HandleNoteOff; _input.ControlChange += HandleControlChange; _input.ConnectionChanged += HandleInputConnectionChanged; }
        private void Unsubscribe() { if(_input==null)return; _input.NoteOn -= HandleNoteOn; _input.NoteOff -= HandleNoteOff; _input.ControlChange -= HandleControlChange; _input.ConnectionChanged -= HandleInputConnectionChanged; }
        private void HandleInputConnectionChanged(bool connected)
        {
            if (connected) return;
            ReleaseOneshots();
            _hasListenFaderBaseline = false;
            for (var index=0; index<_pickupArmed.Length; index++) _pickupArmed[index] = true;
        }
        private void HandleNoteOn(int note,float velocity)
        {
            if(velocity <= 0f) { HandleNoteOff(note); return; }
            if(ApcMiniMk2Constants.IsSceneLaunchNote(note)) { _page=note-ApcMiniMk2Constants.SceneLaunchNoteFirst; if(_logResolvedInput)Debug.Log($"[MIDI] APC page: {_page}.",this); return; }
            if(ApcMiniMk2Constants.TryGetFaderButtonIndex(note,out var faderButton)) { ToggleFaderMode(faderButton); return; }
            if(!ApcMiniMk2Layout.TryGetGridPosition(note,out var row,out var column)) return;
            if (IsListening)
            {
                AssignListeningButton(new ApcButtonSlot { Assigned = true, Page = _page, Row = row, Column = column });
                return;
            }
            var key=ApcMiniMk2Layout.GetCellKey(_page,row,column);
            if(!_buttons.TryGetValue(key,out var action)) return;
            action();
            if(TryGetOneshot(key,out var oneshot)) _pressedOneshots[note]=oneshot;
            if(_logResolvedInput) Debug.Log($"[MIDI] APC pad: page={_page}, row={row}, col={column}.",this);
        }
        private void HandleNoteOff(int note) { if(_pressedOneshots.Remove(note,out var oneshot)) oneshot.TrySetValue(false); }
        private void HandleControlChange(int cc,float normalized)
        {
            if (IsListening && _listeningKind == MidiParameterKind.Float)
            {
                if(!ApcMiniMk2Constants.TryGetFaderIndex(cc,out var listeningIndex)) return;
                if(!_hasListenFaderBaseline) { _listenFaderBaseline=normalized; _hasListenFaderBaseline=true; return; }
                if(Mathf.Abs(normalized-_listenFaderBaseline)<0.05f)return;
                AssignListeningFader(listeningIndex,normalized); return;
            }
            if(!ApcMiniMk2Constants.TryGetFaderIndex(cc,out var index) || !_faders.TryGetValue(index,out var parameter)) return;
            normalized=Mathf.Clamp01(normalized);
            // Mute / Random は対応するFader buttonだけで解除する。
            // mode中の物理Fader移動はparameterを書き換えない。
            if(_faderModes[index] != FaderButtonMode.Normal) return;
            if(_pickupArmed[index])
            {
                var target=parameter.NormalizedValue; var previous=_previousPhysicalFader[index];
                if(Mathf.Abs(normalized-target)>0.02f && (previous-target)*(normalized-target)>0f) { _previousPhysicalFader[index]=normalized; return; }
                _pickupArmed[index]=false; Debug.Log($"[MIDI] Fader {index} pickup acquired.",this);
            }
            _previousPhysicalFader[index]=normalized; parameter.TrySetNormalizedValue(normalized);
        }
        private void AddButtons<T>(IEnumerable<T> bindings,MidiParameterKind kind,Func<IMidiBindableParameter,T,Action> create) where T:class
        { foreach(var binding in bindings ?? Array.Empty<T>()) { if(binding==null)continue; var id=BindingId(binding); if(!TryParameter(id,kind,out var parameter))continue; AddButton(BindingSlot(binding),create(parameter,binding)); } }
        private static string BindingId<T>(T binding) where T:class => binding switch { ToggleMidiBinding b=>b.ParameterId, OneshotMidiBinding b=>b.ParameterId, MomentaryMidiBinding b=>b.ParameterId, StateMidiBinding b=>b.ParameterId, _=>null };
        private static ApcButtonSlot BindingSlot<T>(T binding) where T:class => binding switch { ToggleMidiBinding b=>b.Button, OneshotMidiBinding b=>b.Button, MomentaryMidiBinding b=>b.Button, StateMidiBinding b=>b.Button, _=>default };
        private void AddButton(ApcButtonSlot slot,Action action) { if(!slot.IsValid || action==null)return; if(!_buttons.TryAdd(slot.CellKey,action)) Debug.LogWarning($"[MIDI] Duplicate APC pad mapping for cell {slot.CellKey}; later mapping ignored.",this); }
        private bool TryParameter(string id,MidiParameterKind kind,out IMidiBindableParameter value) { if(_parameters != null && _parameters.TryGet(id,out value) && value.Kind==kind)return true; value=null; if(!string.IsNullOrWhiteSpace(id))Debug.LogWarning($"[MIDI] Binding '{id}' is unresolved or has a type mismatch.",this);return false; }
        private bool TryGetOneshot(int key,out OneshotParameter value) { value=null; foreach(var binding in _oneshots ?? new List<OneshotMidiBinding>()) if(binding!=null && binding.Button.IsValid && binding.Button.CellKey==key && TryParameter(binding.ParameterId,MidiParameterKind.Oneshot,out var raw)) { value=(OneshotParameter)raw;return true; } return false; }
        private void ReleaseOneshots() { foreach(var value in _pressedOneshots.Values)value.TrySetValue(false); _pressedOneshots.Clear(); }
        private void Update()
        {
            if(_bpmSource == null)return;
            var factor=_faderRandomDivision==SequenceDivision.Quarter?1:_faderRandomDivision==SequenceDivision.Eighth?2:4;
            var step=(long)System.Math.Floor(_bpmSource.Beat*factor);
            if(step==_previousRandomStep)return; _previousRandomStep=step;
            for(var index=0;index<_faderModes.Length;index++)
            {
                if(_faderModes[index] != FaderButtonMode.Random || !_faders.TryGetValue(index,out var parameter))continue;
                parameter.TrySetNormalizedValue(Random01(step + 1d,index + 1000d) < 0.5f ? 0f : 1f);
            }
        }
        private void ToggleFaderMode(int index)
        {
            if(index<0 || index>=_faderModes.Length)return;
            var function=index<_faderButtonFunctions.Count?_faderButtonFunctions[index]:(index%2==0?FaderButtonFunction.Mute:FaderButtonFunction.Random);
            var next=function==FaderButtonFunction.Mute?FaderButtonMode.Mute:FaderButtonMode.Random;
            _faderModes[index]=_faderModes[index]==next?FaderButtonMode.Normal:next;
            if(_faders.TryGetValue(index,out var parameter))
            {
                if(_faderModes[index]==FaderButtonMode.Mute)parameter.TrySetNormalizedValue(0f);
                else if(_faderModes[index]==FaderButtonMode.Random)_previousRandomStep=long.MinValue;
                else _pickupArmed[index]=true;
            }
            Debug.Log($"[MIDI] Fader {index + 1} mode: {_faderModes[index]}.",this);
        }
        private void ResetFaderModes()
        {
            for(var index=0;index<_faderModes.Length;index++) { _faderModes[index]=FaderButtonMode.Normal; _pickupArmed[index]=true; }
            _previousRandomStep=long.MinValue;
        }
        private static float Random01(double x, double y) => Pcg.Pcg01(new Vector2((float)x, (float)y)).x;
        private static List<FaderButtonFunction> NormalizeFunctions(List<FaderButtonFunction> source)
        {
            var result=source ?? new List<FaderButtonFunction>();
            while(result.Count<ApcMiniMk2Constants.FaderButtonCount)result.Add(result.Count%2==0?FaderButtonFunction.Mute:FaderButtonFunction.Random);
            if(result.Count>ApcMiniMk2Constants.FaderButtonCount)result.RemoveRange(ApcMiniMk2Constants.FaderButtonCount,result.Count-ApcMiniMk2Constants.FaderButtonCount);
            return result;
        }
        private ToggleMidiBinding FindToggleBinding(string parameterId)
        {
            foreach (var binding in _toggles ?? new List<ToggleMidiBinding>())
                if (binding != null && binding.ParameterId == parameterId) return binding;
            return null;
        }
        private void AssignListeningButton(ApcButtonSlot slot)
        {
            var parameterId = _listeningToggleParameterId;
            var kind = _listeningKind; var slotIndex = _listeningSlotIndex;
            CancelListen();
            ClearButtonAt(slot.CellKey);
            SetButtonSlot(parameterId, kind, slotIndex, slot, true);
            Rebuild(); MappingChanged?.Invoke();
            Debug.Log($"[MIDI] Listen assigned '{parameterId}' slot {slotIndex} to page={slot.Page}, row={slot.Row}, col={slot.Column}.", this);
        }
        private void AssignListeningFader(int index,float value)
        {
            var parameterId=_listeningToggleParameterId; CancelListen();
            foreach(var binding in _floats) if(binding != null && binding.Assigned && binding.FaderIndex==index) binding.Assigned=false;
            var target=_floats.Find(x=>x != null && x.ParameterId==parameterId);
            if(target==null) { target=new FloatMidiBinding { ParameterId=parameterId }; _floats.Add(target); }
            target.Assigned=true; target.FaderIndex=index; _previousPhysicalFader[index]=value; _pickupArmed[index]=true;
            Rebuild(); MappingChanged?.Invoke();
            Debug.Log($"[MIDI] Fader listen assigned '{parameterId}' to fader {index}. Pickup is armed.",this);
        }
        private void ClearButtonAt(int cellKey)
        {
            foreach(var item in _toggles) if(item?.Button.IsValid==true && item.Button.CellKey==cellKey)item.Button=default;
            foreach(var item in _oneshots) if(item?.Button.IsValid==true && item.Button.CellKey==cellKey)item.Button=default;
            foreach(var item in _momentaries) if(item?.Button.IsValid==true && item.Button.CellKey==cellKey)item.Button=default;
            foreach(var item in _states) if(item?.Button.IsValid==true && item.Button.CellKey==cellKey)item.Button=default;
            ClearListCells(_radios,cellKey,x=>x.OptionButtons); ClearListCells(_sequences,cellKey,x=>x.StepButtons);
        }
        private static void ClearListCells<T>(List<T> bindings,int cellKey,Func<T,List<ApcButtonSlot>> get) where T:class
        { foreach(var item in bindings) { var slots=item==null?null:get(item); if(slots==null)continue; for(var i=0;i<slots.Count;i++)if(slots[i].IsValid&&slots[i].CellKey==cellKey)slots[i]=default; } }
        private ApcButtonSlot? GetButtonSlotReference(string id,MidiParameterKind kind,int index,bool create)
        {
            switch(kind)
            {
                case MidiParameterKind.Toggle: { var b=FindOrCreate(_toggles,id,create,()=>new ToggleMidiBinding{ParameterId=id}); return b?.Button; }
                case MidiParameterKind.Oneshot: { var b=FindOrCreate(_oneshots,id,create,()=>new OneshotMidiBinding{ParameterId=id}); return b?.Button; }
                case MidiParameterKind.Momentary: { var b=FindOrCreate(_momentaries,id,create,()=>new MomentaryMidiBinding{ParameterId=id}); return b?.Button; }
                case MidiParameterKind.State: { var b=FindOrCreate(_states,id,create,()=>new StateMidiBinding{ParameterId=id}); return b?.Button; }
                case MidiParameterKind.Radio: return GetListSlot(FindOrCreate(_radios,id,create,()=>new RadioMidiBinding{ParameterId=id}),index,create,x=>x.OptionButtons);
                case MidiParameterKind.Sequence: return GetListSlot(FindOrCreate(_sequences,id,create,()=>new SequenceMidiBinding{ParameterId=id}),index,create,x=>x.StepButtons);
                default:return null;
            }
        }
        private void SetButtonSlot(string id,MidiParameterKind kind,int index,ApcButtonSlot slot,bool create)
        {
            switch(kind)
            {
                case MidiParameterKind.Toggle: { var b=FindOrCreate(_toggles,id,create,()=>new ToggleMidiBinding{ParameterId=id});if(b!=null)b.Button=slot;break; }
                case MidiParameterKind.Oneshot: { var b=FindOrCreate(_oneshots,id,create,()=>new OneshotMidiBinding{ParameterId=id});if(b!=null)b.Button=slot;break; }
                case MidiParameterKind.Momentary: { var b=FindOrCreate(_momentaries,id,create,()=>new MomentaryMidiBinding{ParameterId=id});if(b!=null)b.Button=slot;break; }
                case MidiParameterKind.State: { var b=FindOrCreate(_states,id,create,()=>new StateMidiBinding{ParameterId=id});if(b!=null)b.Button=slot;break; }
                case MidiParameterKind.Radio: SetListSlot(FindOrCreate(_radios,id,create,()=>new RadioMidiBinding{ParameterId=id}),index,slot,create,x=>x.OptionButtons);break;
                case MidiParameterKind.Sequence:SetListSlot(FindOrCreate(_sequences,id,create,()=>new SequenceMidiBinding{ParameterId=id}),index,slot,create,x=>x.StepButtons);break;
            }
        }
        private static T FindOrCreate<T>(List<T> values,string id,bool create,Func<T> factory) where T:class
        { var field=typeof(T).GetField("ParameterId"); foreach(var value in values)if(value!=null&&(string)field.GetValue(value)==id)return value; if(!create)return null; var result=factory();values.Add(result);return result; }
        private static ApcButtonSlot? GetListSlot<T>(T value,int index,bool create,Func<T,List<ApcButtonSlot>> get) where T:class
        { if(value==null||index<0)return null; var list=get(value)??new List<ApcButtonSlot>(); if(create)while(list.Count<=index)list.Add(default); return index<list.Count?list[index]:null; }
        private static void SetListSlot<T>(T value,int index,ApcButtonSlot slot,bool create,Func<T,List<ApcButtonSlot>> get) where T:class
        { if(value==null||index<0)return;var list=get(value);if(list==null)return;if(create)while(list.Count<=index)list.Add(default);if(index<list.Count)list[index]=slot; }
        private static List<T> Copy<T>(List<T> value) where T : class => value == null ? new List<T>() : JsonUtility.FromJson<BindingList<T>>(JsonUtility.ToJson(new BindingList<T> { Items=value })).Items ?? new List<T>();
        private static List<FaderButtonFunction> Copy(List<FaderButtonFunction> value) => value == null ? new List<FaderButtonFunction>() : new List<FaderButtonFunction>(value);
        [Serializable] private sealed class BindingList<T> where T : class { public List<T> Items = new(); }
    }
}
