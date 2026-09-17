using System;
using System.Collections.Generic;
using qoooo.Parameters;
using RosettaUI;

namespace qoooo.Midi
{
    /// <summary>型ごとの必要なslotだけを表示するMIDI Mapping UI。</summary>
    public sealed class MidiBindingUi
    {
        private readonly ApcMiniMk2ParameterController _controller;
        private readonly MidiParameterRegistry _registry;
        public MidiBindingUi(ApcMiniMk2ParameterController controller, MidiParameterRegistry registry) { _controller=controller; _registry=registry; }
        public Element CreateElement()
        {
            return UI.Column(
                UI.Label(() => _controller.IsListening ? $"Listening: {_controller.ListeningParameterId}" : "Listen: idle"),
                UI.Button("Cancel Listen", _controller.CancelListen),
                UI.Fold("Parameter Bindings", CreateBindings()).Open(),
                UI.Fold("Fader Button Functions", CreateFaderFunctions()).Open()
            );
        }
        private Element CreateFaderFunctions()
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
                CreateDirectControl(parameter),
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
                    UI.Label(label(captured)), CreateSlotControl(parameter, captured), UI.Label(() => SlotLabel(parameter.Id, parameter.Kind, captured)),
                    UI.Button("Listen Pad", () => _controller.BeginButtonListen(parameter.Id, parameter.Kind, captured)),
                    UI.Button("Clear", () => _controller.ClearButtonBinding(parameter.Id, parameter.Kind, captured))));
            }
            return UI.Column(rows);
        }
        private Element CreateFader(IMidiBindableParameter parameter)
        {
            var value = parameter as FloatParameter;
            return UI.Row(
                UI.Label(parameter.DisplayName),
                UI.Slider("Value", () => value.Value, next => value.TrySetValue(next), value.Min, value.Max),
                UI.Label(() => _controller.TryGetFaderIndex(parameter.Id, out var index) ? $"Fader {index + 1}" : "Unassigned"),
                UI.Button("Listen Fader", () => _controller.BeginFaderListen(parameter.Id)),
                UI.Button("Clear", () => _controller.ClearFaderBinding(parameter.Id)));
        }
        private static Element CreateDirectControl(IMidiBindableParameter parameter)
        {
            return parameter switch
            {
                BoolParameter value => UI.Button(UI.Label(() => value.Value ? "ON" : "OFF"), () => value.TrySetValue(!value.Value)),
                OneshotParameter value => UI.Button("Fire", value.Trigger),
                MomentaryParameter value => UI.Button("Trigger", value.Trigger),
                StateParameter value => UI.Button(UI.Label(() => $"State {value.Value + 1}/{value.StateCount}"), () => value.Next()),
                _ => UI.Label("-")
            };
        }
        private static Element CreateSlotControl(IMidiBindableParameter parameter, int index)
        {
            return parameter switch
            {
                RadioParameter value => UI.Button(UI.Label(() => value.Value == index ? "Selected" : "Select"), () => value.TrySetValue(index)),
                SequenceParameter value => UI.Button(UI.Label(() => value.Steps[index] ? "ON" : "OFF"), () => value.ToggleStep(index)),
                _ => UI.Label("-")
            };
        }
        private string SlotLabel(string parameterId, MidiParameterKind kind, int index)
        {
            return _controller.TryGetButtonSlot(parameterId, kind, index, out var slot) && slot.Assigned
                ? $"P{slot.Page + 1}  R{slot.Row + 1}  C{slot.Column + 1}"
                : "Unassigned";
        }
    }
}
