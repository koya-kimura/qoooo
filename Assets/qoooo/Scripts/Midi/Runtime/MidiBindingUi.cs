using System;
using System.Collections.Generic;
using qoooo.Midi.Apc;
using qoooo.Midi.Bindings;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using RosettaUI;

namespace qoooo.Midi.Runtime
{
    /// <summary>型ごとの必要なslotだけを表示するMIDI Mapping UI。</summary>
    // Edits APC assignments around registry Parameters; it owns neither values nor mapping persistence.
    public sealed class MidiBindingUi
    {
        private const float BindingsListMaxHeight = 640f;
        private readonly ApcMiniMk2ParameterController _controller;
        private readonly MidiParameterRegistry _registry;
        public MidiBindingUi(ApcMiniMk2ParameterController controller, MidiParameterRegistry registry) { _controller=controller; _registry=registry; }
        public Element CreateBindingsElement()
        {
            return UI.DynamicElementOnStatusChanged(BindingStatus, _ => UI.Column(
                UI.Label(() => _controller.IsListening ? $"Listening: {_controller.ListeningParameterId}" : "Listen: idle"),
                UI.Button("Cancel Listen", _controller.CancelListen),
                UI.ScrollViewVertical(BindingsListMaxHeight,
                    UI.Fold("Parameter Bindings", CreateBindings()).Open())
            ));
        }

        private int BindingStatus()
        {
            unchecked
            {
                var hash = _registry.Revision;
                foreach (var parameter in _registry.Parameters)
                {
                    if (parameter is not RadioParameter radio) continue;
                    hash = hash * 31 + radio.Options.Count;
                    foreach (var option in radio.Options)
                        hash = hash * 31 + (option?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        public Element CreateFaderButtonFunctionsElement()
        {
            var rows=new List<Element>();
            var options=new[] { "Mute", "Random" };
            for(var index=0;index<ApcMiniMk2Constants.FaderButtonCount;index++)
            {
                var captured=index;
                var label=index<8?$"Track {index+1}" : "Shift (Master)";
                rows.Add(UI.Dropdown(label,
                    () => (int)_controller.GetFaderButtonFunction(captured),
                    value => _controller.SetFaderButtonFunction(captured,(FaderButtonFunction)value), options));
            }
            rows.Add(UI.Dropdown("Random Division", () => (int)_controller.FaderRandomDivision,
                value => _controller.SetFaderRandomDivision((SequenceDivision)value),new[]{"Quarter","Eighth","Sixteenth"}));
            return UI.Column(rows);
        }
        private Element CreateBindings()
        {
            var rows = new List<Element>();
            foreach (var parameter in _registry.Parameters)
            {
                if (!IsTextureEffectParameter(parameter)) rows.Add(CreateBinding(parameter));
            }

            var textureEffects = CreateTextureEffectBindings();
            if (textureEffects != null) rows.Insert(0, textureEffects);
            return UI.Column(rows);
        }

        private Element CreateTextureEffectBindings()
        {
            var targets = new List<Element>();
            AddTextureEffectTarget(targets, "Model", "effect.model.");
            AddTextureEffectTarget(targets, "Background", "effect.background.");
            AddTextureEffectTarget(targets, "Text (Legacy)", "effect.text.");
            return targets.Count == 0
                ? null
                : UI.Fold("Texture Effects", UI.Column(targets)).Open();
        }

        private void AddTextureEffectTarget(List<Element> targets, string label, string idPrefix)
        {
            var parameters = new List<IMidiBindableParameter>();
            foreach (var parameter in _registry.Parameters)
                if (parameter.Id.StartsWith(idPrefix, StringComparison.Ordinal)) parameters.Add(parameter);
            if (parameters.Count == 0) return;

            var rows = new List<Element>();
            AddTextureEffectStage(rows, parameters, idPrefix + "uv");
            AddTextureEffectStage(rows, parameters, idPrefix + "sampling");
            AddTextureEffectStage(rows, parameters, idPrefix + "color");
            AddTextureEffectStage(rows, parameters, idPrefix + "alpha");

            var shared = parameters.FindAll(parameter =>
                parameter.Id != idPrefix + "uv"
                && parameter.Id != idPrefix + "sampling"
                && parameter.Id != idPrefix + "color"
                && parameter.Id != idPrefix + "alpha");
            if (shared.Count > 0)
                rows.Add(UI.Fold("Shared Parameters", UI.Column(shared.ConvertAll(CreateBinding))));

            targets.Add(UI.Fold(label, UI.Column(rows)).Open());
        }

        private void AddTextureEffectStage(
            List<Element> rows,
            List<IMidiBindableParameter> parameters,
            string parameterId)
        {
            var parameter = parameters.Find(candidate => candidate.Id == parameterId);
            if (parameter != null) rows.Add(CreateBinding(parameter));
        }

        private static bool IsTextureEffectParameter(IMidiBindableParameter parameter)
        {
            return parameter.Id.StartsWith("effect.model.", StringComparison.Ordinal)
                     || parameter.Id.StartsWith("effect.background.", StringComparison.Ordinal)
                     || parameter.Id.StartsWith("effect.text.", StringComparison.Ordinal);
        }

        private Element CreateBinding(IMidiBindableParameter parameter)
        {
            return parameter switch
            {
                RadioParameter radio => UI.Fold(parameter.DisplayName,
                    CreateSlots(parameter, radio.Options.Count, index => radio.Options[index])).Open(),
                SequenceParameter sequence => UI.Fold(parameter.DisplayName,
                    CreateSlots(parameter, sequence.StepCount, index => $"Step {index + 1}")).Open(),
                FloatParameter value => CreateFader(value),
                BoolParameter or OneshotParameter or MomentaryParameter or StateParameter =>
                    CreateSimpleButton(parameter),
                _ => UI.Label(parameter.DisplayName)
            };
        }
        private Element CreateSimpleButton(IMidiBindableParameter parameter)
        {
            return UI.Row(
                UI.Label(parameter.DisplayName),
                UI.Label(() => SlotLabel(parameter.Id, parameter.Kind, 0)),
                UI.Button("Listen Pad", () => _controller.BeginButtonListen(parameter.Id, parameter.Kind, 0)),
                UI.Button("Clear", () => _controller.ClearButtonBinding(parameter.Id, parameter.Kind, 0)));
        }
        private Element CreateSlots(IMidiBindableParameter parameter, int count, Func<int,string> label)
        {
            var rows = new List<Element>();
            for(var index=0;index<count;index++)
            {
                var captured=index;
                rows.Add(UI.Row(
                    UI.Label(label(captured)), UI.Label(() => SlotLabel(parameter.Id, parameter.Kind, captured)),
                    UI.Button("Listen Pad", () => _controller.BeginButtonListen(parameter.Id, parameter.Kind, captured)),
                    UI.Button("Clear", () => _controller.ClearButtonBinding(parameter.Id, parameter.Kind, captured))));
            }
            return UI.Column(rows);
        }
        private Element CreateFader(FloatParameter parameter)
        {
            return UI.Column(
                UI.Row(
                    UI.Label(parameter.DisplayName),
                    UI.Label(() => _controller.TryGetFaderIndex(parameter.Id, out var index) ? $"Fader {index + 1}" : "Unassigned"),
                    UI.Button("Listen Fader", () => _controller.BeginFaderListen(parameter.Id)),
                    UI.Button("Clear", () => _controller.ClearFaderBinding(parameter.Id))),
                UI.Row(
                    UI.Field("Output Min", () => OutputMin(parameter),
                        value => SetOutputMin(parameter, value)),
                    UI.Field("Output Max", () => OutputMax(parameter),
                        value => SetOutputMax(parameter, value))));
        }

        private float OutputMin(FloatParameter parameter)
        {
            return _controller.TryGetFaderOutputRange(parameter.Id, out var outputMin, out _)
                ? outputMin
                : parameter.Min;
        }

        private float OutputMax(FloatParameter parameter)
        {
            return _controller.TryGetFaderOutputRange(parameter.Id, out _, out var outputMax)
                ? outputMax
                : parameter.Max;
        }

        private void SetOutputMin(FloatParameter parameter, float value)
        {
            _controller.SetFaderOutputRange(parameter.Id, value, OutputMax(parameter));
        }

        private void SetOutputMax(FloatParameter parameter, float value)
        {
            _controller.SetFaderOutputRange(parameter.Id, OutputMin(parameter), value);
        }
        private string SlotLabel(string parameterId, MidiParameterKind kind, int index)
        {
            return _controller.TryGetButtonSlot(parameterId, kind, index, out var slot) && slot.Assigned
                ? $"P{slot.Page + 1}  R{slot.Row + 1}  C{slot.Column + 1}"
                : "Unassigned";
        }
    }
}
