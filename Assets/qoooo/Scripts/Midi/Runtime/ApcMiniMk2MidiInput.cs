using System;
using System.Collections.Generic;
using Minis;
using qoooo.Foundation.Timing;
using qoooo.Midi.Apc;
using qoooo.Midi.Bindings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnitySimpleContainer;

namespace qoooo.Midi.Runtime
{
    [DefaultExecutionOrder(-90)]
    public sealed class ApcMiniMk2MidiInput : MonoBehaviour
    {
        private enum PendingMidiEventType { NoteOn, NoteOff, ControlChange }

        private readonly struct PendingMidiEvent
        {
            public readonly PendingMidiEventType Type;
            public readonly int Number;
            public readonly float Value;

            public PendingMidiEvent(PendingMidiEventType type, int number, float value)
            {
                Type = type;
                Number = number;
                Value = value;
            }
        }

        [SerializeField] private string _productName = "APC mini mk2 Control";
        [SerializeField] private int _channel = ApcMiniMk2Constants.MidiChannel;
        private IBpmSource _bpmSource;
        [SerializeField] private List<ApcMiniMk2Binding> _bindings = new();
        [SerializeField] private List<FaderButtonFunction> _faderButtonFunctions = new();
        [SerializeField] private bool _logInputEvents = true;

        private readonly List<PendingMidiEvent> _pendingEvents = new();
        private readonly List<PendingMidiEvent> _dispatchingEvents = new();
        private MidiDevice _device;
        private IApcMiniMk2Controller _controller;

        public bool MidiSuccess => _device != null;
        public float FaderValue(int index) => _controller?.FaderValue(index) ?? 0f;
        public bool BooleanValue(string key) => _controller?.BooleanValue(key) ?? false;
        public int RadioValue(string key) => _controller?.RadioValue(key) ?? 0;
        public int StateValue(string key) => _controller?.StateValue(key) ?? 0;
        public bool SequenceActive(string key) => _controller?.SequenceActive(key) ?? false;

        [Inject]
        public void Construct(IBpmSource bpmSource)
        {
            _bpmSource = bpmSource;
        }

        private void OnEnable()
        {
            InitializeController();
            InputSystem.onDeviceChange += HandleDeviceChange;
            RebindDevice();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
            UnsubscribeCurrentDevice();
            _pendingEvents.Clear();
            _dispatchingEvents.Clear();
            _controller = null;
        }

        private void Update()
        {
            if (_controller == null) return;

            DrainInputEvents();
            if (_bpmSource == null) return;
            _controller.UpdateState(_bpmSource.Beat);
        }

        private void InitializeController()
        {
            var registry = ApcMiniMk2BindingRegistry.Create(_bindings);
            if (!registry.IsValid)
            {
                foreach (var error in registry.Errors) Debug.LogError($"[APC MIDI] Mapping error: {error}", this);
                _controller = null;
                return;
            }

            _controller = new ApcMiniMk2Controller(registry, _faderButtonFunctions);
            Debug.Log($"[APC MIDI] Controller ready. Bindings: {registry.Bindings.Count}; channel: {_channel}; product filter: '{_productName}'.", this);
        }

        private void HandleDeviceChange(InputDevice inputDevice, InputDeviceChange change)
        {
            if (inputDevice is not MidiDevice) return;
            Debug.Log($"[APC MIDI] Device change: {change}; product: '{inputDevice.description.product}'.", this);
            RebindDevice();
        }

        private void RebindDevice()
        {
            UnsubscribeCurrentDevice();

            foreach (var inputDevice in InputSystem.devices)
            {
                if (inputDevice is not MidiDevice candidate || !Matches(candidate)) continue;

                _device = candidate;
                _device.onWillNoteOn += HandleNoteOn;
                _device.onWillNoteOff += HandleNoteOff;
                _device.onWillControlChange += HandleControlChange;
                Debug.Log($"[APC MIDI] Connected: '{candidate.description.product}' (channel {candidate.channel}).", this);
                return;
            }

            Debug.Log($"[APC MIDI] Waiting for '{_productName}' on channel {_channel}. Move a control once so Minis can discover it.", this);
        }

        private bool Matches(MidiDevice candidate)
        {
            if (candidate.channel != _channel) return false;
            if (string.IsNullOrWhiteSpace(_productName)) return true;
            return !string.IsNullOrEmpty(candidate.description.product)
                && candidate.description.product.IndexOf(_productName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UnsubscribeCurrentDevice()
        {
            if (_device == null) return;

            _device.onWillNoteOn -= HandleNoteOn;
            _device.onWillNoteOff -= HandleNoteOff;
            _device.onWillControlChange -= HandleControlChange;
            Debug.Log($"[APC MIDI] Disconnected: '{_device.description.product}'.", this);
            _device = null;
        }

        private void HandleNoteOn(MidiNoteControl control, float velocity)
        {
            _pendingEvents.Add(new PendingMidiEvent(PendingMidiEventType.NoteOn, control.noteNumber, velocity));
            if (_logInputEvents) Debug.Log($"[APC MIDI] Note On queued: note={control.noteNumber}, velocity={velocity:F3}.", this);
        }

        private void HandleNoteOff(MidiNoteControl control)
        {
            _pendingEvents.Add(new PendingMidiEvent(PendingMidiEventType.NoteOff, control.noteNumber, 0f));
            if (_logInputEvents) Debug.Log($"[APC MIDI] Note Off queued: note={control.noteNumber}.", this);
        }

        private void HandleControlChange(MidiValueControl control, float value)
        {
            _pendingEvents.Add(new PendingMidiEvent(PendingMidiEventType.ControlChange, control.controlNumber, value));
            if (_logInputEvents) Debug.Log($"[APC MIDI] CC queued: cc={control.controlNumber}, value={value:F3}.", this);
        }

        private void DrainInputEvents()
        {
            if (_pendingEvents.Count == 0) return;

            _dispatchingEvents.Clear();
            _dispatchingEvents.AddRange(_pendingEvents);
            _pendingEvents.Clear();

            foreach (var midiEvent in _dispatchingEvents)
            {
                switch (midiEvent.Type)
                {
                    case PendingMidiEventType.NoteOn:
                        _controller.ProcessNoteOn(midiEvent.Number, midiEvent.Value);
                        break;
                    case PendingMidiEventType.NoteOff:
                        _controller.ProcessNoteOff(midiEvent.Number);
                        break;
                    case PendingMidiEventType.ControlChange:
                        _controller.ProcessControlChange(midiEvent.Number, midiEvent.Value);
                        break;
                }
            }
        }
    }
}
