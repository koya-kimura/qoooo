using System.Collections.Generic;
using qoooo.Text.Model;
using UnityEngine;

namespace qoooo.Text.Layouts
{
    public class TextCornerRotatePattern : MonoBehaviour, ITextPattern
    {
        [SerializeField] private Vector2 _size = new(10f, 6f);

        [SerializeField] [Min(0f)] private float _margin = 1f;

        [SerializeField] private float _rotationSpeed = 57.29578f;

        [SerializeField] private float _scale = 2f;

        public void Sample(string text, List<GlyphSample> output)
        {
            if (string.IsNullOrEmpty(text)) return;

            for (var y = -1; y <= 1; y += 2)
            for (var x = -1; x <= 1; x += 2)
            {
                var index =
                    (y == -1 ? 0 : 2)
                    + (x == -1 ? 0 : 1);

                var position =
                    new Vector3(
                        x * (_size.x * 0.5f - _margin),
                        y * (_size.y * 0.5f - _margin),
                        0f
                    );

                // x*y が
                //
                // 左下  +1
                // 右下  -1
                // 左上  -1
                // 右上  +1
                //
                // になる
                var direction = x * y;

                var angle =
                    Time.time
                    * _rotationSpeed
                    * direction;

                output.Add(new GlyphSample
                {
                    // 元TSは全部 "G"
                    // 今回は入力文字列の先頭文字
                    SourceCharacterIndex = 0,

                    Position = position,

                    Rotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            angle
                        ),

                    Scale =
                        Vector3.one * _scale,

                    GroupIndex = 0,
                    IndexInGroup = index,
                    CountInGroup = 4,

                    U = index / 4f
                });
            }
        }
    }
}