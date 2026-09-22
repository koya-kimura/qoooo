using System.Collections.Generic;

namespace qoooo.Text.Model
{
    // Legacy Text layout sampler contract; active Poster Text uses PosterElement data instead.
    public interface ITextPattern
    {
        void Sample(string text, List<GlyphSample> output);
    }
}
