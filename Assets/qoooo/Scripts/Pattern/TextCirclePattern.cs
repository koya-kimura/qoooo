using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Pattern
{
    public class TextCirclePattern : MonoBehaviour, ITextPattern
    {
        [SerializeField] [Min(0f)] private float _radius = 3f;
        [SerializeField] private float _startAngle;
        [SerializeField] private bool _faceOutward = true;

        public void Sample(string text, List<GlyphSample> output)
        {
            if (string.IsNullOrEmpty(text)) return;

            for (var index = 0; index < text.Length; index++)
            {
                var angle = _startAngle + 360f * index / text.Length;
                var radians = angle * Mathf.Deg2Rad;
                var position = new Vector3(
                    Mathf.Cos(radians) * _radius,
                    Mathf.Sin(radians) * _radius,
                    0f);

                output.Add(new GlyphSample
                {
                    SourceCharacterIndex = index,
                    Position = position,
                    Rotation = _faceOutward
                        ? Quaternion.Euler(0f, 0f, angle - 90f)
                        : Quaternion.identity,
                    Scale = Vector3.one,
                    GroupIndex = 0,
                    IndexInGroup = index,
                    CountInGroup = text.Length,
                    U = (float)index / text.Length
                });
            }
        }
    }
}