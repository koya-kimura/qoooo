using System.Collections.Generic;

namespace qoooo.Text.Model
{
    public interface ITextPattern
    {
        void Sample(string text, List<GlyphSample> output);
    }
}
