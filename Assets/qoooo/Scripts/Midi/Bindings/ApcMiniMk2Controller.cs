using System;
using System.Collections.Generic;
using qoooo.Foundation.Utilities;
using qoooo.Midi.Apc;

namespace qoooo.Midi.Bindings
{
    public sealed class ApcMiniMk2Controller : IApcMiniMk2Controller
    {
        private readonly ApcMiniMk2BindingRegistry _registry;
        private readonly Dictionary<string, bool> _booleanValues = new();
        private readonly Dictionary<string, int> _integerValues = new();
        private readonly Dictionary<string, bool[]> _sequenceSteps = new();
        private readonly float[] _physicalFaderValues = new float[ApcMiniMk2Constants.FaderCount];
        private readonly float[] _randomFaderValues = new float[ApcMiniMk2Constants.FaderCount];
        private readonly FaderButtonMode[] _faderButtonModes = new FaderButtonMode[ApcMiniMk2Constants.FaderButtonCount];
        private readonly FaderButtonFunction[] _faderButtonFunctions = new FaderButtonFunction[ApcMiniMk2Constants.FaderButtonCount];
        private readonly HashSet<string> _momentaryTriggeredThisFrame = new();
        private readonly HashSet<string> _momentaryResetNextFrame = new();

        private int _currentPage;
        private double _currentBeat;
        private long _previousBeatIndex = long.MinValue;

        public ApcMiniMk2Controller(ApcMiniMk2BindingRegistry registry, IReadOnlyList<FaderButtonFunction> faderButtonFunctions)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            InitializeValues();
            InitializeFaderButtonFunctions(faderButtonFunctions);
        }

        public void ProcessNoteOn(int noteNumber, float velocity)
        {
            if (velocity <= 0f)
            {
                ProcessNoteOff(noteNumber);
                return;
            }

            if (ApcMiniMk2Constants.IsSceneLaunchNote(noteNumber))
            {
                _currentPage = noteNumber - ApcMiniMk2Constants.SceneLaunchNoteFirst;
                return;
            }

            if (ApcMiniMk2Constants.IsGridNote(noteNumber))
            {
                ProcessGridNoteOn(noteNumber);
                return;
            }

            if (ApcMiniMk2Constants.TryGetFaderButtonIndex(noteNumber, out var faderIndex))
            {
                _faderButtonModes[faderIndex] = _faderButtonModes[faderIndex] == FaderButtonMode.Normal
                    ? (_faderButtonFunctions[faderIndex] == FaderButtonFunction.Mute ? FaderButtonMode.Mute : FaderButtonMode.Random)
                    : FaderButtonMode.Normal;

                if (_faderButtonModes[faderIndex] == FaderButtonMode.Random)
                    _randomFaderValues[faderIndex] = Random01(_previousBeatIndex + 1d, faderIndex + 1000d) < 0.5f ? 0f : 1f;
            }
        }

        public void ProcessNoteOff(int noteNumber)
        {
            if (!ApcMiniMk2Constants.IsGridNote(noteNumber)
                || !TryGetRegisteredCell(noteNumber, out var cell)
                || cell.Type != ApcMiniMk2BindingType.Oneshot) return;

            _booleanValues[cell.Key] = false;
        }

        public void ProcessControlChange(int controlNumber, float value)
        {
            if (!ApcMiniMk2Constants.TryGetFaderIndex(controlNumber, out var index)) return;
            _physicalFaderValues[index] = Clamp01(value);
        }

        public void UpdateState(double beat)
        {
            _currentBeat = beat;
            var beatIndex = FloorToLong(beat);
            if (beatIndex != _previousBeatIndex)
            {
                _previousBeatIndex = beatIndex;
                UpdateRandomBindings(beatIndex);
                UpdateRandomFaders(beatIndex);
            }

            foreach (var key in _momentaryResetNextFrame) _booleanValues[key] = false;
            _momentaryResetNextFrame.Clear();
            foreach (var key in _momentaryTriggeredThisFrame) _momentaryResetNextFrame.Add(key);
            _momentaryTriggeredThisFrame.Clear();
        }

        public float FaderValue(int index)
        {
            if (index < 0 || index >= ApcMiniMk2Constants.FaderCount) return 0f;
            return _faderButtonModes[index] switch
            {
                FaderButtonMode.Mute => 0f,
                FaderButtonMode.Random => _randomFaderValues[index],
                _ => _physicalFaderValues[index]
            };
        }

        public bool BooleanValue(string key) => key != null && _booleanValues.TryGetValue(key, out var value) && value;
        public int RadioValue(string key) => IntegerValue(key, ApcMiniMk2BindingType.Radio);
        public int StateValue(string key) => IntegerValue(key, ApcMiniMk2BindingType.State);

        public bool SequenceActive(string key)
        {
            if (key == null || !_sequenceSteps.TryGetValue(key, out var steps) || steps.Length == 0) return false;
            var index = PositiveModulo(FloorToLong(_currentBeat), steps.Length);
            return steps[index];
        }

        private void InitializeValues()
        {
            foreach (var binding in _registry.Bindings.Values)
            {
                switch (binding.Type)
                {
                    case ApcMiniMk2BindingType.Radio:
                        _integerValues[binding.Key] = binding.DefaultInt;
                        break;
                    case ApcMiniMk2BindingType.State:
                        _integerValues[binding.Key] = NormalizeStateDefault(binding);
                        break;
                    case ApcMiniMk2BindingType.Sequence:
                        _sequenceSteps[binding.Key] = CopySteps(binding.DefaultSteps);
                        break;
                    default:
                        _booleanValues[binding.Key] = binding.DefaultBool;
                        break;
                }
            }
        }

        private void InitializeFaderButtonFunctions(IReadOnlyList<FaderButtonFunction> configured)
        {
            var defaults = new[]
            {
                FaderButtonFunction.Mute, FaderButtonFunction.Random, FaderButtonFunction.Mute,
                FaderButtonFunction.Random, FaderButtonFunction.Mute, FaderButtonFunction.Random,
                FaderButtonFunction.Mute, FaderButtonFunction.Random, FaderButtonFunction.Mute
            };

            for (var i = 0; i < _faderButtonFunctions.Length; i++)
                _faderButtonFunctions[i] = configured != null && configured.Count == _faderButtonFunctions.Length
                    ? configured[i] : defaults[i];
        }

        private void ProcessGridNoteOn(int note)
        {
            if (!TryGetRegisteredCell(note, out var cell)) return;

            switch (cell.Type)
            {
                case ApcMiniMk2BindingType.Toggle:
                case ApcMiniMk2BindingType.Random:
                    _booleanValues[cell.Key] = !BooleanValue(cell.Key);
                    break;
                case ApcMiniMk2BindingType.Radio:
                    _integerValues[cell.Key] = cell.TargetIndex;
                    break;
                case ApcMiniMk2BindingType.Oneshot:
                    _booleanValues[cell.Key] = true;
                    break;
                case ApcMiniMk2BindingType.Momentary:
                    _booleanValues[cell.Key] = true;
                    _momentaryTriggeredThisFrame.Add(cell.Key);
                    break;
                case ApcMiniMk2BindingType.State:
                    var cycleLength = GetStateCycleLength(cell.Key);
                    _integerValues[cell.Key] = (StateValue(cell.Key) + 1) % cycleLength;
                    break;
                case ApcMiniMk2BindingType.Sequence:
                    if (_sequenceSteps.TryGetValue(cell.Key, out var steps) && cell.TargetIndex < steps.Length)
                        steps[cell.TargetIndex] = !steps[cell.TargetIndex];
                    break;
            }
        }

        private bool TryGetRegisteredCell(int note, out ApcMiniMk2RegisteredCell cell)
        {
            if (!ApcMiniMk2Layout.TryGetGridPosition(note, out var row, out var col))
            {
                cell = default;
                return false;
            }
            return _registry.TryGetCell(ApcMiniMk2Layout.GetCellKey(_currentPage, row, col), out cell);
        }

        private int IntegerValue(string key, ApcMiniMk2BindingType expectedType)
            => key != null && _registry.TryGetBinding(key, out var binding) && binding.Type == expectedType
                && _integerValues.TryGetValue(key, out var value) ? value : 0;

        private int GetStateCycleLength(string key)
            => _registry.TryGetBinding(key, out var binding) ? Math.Max(1, binding.CycleLength) : 1;

        private void UpdateRandomBindings(long beatIndex)
        {
            foreach (var binding in _registry.Bindings.Values)
            {
                if (binding.Type != ApcMiniMk2BindingType.Random || !BooleanValue(binding.Key)
                    || !_registry.TryGetBinding(binding.RadioKey, out var radio) || radio.Targets.Count == 0) continue;

                var seed = binding.Targets[0];
                var cellKey = ApcMiniMk2Layout.GetCellKey(seed.Page, seed.Row, seed.Col);
                var random = Random01(beatIndex + 1d, cellKey);
                var nextIndex = Math.Min((int)Math.Floor(random * radio.Targets.Count), radio.Targets.Count - 1);
                _integerValues[binding.RadioKey] = nextIndex;
            }
        }

        private void UpdateRandomFaders(long beatIndex)
        {
            for (var index = 0; index < _faderButtonModes.Length; index++)
            {
                if (_faderButtonModes[index] != FaderButtonMode.Random) continue;
                _randomFaderValues[index] = Random01(beatIndex + 1d, index + 1000d) < 0.5f ? 0f : 1f;
            }
        }

        private static bool[] CopySteps(IReadOnlyList<bool> source)
        {
            if (source == null) return Array.Empty<bool>();
            var copy = new bool[source.Count];
            for (var i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        private static int NormalizeStateDefault(ApcMiniMk2Binding binding)
        {
            var length = Math.Max(1, binding.CycleLength);
            return binding.DefaultInt >= 0 && binding.DefaultInt < length ? binding.DefaultInt : 0;
        }

        private static long FloorToLong(double value) => value <= long.MinValue ? long.MinValue : value >= long.MaxValue ? long.MaxValue : (long)Math.Floor(value);
        private static int PositiveModulo(long value, int count) => (int)((value % count + count) % count);
        private static float Random01(double x, double y) => Pcg.Pcg01(new UnityEngine.Vector2((float)x, (float)y)).x;
        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
