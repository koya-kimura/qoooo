using System;
using System.Collections.Generic;
using qoooo.Timing;
using UnityEngine;

namespace qoooo.Parameters
{
    public enum SequenceDivision { Quarter, Eighth, Sixteenth }
    [Serializable]
    public sealed class SequenceParameter : IMidiBindableParameter, ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private List<bool> _defaultSteps = new();
        [SerializeField] private SequenceDivision _division = SequenceDivision.Quarter;
        [NonSerialized] private bool[] _steps;
        [NonSerialized] private int _activeStep = -1;
        public SequenceParameter() { }
        public SequenceParameter(string id,string displayName,IEnumerable<bool> steps,SequenceDivision division=SequenceDivision.Quarter) { _id=id;_displayName=displayName;_defaultSteps=steps==null?new List<bool>():new List<bool>(steps);_division=division; }
        public string Id=>_id; public string DisplayName=>string.IsNullOrWhiteSpace(_displayName)?_id:_displayName; public MidiParameterKind Kind=>MidiParameterKind.Sequence;
        public IReadOnlyList<bool> Steps { get { EnsureInitialized(); return _steps; } }
        public int StepCount { get { EnsureInitialized(); return _steps.Length; } }
        public SequenceDivision Division=>_division; public int ActiveStep=>_activeStep;
        public bool IsActive => _activeStep >= 0 && _activeStep < StepCount && Steps[_activeStep];
        public event Action Changed; public event Action<int> ActiveStepChanged;
        public bool TrySetStep(int index,bool value) { EnsureInitialized(); if(index<0||index>=_steps.Length||_steps[index]==value) return false; _steps[index]=value;Changed?.Invoke();return true; }
        public bool ToggleStep(int index) => index>=0&&index<StepCount&&TrySetStep(index,!Steps[index]);
        public bool TrySetDivision(SequenceDivision division) { if(!Enum.IsDefined(typeof(SequenceDivision),division)) division=SequenceDivision.Quarter; if(_division==division)return false;_division=division;Changed?.Invoke();return true; }
        public void UpdateBeat(double beat) { EnsureInitialized(); var factor=_division==SequenceDivision.Quarter?1:_division==SequenceDivision.Eighth?2:4; var next=_steps.Length==0?-1:PositiveModulo((long)Math.Floor(beat*factor),_steps.Length); if(next==_activeStep)return;_activeStep=next;ActiveStepChanged?.Invoke(next); }
        public void InitializeFromDefault() { _defaultSteps??=new List<bool>();_steps=_defaultSteps.ToArray();_activeStep=-1; if(!Enum.IsDefined(typeof(SequenceDivision),_division))_division=SequenceDivision.Quarter; }
        public void ResetToDefault() { InitializeFromDefault(); Changed?.Invoke(); }
        public void OnBeforeSerialize() { } public void OnAfterDeserialize() { _steps=null;_activeStep=-1; }
        private void EnsureInitialized(){if(_steps==null)InitializeFromDefault();}
        private static int PositiveModulo(long value,int count)=>(int)((value%count+count)%count);
    }
}
