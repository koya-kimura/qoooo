using System.Collections.Generic;
using qoooo.Util;
using UnityEngine;

namespace qoooo.Pattern
{
    public class TextGridPattern : MonoBehaviour, ITextPattern
    {
        [SerializeField] [Min(1)] private int _columns = 10;
        [SerializeField] [Min(1)] private int _rows = 6;

        [SerializeField] private Vector2 _size = new(10f, 6f);
        [SerializeField] [Min(0.01f)] private float _cycleLength = 16f;
        [SerializeField] [Min(0.01f)] private float _easeDuration = 2f;

        [SerializeField] private float _timeScale = 2f;
        [SerializeField] [Min(0f)] private float _randomTimeRange = 1000f;

        public void Sample(string text, List<GlyphSample> output)
        {
            if (string.IsNullOrEmpty(text)) return;

            var count = _columns * _rows;
            var t = Time.time * _timeScale;

            for (var y = 0; y < _rows; y++)
            for (var x = 0; x < _columns; x++)
            {
                var index = y * _columns + x;

                var ux = _columns <= 1
                    ? 0.5f
                    : (float)x / (_columns - 1);

                var uy = _rows <= 1
                    ? 0.5f
                    : (float)y / (_rows - 1);

                var position = new Vector3(
                    Mathf.Lerp(
                        -_size.x * 0.5f,
                        _size.x * 0.5f,
                        ux
                    ),
                    Mathf.Lerp(
                        _size.y * 0.5f,
                        -_size.y * 0.5f,
                        uy
                    ),
                    0f
                );

                var random = Pcg.Pcg01(
                    new Vector2(y, x)
                ).x;

                var timeOffset = Mathf.Floor(
                    random * _randomTimeRange
                );

                var phase = Rhythm.GetPhase(
                    t + timeOffset,
                    _cycleLength,
                    _easeDuration
                );

                var characterIndex =
                    Mathf.FloorToInt(phase * 16f);

                characterIndex =
                    PositiveModulo(
                        characterIndex,
                        text.Length
                    );

                output.Add(new GlyphSample
                {
                    SourceCharacterIndex = characterIndex,

                    Position = position,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,

                    GroupIndex = y,
                    IndexInGroup = x,
                    CountInGroup = _columns,

                    U = (float)index / count
                });
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            return (value % modulo + modulo) % modulo;
        }
    }
}