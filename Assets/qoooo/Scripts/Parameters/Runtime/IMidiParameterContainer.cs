using System.Collections.Generic;
using qoooo.Parameters.Model;

namespace qoooo.Parameters.Runtime
{
    // discovery用にowner保持のParameterを公開する。source of truthはcontainerのまま。
    public interface IMidiParameterContainer
    {
        IEnumerable<IMidiBindableParameter> MidiParameters { get; }
    }
}
