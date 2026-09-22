using System.Collections.Generic;
using qoooo.Parameters.Model;

namespace qoooo.Parameters.Runtime
{
    /// <summary>
    /// nested componentのMIDI Parameter。所有者であるIMidiParameterContainerがregistryへ公開し、
    /// DIによる重複登録を防ぐ。
    /// </summary>
    // nested sourceのParameterを公開し、包含するLayerが同じinstanceをforwardできるようにする。
    public interface IMidiParameterSource
    {
        IEnumerable<IMidiBindableParameter> MidiParameters { get; }
    }
}
