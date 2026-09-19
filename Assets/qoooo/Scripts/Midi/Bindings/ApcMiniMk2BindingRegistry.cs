using System.Collections.Generic;
using qoooo.Midi.Apc;

namespace qoooo.Midi.Bindings
{
    public sealed class ApcMiniMk2BindingRegistry
    {
        public IReadOnlyDictionary<int, ApcMiniMk2RegisteredCell> Cells => _cells;
        public IReadOnlyDictionary<string, ApcMiniMk2Binding> Bindings => _bindings;
        public IReadOnlyList<string> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        private readonly Dictionary<int, ApcMiniMk2RegisteredCell> _cells = new();
        private readonly Dictionary<string, ApcMiniMk2Binding> _bindings = new();
        private readonly List<string> _errors = new();

        private ApcMiniMk2BindingRegistry(IReadOnlyList<ApcMiniMk2Binding> bindings)
        {
            Build(bindings);
        }

        public static ApcMiniMk2BindingRegistry Create(IReadOnlyList<ApcMiniMk2Binding> bindings)
            => new(bindings);

        public bool TryGetCell(int cellKey, out ApcMiniMk2RegisteredCell cell)
            => _cells.TryGetValue(cellKey, out cell);

        public bool TryGetBinding(string key, out ApcMiniMk2Binding binding)
            => _bindings.TryGetValue(key, out binding);

        private void Build(IReadOnlyList<ApcMiniMk2Binding> bindings)
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
            {
                if (binding == null)
                {
                    _errors.Add("Mapping contains a null binding.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.Key))
                {
                    _errors.Add("Binding key must not be empty.");
                    continue;
                }

                if (!_bindings.TryAdd(binding.Key, binding))
                {
                    _errors.Add($"Binding key '{binding.Key}' is duplicated.");
                    continue;
                }

                if (binding.Targets == null || binding.Targets.Count == 0)
                {
                    _errors.Add($"Binding '{binding.Key}' must contain at least one target.");
                    continue;
                }

                ValidateBinding(binding);

                for (var targetIndex = 0; targetIndex < binding.Targets.Count; targetIndex++)
                {
                    var target = binding.Targets[targetIndex];
                    if (!ApcMiniMk2Layout.IsValidCell(target.Page, target.Row, target.Col))
                    {
                        _errors.Add($"Binding '{binding.Key}' has an invalid cell ({target.Page}, {target.Row}, {target.Col}).");
                        continue;
                    }

                    var key = ApcMiniMk2Layout.GetCellKey(target.Page, target.Row, target.Col);
                    if (!_cells.TryAdd(key, new ApcMiniMk2RegisteredCell(binding.Key, binding.Type, targetIndex)))
                    {
                        _errors.Add($"Cell ({target.Page}, {target.Row}, {target.Col}) is assigned more than once.");
                    }
                }
            }

            ValidateRandomBindings();
        }

        private void ValidateBinding(ApcMiniMk2Binding binding)
        {
            switch (binding.Type)
            {
                case ApcMiniMk2BindingType.Radio:
                    if (binding.DefaultInt < 0 || binding.DefaultInt >= binding.Targets.Count)
                        _errors.Add($"Radio '{binding.Key}' defaultInt must be in range [0, {binding.Targets.Count - 1}].");
                    break;
                case ApcMiniMk2BindingType.Sequence:
                    if (binding.DefaultSteps == null || binding.DefaultSteps.Count != binding.Targets.Count)
                        _errors.Add($"Sequence '{binding.Key}' defaultSteps count must match targets count.");
                    break;
            }
        }

        private void ValidateRandomBindings()
        {
            var assignedRadioKeys = new HashSet<string>();
            foreach (var binding in _bindings.Values)
            {
                if (binding.Type != ApcMiniMk2BindingType.Random) continue;

                if (string.IsNullOrWhiteSpace(binding.RadioKey)
                    || !_bindings.TryGetValue(binding.RadioKey, out var radio)
                    || radio.Type != ApcMiniMk2BindingType.Radio)
                {
                    _errors.Add($"Random '{binding.Key}' must reference an existing Radio binding.");
                    continue;
                }

                if (!assignedRadioKeys.Add(binding.RadioKey))
                    _errors.Add($"Radio '{binding.RadioKey}' is already controlled by another Random binding.");
            }
        }
    }
}
