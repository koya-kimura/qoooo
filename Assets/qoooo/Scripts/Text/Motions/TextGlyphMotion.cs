using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Motions
{
    public abstract class TextGlyphMotion : MonoBehaviour
    {
        public abstract void Apply(ref GlyphSample sample, float time);
    }
}
