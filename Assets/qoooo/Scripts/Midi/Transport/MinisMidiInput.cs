using System;
using System.Collections.Generic;
using Minis;
using qoooo.Midi.Apc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace qoooo.Midi.Transport
{
    /// <summary>Minis callbackをmain-thread Updateで確定したraw MIDI eventへ変換する。</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MinisMidiInput : MonoBehaviour, IMidiInput
    {
        private enum EventType { NoteOn, NoteOff, ControlChange }
        private readonly struct Event { public readonly EventType Type; public readonly int Number; public readonly float Value; public Event(EventType type,int number,float value){Type=type;Number=number;Value=value;} }
        [SerializeField] private string _productName = "APC mini mk2 Control";
        [SerializeField] private int _channel = ApcMiniMk2Constants.MidiChannel;
        [SerializeField] private bool _logInputEvents = true;
        private readonly List<Event> _pending = new();
        private readonly List<Event> _dispatching = new();
        private MidiDevice _device;
        private float _nextDeviceScan;
        private bool _waitingLogged;
        public event Action<int,float> NoteOn;
        public event Action<int> NoteOff;
        public event Action<int,float> ControlChange;
        public event Action<bool> ConnectionChanged;
        public bool IsConnected => _device != null;
        private void OnEnable() { InputSystem.onDeviceChange += OnDeviceChange; Rebind(); }
        private void OnDisable() { InputSystem.onDeviceChange -= OnDeviceChange; Unbind(); _pending.Clear(); _dispatching.Clear(); }
        private void Update()
        {
            if (Time.unscaledTime >= _nextDeviceScan)
            {
                _nextDeviceScan = Time.unscaledTime + 1f;
                if (_device == null || !IsStillAvailable(_device)) Rebind();
            }
            if (_pending.Count == 0) return;
            _dispatching.Clear(); _dispatching.AddRange(_pending); _pending.Clear();
            foreach (var item in _dispatching)
            {
                switch(item.Type)
                {
                    case EventType.NoteOn: NoteOn?.Invoke(item.Number,item.Value); break;
                    case EventType.NoteOff: NoteOff?.Invoke(item.Number); break;
                    case EventType.ControlChange: ControlChange?.Invoke(item.Number,item.Value); break;
                }
            }
        }
        private void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled) Rebind();
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused && isActiveAndEnabled) Rebind();
        }
        public void RequestReconnect()
        {
            if (isActiveAndEnabled) Rebind();
        }
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is MidiDevice && isActiveAndEnabled) Rebind();
        }
        private void Rebind()
        {
            _pending.Clear();
            _dispatching.Clear();
            Unbind();
            foreach(var item in InputSystem.devices)
            {
                if(item is not MidiDevice candidate || !candidate.added || !candidate.enabled || !Matches(candidate)) continue;
                _device=candidate; _device.onWillNoteOn += OnNoteOn; _device.onWillNoteOff += OnNoteOff; _device.onWillControlChange += OnControlChange;
                _waitingLogged = false;
                Debug.Log($"[MIDI] Input connected: '{candidate.description.product}' ch {candidate.channel}.",this); ConnectionChanged?.Invoke(true); return;
            }
            if (!_waitingLogged) Debug.Log($"[MIDI] Waiting for '{_productName}' ch {_channel}. Move a control once so Minis discovers it.",this);
            _waitingLogged = true;
            ConnectionChanged?.Invoke(false);
        }
        private static bool IsStillAvailable(MidiDevice device)
        {
            if (!device.added || !device.enabled) return false;
            foreach (var item in InputSystem.devices) if (ReferenceEquals(item, device)) return true;
            return false;
        }
        private bool Matches(MidiDevice value) => value.channel == _channel && (string.IsNullOrWhiteSpace(_productName) || (!string.IsNullOrEmpty(value.description.product) && value.description.product.IndexOf(_productName,StringComparison.OrdinalIgnoreCase)>=0));
        private void Unbind()
        {
            if(_device == null) return;
            _device.onWillNoteOn -= OnNoteOn; _device.onWillNoteOff -= OnNoteOff; _device.onWillControlChange -= OnControlChange;
            Debug.Log($"[MIDI] Input disconnected: '{_device.description.product}'.",this); _device=null; ConnectionChanged?.Invoke(false);
        }
        private void OnNoteOn(MidiNoteControl control,float value) { _pending.Add(new Event(value <= 0f ? EventType.NoteOff : EventType.NoteOn,control.noteNumber,value)); if(_logInputEvents) Debug.Log($"[MIDI] Note {(value<=0?"Off":"On")}: {control.noteNumber}, {value:F3}",this); }
        private void OnNoteOff(MidiNoteControl control) { _pending.Add(new Event(EventType.NoteOff,control.noteNumber,0)); if(_logInputEvents) Debug.Log($"[MIDI] Note Off: {control.noteNumber}",this); }
        private void OnControlChange(MidiValueControl control,float value) { _pending.Add(new Event(EventType.ControlChange,control.controlNumber,Mathf.Clamp01(value))); if(_logInputEvents) Debug.Log($"[MIDI] CC: {control.controlNumber}, {value:F3}",this); }
    }
}
