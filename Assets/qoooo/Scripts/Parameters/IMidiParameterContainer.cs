using System.Collections.Generic;

namespace qoooo.Parameters
{
    public interface IMidiParameterContainer
    {
        IEnumerable<IMidiBindableParameter> MidiParameters { get; }
    }
}
