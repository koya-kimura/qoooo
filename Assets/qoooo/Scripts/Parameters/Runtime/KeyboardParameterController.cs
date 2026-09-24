using System;
using System.Collections.Generic;
using PrefsGUI;
using qoooo.Parameters.Model;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace qoooo.Parameters.Runtime
{
    [DefaultExecutionOrder(-80)]
    // Adapts persisted keyboard bindings into owner-held Parameters resolved from the registry.
    // It never calls visual consumers directly or owns duplicate Parameter state.
    public sealed class KeyboardParameterController : MonoBehaviour
    {
        private const string PrefsKey = "qoooo.keyboard.trip26.bindings.v1";
        private const float BindingsListMaxHeight = 640f;
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private MidiParameterRegistry _registry;
        [SerializeField] private List<KeyboardParameterBinding> _bindings = new();

        private readonly PrefsAny<KeyboardBindingData> _prefs = new(PrefsKey);
        private KeyboardBindingData _recordBeforePrepare;
        private KeyboardParameterBinding _listeningBinding;
        private int _uiRevision;
        public bool IsDirty { get; private set; }

        private void Awake()
        {
            var saved = _prefs.Get();
            _bindings = IsCompatible(saved)
                ? Copy(saved.Bindings)
                : CreateDefaultBindings();
            NormalizeBindings();
        }

        public void Configure(UIDocument uiDocument, MidiParameterRegistry registry)
        {
            _uiDocument = uiDocument;
            _registry = registry;
            NormalizeAgainstRegistry();
            Changed(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _registry == null) return;

            if (_listeningBinding != null)
            {
                CaptureListeningKey(keyboard);
                return;
            }

            if (IsSystemModifierPressed(keyboard)) return;

            var textInputFocused = IsTextInputFocused();
            foreach (var binding in _bindings)
            {
                if (!IsUsable(binding) || !keyboard[binding.Key].wasPressedThisFrame) continue;
                if (binding.Shift != IsShiftPressed(keyboard)) continue;
                if (textInputFocused && !(binding.ParameterId == "ui.toggle-visibility" && IsUiHidden())) continue;
                Apply(binding);
            }
        }

        public Element CreateElement()
        {
            return UI.DynamicElementOnStatusChanged(
                BindingUiStatus,
                _ => CreateBindingsElement());
        }

        public void PrepareSave()
        {
            _recordBeforePrepare = Copy(_prefs.Get()) ?? new KeyboardBindingData();
            _prefs.Set(new KeyboardBindingData
            {
                HasSavedData = true,
                Bindings = Copy(_bindings)
            });
        }

        public void CommitSave()
        {
            _recordBeforePrepare = null;
            IsDirty = false;
            Debug.Log("[Keyboard] Bindings staged for Prefs.Save.", this);
        }

        public void AbortSave()
        {
            if (_recordBeforePrepare == null) return;
            _prefs.Set(_recordBeforePrepare);
            _recordBeforePrepare = null;
        }

        private Element CreateBindingsElement()
        {
            var rows = new List<Element>
            {
                UI.Label(() => IsDirty ? "Bindings: unsaved" : "Bindings: saved"),
                UI.Label(() => _listeningBinding == null
                    ? "Listen: idle"
                    : $"Listening: {_listeningBinding.ParameterId}"),
                UI.Row(
                    UI.Button("New Binding", AddBinding),
                    UI.Button("Cancel Listen", CancelListen))
            };

            var parameters = ValidParameters();
            var parameterNames = parameters.ConvertAll(ParameterLabel).ToArray();
            for (var index = 0; index < _bindings.Count; index++)
            {
                var binding = _bindings[index];
                rows.Add(UI.Fold(BindingLabel(binding),
                    UI.Dropdown("Parameter",
                        () => Math.Max(0, parameters.FindIndex(parameter => parameter.Id == binding.ParameterId)),
                        selected => SetParameter(binding, parameters, selected),
                        parameterNames),
                    UI.Row(
                        UI.Label(() => $"Key: {KeyLabel(binding)}"),
                        UI.Button("Listen Key", () => BeginListen(binding)),
                        UI.Button("Clear", () => RemoveBinding(binding))),
                    UI.Label(() => $"Action: {ActionLabel(binding)}"),
                    CreateValueControl(binding)).Open());
            }

            return UI.ScrollViewVertical(BindingsListMaxHeight, UI.Column(rows));
        }

        private Element CreateValueControl(KeyboardParameterBinding binding)
        {
            if (!_registry.TryGet(binding.ParameterId, out var parameter))
                return UI.Label("Missing parameter");

            return parameter switch
            {
                FloatParameter value => UI.Field("Value", () => binding.Value,
                    next => SetFloatValue(binding, value, next)),
                RadioParameter value => UI.Dropdown("Option",
                    () => Mathf.Clamp(binding.OptionIndex, 0, value.Options.Count - 1),
                    next => SetOptionIndex(binding, next), value.Options),
                SequenceParameter value => UI.Dropdown("Step",
                    () => Mathf.Clamp(binding.OptionIndex, 0, value.StepCount - 1),
                    next => SetOptionIndex(binding, next), StepNames(value.StepCount)),
                _ => UI.Space()
            };
        }

        private void AddBinding()
        {
            var parameters = ValidParameters();
            if (parameters.Count == 0) return;
            var binding = new KeyboardParameterBinding { ParameterId = parameters[0].Id };
            ConfigureForParameter(binding, parameters[0]);
            _bindings.Add(binding);
            Changed();
        }

        private void RemoveBinding(KeyboardParameterBinding binding)
        {
            if (ReferenceEquals(_listeningBinding, binding)) _listeningBinding = null;
            _bindings.Remove(binding);
            Changed();
        }

        private void SetParameter(
            KeyboardParameterBinding binding,
            IReadOnlyList<IMidiBindableParameter> parameters,
            int index)
        {
            if (index < 0 || index >= parameters.Count) return;
            binding.ParameterId = parameters[index].Id;
            ConfigureForParameter(binding, parameters[index]);
            Changed();
        }

        private void SetFloatValue(KeyboardParameterBinding binding, FloatParameter parameter, float value)
        {
            binding.Value = Mathf.Clamp(float.IsFinite(value) ? value : parameter.Min, parameter.Min, parameter.Max);
            Changed();
        }

        private void SetOptionIndex(KeyboardParameterBinding binding, int index)
        {
            binding.OptionIndex = Math.Max(0, index);
            Changed();
        }

        private void BeginListen(KeyboardParameterBinding binding)
        {
            _listeningBinding = binding;
            Changed(false);
        }

        private void CancelListen()
        {
            if (_listeningBinding == null) return;
            _listeningBinding = null;
            Changed(false);
        }

        private void CaptureListeningKey(Keyboard keyboard)
        {
            foreach (var keyControl in keyboard.allKeys)
            {
                if (!keyControl.wasPressedThisFrame) continue;
                var key = keyControl.keyCode;
                if (key == Key.Escape)
                {
                    CancelListen();
                    return;
                }
                if (IsReservedKey(key) || IsSystemModifierPressed(keyboard)) return;

                _bindings.RemoveAll(binding => !ReferenceEquals(binding, _listeningBinding)
                    && binding.Key == key
                    && binding.Shift == IsShiftPressed(keyboard));
                _listeningBinding.Key = key;
                _listeningBinding.Shift = IsShiftPressed(keyboard);
                _listeningBinding = null;
                Changed();
                return;
            }
        }

        private void Apply(KeyboardParameterBinding binding)
        {
            if (!_registry.TryGet(binding.ParameterId, out var parameter))
            {
                Debug.LogWarning($"[Keyboard] Binding target '{binding.ParameterId}' is unresolved.", this);
                return;
            }

            switch (parameter)
            {
                case BoolParameter value when binding.Action == KeyboardBindingAction.Toggle:
                    value.TrySetValue(!value.Value);
                    break;
                case FloatParameter value when binding.Action == KeyboardBindingAction.SetValue:
                    value.TrySetValue(binding.Value);
                    break;
                case RadioParameter value when binding.Action == KeyboardBindingAction.SetOption:
                    if (binding.OptionIndex >= 0 && binding.OptionIndex < value.Options.Count)
                        value.TrySetValue(binding.OptionIndex);
                    break;
                case StateParameter value when binding.Action == KeyboardBindingAction.NextState:
                    value.Next();
                    break;
                case SequenceParameter value when binding.Action == KeyboardBindingAction.ToggleStep:
                    if (binding.OptionIndex >= 0 && binding.OptionIndex < value.StepCount)
                        value.ToggleStep(binding.OptionIndex);
                    break;
                case OneshotParameter value when binding.Action == KeyboardBindingAction.Trigger:
                    value.Trigger();
                    break;
                case MomentaryParameter value when binding.Action == KeyboardBindingAction.Trigger:
                    value.Trigger();
                    break;
            }
        }

        private void NormalizeBindings()
        {
            _bindings ??= new List<KeyboardParameterBinding>();
            _bindings.RemoveAll(binding => binding == null || string.IsNullOrWhiteSpace(binding.ParameterId));
            var usedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in _bindings)
            {
                if (IsReservedKey(binding.Key)) binding.Key = Key.None;
                var key = $"{binding.Key}:{binding.Shift}";
                if (binding.Key != Key.None && !usedKeys.Add(key)) binding.Key = Key.None;
            }
        }

        private void NormalizeAgainstRegistry()
        {
            foreach (var binding in _bindings)
            {
                if (!_registry.TryGet(binding.ParameterId, out var parameter)) continue;
                binding.Action = ActionFor(parameter);
                switch (parameter)
                {
                    case FloatParameter value:
                        binding.Value = Mathf.Clamp(
                            float.IsFinite(binding.Value) ? binding.Value : value.Min,
                            value.Min,
                            value.Max);
                        break;
                    case RadioParameter value:
                        binding.OptionIndex = Mathf.Clamp(binding.OptionIndex, 0, value.Options.Count - 1);
                        break;
                    case SequenceParameter value:
                        binding.OptionIndex = Mathf.Clamp(binding.OptionIndex, 0, value.StepCount - 1);
                        break;
                }
            }
        }

        private List<IMidiBindableParameter> ValidParameters()
        {
            var result = new List<IMidiBindableParameter>();
            foreach (var parameter in _registry?.Parameters ?? Array.Empty<IMidiBindableParameter>())
                result.Add(parameter);
            return result;
        }

        private void ConfigureForParameter(KeyboardParameterBinding binding, IMidiBindableParameter parameter)
        {
            binding.Action = ActionFor(parameter);
            binding.OptionIndex = 0;
            if (parameter is FloatParameter value) binding.Value = value.Value;
        }

        private static KeyboardBindingAction ActionFor(IMidiBindableParameter parameter)
        {
            return parameter switch
            {
                BoolParameter => KeyboardBindingAction.Toggle,
                FloatParameter => KeyboardBindingAction.SetValue,
                RadioParameter => KeyboardBindingAction.SetOption,
                StateParameter => KeyboardBindingAction.NextState,
                SequenceParameter => KeyboardBindingAction.ToggleStep,
                OneshotParameter or MomentaryParameter => KeyboardBindingAction.Trigger,
                _ => KeyboardBindingAction.Trigger
            };
        }

        private int BindingUiStatus()
        {
            unchecked
            {
                var hash = _uiRevision * 397
                           ^ (_registry?.Revision ?? 0)
                           ^ (_listeningBinding?.GetHashCode() ?? 0);
                foreach (var parameter in _registry?.Parameters ?? Array.Empty<IMidiBindableParameter>())
                {
                    if (parameter is not RadioParameter radio) continue;
                    hash = hash * 31 + radio.Options.Count;
                    foreach (var option in radio.Options)
                        hash = hash * 31 + (option?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        private void Changed(bool dirty = true)
        {
            if (dirty) IsDirty = true;
            _uiRevision++;
        }

        private static string ParameterLabel(IMidiBindableParameter parameter)
            => $"{parameter.DisplayName} ({parameter.Id})";

        private static string BindingLabel(KeyboardParameterBinding binding)
            => string.IsNullOrWhiteSpace(binding.ParameterId) ? "Missing Binding" : binding.ParameterId;

        private static string ActionLabel(KeyboardParameterBinding binding)
            => binding.Action switch
            {
                KeyboardBindingAction.SetOption => $"Set Option {binding.OptionIndex + 1}",
                KeyboardBindingAction.ToggleStep => $"Toggle Step {binding.OptionIndex + 1}",
                KeyboardBindingAction.SetValue => $"Set Value {binding.Value:G4}",
                KeyboardBindingAction.NextState => "Next State",
                KeyboardBindingAction.Toggle => "Toggle",
                _ => "Trigger"
            };

        private static string KeyLabel(KeyboardParameterBinding binding)
        {
            if (binding.Key == Key.None) return "Unassigned";
            return binding.Shift ? $"Shift+{binding.Key}" : binding.Key.ToString();
        }

        private static string[] StepNames(int count)
        {
            var result = new string[Math.Max(0, count)];
            for (var index = 0; index < result.Length; index++) result[index] = $"Step {index + 1}";
            return result;
        }

        private static bool IsUsable(KeyboardParameterBinding binding)
            => binding != null && binding.Key != Key.None && !IsReservedKey(binding.Key);

        private static bool IsShiftPressed(Keyboard keyboard)
            => keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        private static bool IsSystemModifierPressed(Keyboard keyboard)
            => keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed
               || keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed
               || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;

        private static bool IsReservedKey(Key key)
        {
            return key is Key.None or Key.Escape
                or Key.LeftShift or Key.RightShift
                or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt
                or Key.LeftMeta or Key.RightMeta;
        }

        private static List<KeyboardParameterBinding> CreateDefaultBindings()
        {
            return new List<KeyboardParameterBinding>
            {
                new() { ParameterId = "ui.toggle-visibility", Key = Key.D, Action = KeyboardBindingAction.Trigger },
                new() { ParameterId = "rhythm.tap", Key = Key.Space, Action = KeyboardBindingAction.Trigger },
                new() { ParameterId = "rhythm.sync-beat", Key = Key.B, Action = KeyboardBindingAction.Trigger },
                new() { ParameterId = "rhythm.sync-bar", Key = Key.B, Shift = true, Action = KeyboardBindingAction.Trigger }
            };
        }

        private static bool IsCompatible(KeyboardBindingData value)
            => value != null && value.HasSavedData && value.SchemaVersion == 1 && value.ProfileId == "trip26";

        private static T Copy<T>(T value) where T : class
            => value == null ? null : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));

        private bool IsTextInputFocused()
        {
            var focused = _uiDocument?.rootVisualElement?.panel?.focusController?.focusedElement as VisualElement;
            for (var element = focused; element != null; element = element.parent)
                if (element is TextInputBaseField<string>) return true;
            return false;
        }

        private bool IsUiHidden()
        {
            return _uiDocument?.rootVisualElement != null && !_uiDocument.rootVisualElement.visible;
        }
    }
}
