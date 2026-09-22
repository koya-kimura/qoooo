namespace qoooo.Midi.Transport
{
    // Thread-safe byte-message boundary used by LED rendering without exposing the output worker.
    public interface IMidiOutput
    {
        bool IsConnected { get; }
        void SendNoteOn(int note, byte velocity);
        void SendNoteOff(int note);
    }
}
