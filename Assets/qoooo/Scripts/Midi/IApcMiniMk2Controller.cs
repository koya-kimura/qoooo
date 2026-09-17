namespace qoooo.Midi
{
    public interface IApcMiniMk2Controller
    {
        void ProcessNoteOn(int noteNumber, float velocity);
        void ProcessNoteOff(int noteNumber);
        void ProcessControlChange(int controlNumber, float value);
        void UpdateState(double beat);

        float FaderValue(int index);
        bool BooleanValue(string key);
        int RadioValue(string key);
        int StateValue(string key);
        bool SequenceActive(string key);
    }
}
