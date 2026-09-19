using UnityEngine;

namespace qoooo.Text.Model
{
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
