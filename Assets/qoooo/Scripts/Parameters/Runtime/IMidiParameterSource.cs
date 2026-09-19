using System.Collections.Generic;
using qoooo.Parameters.Model;

namespace qoooo.Parameters.Runtime
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
