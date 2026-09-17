using System;
using System.Collections.Generic;

namespace qoooo.Midi
{
    [Serializable]
    public sealed class ApcMidiMappingData
    {
        public int SchemaVersion = 1;
        public string ProfileId = "trip26";
        public bool HasSavedData;
        public List<ToggleMidiBinding> Toggles = new();
        public List<RadioMidiBinding> Radios = new();
        public List<OneshotMidiBinding> Oneshots = new();
        public List<MomentaryMidiBinding> Momentaries = new();
        public List<StateMidiBinding> States = new();
        public List<SequenceMidiBinding> Sequences = new();
        public List<FloatMidiBinding> Floats = new();
        public List<FaderButtonFunction> FaderButtonFunctions = new();
        public int FaderRandomDivision;
    }

    [Serializable]
    public sealed class MidiParameterStateData
    {
        public int SchemaVersion = 1;
        public string ProfileId = "trip26";
        public bool HasSavedData;
        public List<MidiParameterValueData> Values = new();
    }

    [Serializable]
    public sealed class MidiParameterValueData
    {
        public string Id;
        public int Kind;
        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public List<bool> Steps = new();
        public int Division;
    }
}
