using System;

namespace qoooo.Midi
{
    public interface IMidiInput
    {
        event Action<int, float> NoteOn;
        event Action<int> NoteOff;
        event Action<int, float> ControlChange;
        event Action<bool> ConnectionChanged;
        bool IsConnected { get; }
    }
}
