using UnityEngine;

namespace qoooo.Pattern
{
    public abstract class TextGlyphMotion : MonoBehaviour
    {
        public abstract void Apply(ref GlyphSample sample, float time);
    }
}
