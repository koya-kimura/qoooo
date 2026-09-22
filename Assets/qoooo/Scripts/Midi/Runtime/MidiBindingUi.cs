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
            return UI.Column(
                UI.Label(() => _controller.IsListening ? $"Listening: {_controller.ListeningParameterId}" : "Listen: idle"),
                UI.Button("Cancel Listen", _controller.CancelListen),
                UI.ScrollViewVertical(BindingsListMaxHeight,
                    UI.Fold("Parameter Bindings", CreateBindings()).Open())
            );
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
                var captured = parameter;
                switch (captured)
                {
                    case RadioParameter radio: rows.Add(UI.Fold(captured.DisplayName, CreateSlots(captured, radio.Options.Count, index => radio.Options[index])).Open()); break;
                    case SequenceParameter sequence: rows.Add(UI.Fold(captured.DisplayName, CreateSlots(captured, sequence.StepCount, index => $"Step {index + 1}")).Open()); break;
                    case FloatParameter: rows.Add(CreateFader(captured)); break;
                    case BoolParameter:
                    case OneshotParameter:
                    case MomentaryParameter:
                    case StateParameter: rows.Add(CreateSimpleButton(captured)); break;
                }
            }
            return UI.Column(rows);
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
        private Element CreateFader(IMidiBindableParameter parameter)
        {
            return UI.Row(
                UI.Label(parameter.DisplayName),
                UI.Label(() => _controller.TryGetFaderIndex(parameter.Id, out var index) ? $"Fader {index + 1}" : "Unassigned"),
                UI.Button("Listen Fader", () => _controller.BeginFaderListen(parameter.Id)),
                UI.Button("Clear", () => _controller.ClearFaderBinding(parameter.Id)));
        }
        private string SlotLabel(string parameterId, MidiParameterKind kind, int index)
        {
            return _controller.TryGetButtonSlot(parameterId, kind, index, out var slot) && slot.Assigned
                ? $"P{slot.Page + 1}  R{slot.Row + 1}  C{slot.Column + 1}"
                : "Unassigned";
        }
    }
}
