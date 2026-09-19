using System.Collections.Generic;
using qoooo.Foundation.Utilities;
using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Layouts
{
    public class TextRectangleLoopPattern : MonoBehaviour, ITextPattern
    {
        [SerializeField] private Vector2 _size = new(10f, 6f);

        [SerializeField] private float[] _loopScales =
        {
            0.5f,
            0.95f
        };

        [SerializeField] [Min(1)] private int _baseGlyphCount = 100;

        [SerializeField] private float _moveSpeed = 1f;

        [SerializeField] [Min(0f)] private float _cornerDistance = 0.2f;

        [SerializeField] [Range(0f, 1f)] private float _normalCharacterProbability = 0.9f;

        // 元TSの _beat = millis / 500
        [SerializeField] private float _timeScale = 2f;

        public void Sample(
            string text,
            List<GlyphSample> output)
        {
            if (string.IsNullOrEmpty(text)) return;

            var beat =
                Time.time * _timeScale;

            for (var group = 0;
                 group < _loopScales.Length;
                 group++)
            {
                var loopScale =
                    _loopScales[group];

                var width =
                    _size.x * loopScale;

                var height =
                    _size.y * loopScale;

                var perimeter =
                    width * 2f +
                    height * 2f;

                var glyphCount =
                    Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            _baseGlyphCount
                            * loopScale
                        )
                    );

                for (var i = 0;
                     i < glyphCount;
                     i++)
                {
                    var u =
                        (float)i / glyphCount;

                    var distance =
                        Mathf.Repeat(
                            u * perimeter
                            + Time.time * _moveSpeed,
                            perimeter
                        );

                    GetRectangleSample(
                        distance,
                        width,
                        height,
                        perimeter,
                        out var position,
                        out var angle
                    );

                    angle =
                        GetCornerRotation(
                            distance,
                            width,
                            height,
                            perimeter,
                            angle
                        );

                    var characterIndex =
                        GetCharacterIndex(
                            text,
                            i,
                            group,
                            beat
                        );

                    output.Add(
                        new GlyphSample
                        {
                            SourceCharacterIndex =
                                characterIndex,

                            Position =
                                position,

                            Rotation =
                                Quaternion.Euler(
                                    0f,
                                    0f,
                                    angle
                                ),

                            Scale =
                                Vector3.one,

                            GroupIndex =
                                group,

                            IndexInGroup =
                                i,

                            CountInGroup =
                                glyphCount,

                            U =
                                u
                        }
                    );
                }
            }
        }

        private static void GetRectangleSample(
            float distance,
            float width,
            float height,
            float perimeter,
            out Vector3 position,
            out float angle)
        {
            if (distance < width)
            {
                // 上辺
                position =
                    new Vector3(
                        -width * 0.5f + distance,
                        height * 0.5f,
                        0f
                    );

                angle = 0f;
            }
            else if (
                distance < width + height)
            {
                // 右辺
                var d =
                    distance - width;

                position =
                    new Vector3(
                        width * 0.5f,
                        height * 0.5f - d,
                        0f
                    );

                angle = -90f;
            }
            else if (
                distance <
                width * 2f + height)
            {
                // 下辺
                var d =
                    distance -
                    width -
                    height;

                position =
                    new Vector3(
                        width * 0.5f - d,
                        -height * 0.5f,
                        0f
                    );

                angle = 180f;
            }
            else
            {
                // 左辺
                var d =
                    distance -
                    width * 2f -
                    height;

                position =
                    new Vector3(
                        -width * 0.5f,
                        -height * 0.5f + d,
                        0f
                    );

                angle = 90f;
            }
        }

        private float GetCornerRotation(
            float distance,
            float width,
            float height,
            float perimeter,
            float defaultAngle)
        {
            var d = _cornerDistance;

            // 右上
            if (Mathf.Abs(distance - width) < d)
            {
                var t =
                    Mathf.InverseLerp(
                        width - d,
                        width + d,
                        distance
                    );

                return Mathf.Lerp(
                    0f,
                    -90f,
                    t
                );
            }

            // 右下
            var corner2 =
                width + height;

            if (Mathf.Abs(distance - corner2) < d)
            {
                var t =
                    Mathf.InverseLerp(
                        corner2 - d,
                        corner2 + d,
                        distance
                    );

                return Mathf.Lerp(
                    -90f,
                    -180f,
                    t
                );
            }

            // 左下
            var corner3 =
                width * 2f + height;

            if (Mathf.Abs(distance - corner3) < d)
            {
                var t =
                    Mathf.InverseLerp(
                        corner3 - d,
                        corner3 + d,
                        distance
                    );

                return Mathf.Lerp(
                    -180f,
                    -270f,
                    t
                );
            }

            // 左上
            if (
                distance < d ||
                distance > perimeter - d)
            {
                var wrappedDistance =
                    distance < d
                        ? distance + perimeter
                        : distance;

                var t =
                    Mathf.InverseLerp(
                        perimeter - d,
                        perimeter + d,
                        wrappedDistance
                    );

                return Mathf.Lerp(
                    -270f,
                    -360f,
                    t
                );
            }

            return defaultAngle;
        }

        private int GetCharacterIndex(
            string text,
            int glyphIndex,
            int group,
            float beat)
        {
            // グループと文字番号から
            // 安定したseedを作る
            var seed =
                glyphIndex
                + group * 75920;

            var beatIndex =
                Mathf.Floor(beat);

            var random =
                Pcg.Pcg01(
                    new Vector2(
                        seed,
                        beatIndex
                    )
                ).x;

            // 90%は通常通りの文字
            if (random <
                _normalCharacterProbability)
                return glyphIndex
                       % text.Length;

            // 残りは高速にランダム文字へ変化
            var fastBeat =
                Mathf.Floor(beat * 8f);

            var randomCharacter =
                Pcg.Pcg01(
                    new Vector2(
                        seed,
                        fastBeat
                    )
                ).x;

            return Mathf.FloorToInt(
                       randomCharacter
                       * text.Length
                   )
                   % text.Length;
        }
    }
}