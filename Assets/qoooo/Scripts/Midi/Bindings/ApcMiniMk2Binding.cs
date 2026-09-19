using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Midi.Bindings
{
    public enum ApcMiniMk2BindingType { Toggle, Radio, Oneshot, Momentary, State, Sequence, Random }
    public enum FaderButtonFunction { Mute, Random }
    public enum FaderButtonMode { Normal, Mute, Random }

    [Serializable]
    public struct ApcMiniMk2CellPosition
    {
        [Range(0, 7)] public int Page;
        [Range(0, 7)] public int Row;
        [Range(0, 7)] public int Col;
    }

    [Serializable]
    public class ApcMiniMk2Binding
    {
        [SerializeField] private string _key;
        [SerializeField] private ApcMiniMk2BindingType _type;
        [SerializeField] private List<ApcMiniMk2CellPosition> _targets = new();
        [SerializeField] private bool _defaultBool;
        [SerializeField] private int _defaultInt;
        [SerializeField] private int _cycleLength = 1;
        [SerializeField] private List<bool> _defaultSteps = new();
        [SerializeField] private string _radioKey;

        public ApcMiniMk2Binding() { }

        public ApcMiniMk2Binding(
            string key,
            ApcMiniMk2BindingType type,
            IEnumerable<ApcMiniMk2CellPosition> targets,
            bool defaultBool = false,
            int defaultInt = 0,
            int cycleLength = 1,
            IEnumerable<bool> defaultSteps = null,
            string radioKey = null)
        {
            _key = key;
            _type = type;
            _targets = targets == null ? new List<ApcMiniMk2CellPosition>() : new List<ApcMiniMk2CellPosition>(targets);
            _defaultBool = defaultBool;
            _defaultInt = defaultInt;
            _cycleLength = cycleLength;
            _defaultSteps = defaultSteps == null ? new List<bool>() : new List<bool>(defaultSteps);
            _radioKey = radioKey;
        }

        public string Key => _key;
        public ApcMiniMk2BindingType Type => _type;
        public IReadOnlyList<ApcMiniMk2CellPosition> Targets => _targets;
        public bool DefaultBool => _defaultBool;
        public int DefaultInt => _defaultInt;
        public int CycleLength => _cycleLength;
        public IReadOnlyList<bool> DefaultSteps => _defaultSteps;
        public string RadioKey => _radioKey;
    }

    public readonly struct ApcMiniMk2RegisteredCell
    {
        public readonly string Key;
        public readonly ApcMiniMk2BindingType Type;
        public readonly int TargetIndex;

        public ApcMiniMk2RegisteredCell(string key, ApcMiniMk2BindingType type, int targetIndex)
        {
            Key = key;
            Type = type;
            TargetIndex = targetIndex;
        }
    }
}
