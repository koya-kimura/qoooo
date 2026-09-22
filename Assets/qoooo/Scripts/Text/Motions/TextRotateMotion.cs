using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Motions
{
    // CREATIVE AREA / 創作領域: sampleしたLegacy Text glyphへtime-based rotationを適用する。
    // この経路はTrip26のfinal compositionではinactive。
    public class TextRotateMotion : TextGlyphMotion
    {
        [SerializeField] private Vector3 _axis = Vector3.forward;
        [SerializeField] private float _degreesPerSecond = 30f;
        [SerializeField] private bool _rotateGlyph = true;

        public override void Apply(ref GlyphSample sample, float time)
        {
            var rotation = Quaternion.AngleAxis(time * _degreesPerSecond, _axis.normalized);
            sample.Position = rotation * sample.Position;
            if (_rotateGlyph) sample.Rotation = rotation * sample.Rotation;
        }
    }
}
