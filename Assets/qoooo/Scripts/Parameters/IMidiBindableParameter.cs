namespace qoooo.Parameters
{
    public enum MidiParameterKind { Toggle, Radio, Oneshot, Momentary, State, Sequence, Float }

    public interface IMidiBindableParameter
    {
        string Id { get; }
        string DisplayName { get; }
        MidiParameterKind Kind { get; }
        void InitializeFromDefault();
        void ResetToDefault();
    }
}
