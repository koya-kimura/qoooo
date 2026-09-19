namespace qoooo.Midi.Transport
{
    public interface IMidiOutput
    {
        bool IsConnected { get; }
        void SendNoteOn(int note, byte velocity);
        void SendNoteOff(int note);
    }
}
