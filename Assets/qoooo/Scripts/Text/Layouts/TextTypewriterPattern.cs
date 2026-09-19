using System.Collections.Generic;
using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Layouts
{
    public class TextTypewriterPattern :
        MonoBehaviour,
        ITextPattern
    {
        [SerializeField] [Min(0.01f)] private float _charactersPerSecond = 16f;

        [SerializeField] [Min(0f)] private float _pauseDuration = 4f;

        [SerializeField] private float _spacing = 0.6f;

        [SerializeField] private Vector3 _offset;

        public float Spacing => _spacing;

        public void Sample(
            string text,
            List<GlyphSample> output)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var typingDuration =
                text.Length
                / _charactersPerSecond;

            var cycleDuration =
                typingDuration
                + _pauseDuration;

            var localTime =
                Mathf.Repeat(
                    Time.time,
                    cycleDuration
                );

            var visibleCount =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        localTime
                        * _charactersPerSecond
                    ),
                    0,
                    text.Length
                );

            var totalWidth =
                (text.Length - 1)
                * _spacing;

            for (var i = 0;
                 i < visibleCount;
                 i++)
            {
                var x =
                    i * _spacing
                    - totalWidth * 0.5f;

                output.Add(
                    new GlyphSample
                    {
                        SourceCharacterIndex = i,

                        Position =
                            _offset
                            + new Vector3(
                                x,
                                0f,
                                0f
                            ),

                        Rotation =
                            Quaternion.identity,

                        Scale =
                            Vector3.one,

                        GroupIndex =
                            0,

                        IndexInGroup =
                            i,

                        CountInGroup =
                            text.Length,

                        U =
                            text.Length <= 1
                                ? 0f
                                : (float)i
                                  / (text.Length - 1)
                    }
                );
            }
        }
    }
}
