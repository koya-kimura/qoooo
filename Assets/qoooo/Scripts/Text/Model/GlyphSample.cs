using UnityEngine;

namespace qoooo.Text.Model
{
    // One sampled Legacy Text glyph transform; inactive in Trip26 final composition.
    public struct GlyphSample
    {
        public int SourceCharacterIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int GroupIndex;
        public int IndexInGroup;
        public int CountInGroup;
        public float U;
    }
}
