using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Midi
{
    [Serializable]
    public struct ApcButtonSlot
    {
        public bool Assigned;
        [Range(0,7)] public int Page;
        [Range(0,7)] public int Row;
        [Range(0,7)] public int Column;
        public readonly bool IsValid => Assigned && ApcMiniMk2Layout.IsValidCell(Page,Row,Column);
        public readonly int CellKey => ApcMiniMk2Layout.GetCellKey(Page,Row,Column);
    }
    [Serializable] public sealed class ToggleMidiBinding { public string ParameterId; public ApcButtonSlot Button; }
    [Serializable] public sealed class OneshotMidiBinding { public string ParameterId; public ApcButtonSlot Button; }
    [Serializable] public sealed class MomentaryMidiBinding { public string ParameterId; public ApcButtonSlot Button; }
    [Serializable] public sealed class StateMidiBinding { public string ParameterId; public ApcButtonSlot Button; }
    [Serializable] public sealed class RadioMidiBinding { public string ParameterId; public List<ApcButtonSlot> OptionButtons = new(); }
    [Serializable] public sealed class SequenceMidiBinding { public string ParameterId; public List<ApcButtonSlot> StepButtons = new(); }
    [Serializable] public sealed class FloatMidiBinding { public string ParameterId; public bool Assigned; [Range(0,8)] public int FaderIndex; }
}
