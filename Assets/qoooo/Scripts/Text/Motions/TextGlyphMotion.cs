using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Motions
{
    // sampleしたLegacy Text glyphをpost-processする。active Poster Textはこのboundaryを使わない。
    public abstract class TextGlyphMotion : MonoBehaviour
    {
        public abstract void Apply(ref GlyphSample sample, float time);
    }
}
