using System.Collections.Generic;

namespace qoooo.Parameters
{
    /// <summary>
    /// A nested component's MIDI parameters. The owning IMidiParameterContainer is responsible
    /// for exposing these to the registry, preventing duplicate registration by DI.
    /// </summary>
    public interface IMidiParameterSource
    {
        IEnumerable<IMidiBindableParameter> MidiParameters { get; }
    }
}
