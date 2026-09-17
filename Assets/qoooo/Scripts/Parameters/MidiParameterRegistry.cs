using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace qoooo.Parameters
{
    public sealed class MidiParameterRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, IMidiBindableParameter> _byId = new(StringComparer.Ordinal);
        private readonly List<IMidiBindableParameter> _parameters = new();
        public IReadOnlyList<IMidiBindableParameter> Parameters => _parameters;
        public int Revision { get; private set; }

        public void Initialize(IEnumerable<IMidiParameterContainer> providers)
        {
            _byId.Clear();
            _parameters.Clear();
            var duplicates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var provider in providers ?? Array.Empty<IMidiParameterContainer>())
            {
                if (provider == null) continue;
                foreach (var parameter in provider.MidiParameters ?? Array.Empty<IMidiBindableParameter>())
                {
                    if (parameter == null || string.IsNullOrWhiteSpace(parameter.Id))
                    {
                        Debug.LogError("[MIDI] Parameter has no ID.", provider as Object);
                        continue;
                    }

                    if (!IsValid(parameter))
                    {
                        Debug.LogError($"[MIDI] Parameter '{parameter.Id}' has invalid configuration.",
                            provider as Object);
                        continue;
                    }

                    if (!_byId.TryAdd(parameter.Id, parameter)) duplicates.Add(parameter.Id);
                }
            }

            foreach (var id in duplicates)
            {
                _byId.Remove(id);
                Debug.LogError($"[MIDI] Duplicate parameter ID '{id}'; both are excluded.", this);
            }

            foreach (var item in _byId.Values)
            {
                item.InitializeFromDefault();
                _parameters.Add(item);
            }

            _parameters.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            Revision++;
        }

        public bool TryGet(string id, out IMidiBindableParameter parameter)
        {
            return _byId.TryGetValue(id ?? string.Empty, out parameter);
        }

        private static bool IsValid(IMidiBindableParameter p)
        {
            return p switch { RadioParameter r => r.IsValid, SequenceParameter s => s.StepCount > 0, _ => true };
        }
    }
}