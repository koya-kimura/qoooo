namespace qoooo.Parameters.Model
{
    // registry、binding UI、mapping、persistence adapterが使うstableなkind。
    public enum MidiParameterKind { Toggle, Radio, Oneshot, Momentary, State, Sequence, Float }

    // owner保持Parameterの共通boundary。adapterがinstanceを変更し、consumerは変更しない。
    public interface IMidiBindableParameter
    {
        string Id { get; }
        string DisplayName { get; }
        MidiParameterKind Kind { get; }
        void InitializeFromDefault();
        void ResetToDefault();
    }
}
