using System.Collections.Generic;

namespace qoooo.Pattern
{
    public interface ITextPattern
    {
        void Sample(string text, List<GlyphSample> output);
    }
}
