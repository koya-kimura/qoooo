using System;
using System.Collections.Generic;
using qoooo.Midi.Bindings;

namespace qoooo.Midi.Profiles
{
    [Serializable]
    // 保存するAPC mapping snapshot。runtimeのpage、pickup、Listen stateは意図的に含めない。
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
    // 明示的なQoooo Saveのために保存するowner Parameter valueのcollection。
    public sealed class MidiParameterStateData
    {
        public int SchemaVersion = 1;
        public string ProfileId = "trip26";
        public bool HasSavedData;
        public List<MidiParameterValueData> Values = new();
    }

    [Serializable]
    // 1つのstable Parameter IDとkind固有のvalue fieldを持つserialization DTO。
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
