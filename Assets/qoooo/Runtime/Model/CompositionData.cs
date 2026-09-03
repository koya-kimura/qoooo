using System;
using System.Collections.Generic;

namespace Qoooo.VJ.Model
{
    [Serializable]
    public sealed class CompositionData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<LayerData> layers = new();
    }
}
