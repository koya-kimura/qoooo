using System;
using System.Collections.Generic;
using qoooo.Midi.Apc;
using qoooo.Parameters.Model;
using UnityEngine;

namespace qoooo.Midi.Bindings
{
    [Serializable]
    // Serializable APC address shared by typed Parameter binding records.
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
    [Serializable]
    public sealed class FloatMidiBinding
    {
        public string ParameterId;
        public bool Assigned;
        [Range(0,8)] public int FaderIndex;
        public bool HasOutputRange;
        public float OutputMin;
        public float OutputMax;
    }

    public static class FaderRangeMapping
    {
        public static void EnsureRange(FloatMidiBinding binding, FloatParameter parameter)
        {
            if (binding == null || parameter == null || binding.HasOutputRange) return;
            binding.OutputMin = parameter.Min;
            binding.OutputMax = parameter.Max;
            binding.HasOutputRange = true;
        }

        public static float Map(FloatMidiBinding binding, FloatParameter parameter, float midi01)
        {
            EnsureRange(binding, parameter);
            var mapped = Mathf.Lerp(binding.OutputMin, binding.OutputMax, Mathf.Clamp01(midi01));
            return Mathf.Clamp(mapped, parameter.Min, parameter.Max);
        }

        public static float Inverse(FloatMidiBinding binding, FloatParameter parameter, float value)
        {
            EnsureRange(binding, parameter);
            var range = binding.OutputMax - binding.OutputMin;
            if (Mathf.Approximately(range, 0f)) return 0f;
            return Mathf.Clamp01((value - binding.OutputMin) / range);
        }
    }
}
