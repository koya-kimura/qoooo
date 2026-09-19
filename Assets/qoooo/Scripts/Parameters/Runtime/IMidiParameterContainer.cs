using System.Collections.Generic;
using qoooo.Parameters.Model;

namespace qoooo.Parameters.Runtime
{
    public interface IMidiParameterContainer
    {
        IEnumerable<IMidiBindableParameter> MidiParameters { get; }
    }
}
