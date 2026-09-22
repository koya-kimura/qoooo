using System;

namespace qoooo.Midi.Transport
{
    // Main-thread raw MIDI event boundary consumed by the APC Parameter adapter.
    public interface IMidiInput
    {
        event Action<int, float> NoteOn;
        event Action<int> NoteOff;
        event Action<int, float> ControlChange;
        event Action<bool> ConnectionChanged;
        bool IsConnected { get; }
    }
}
