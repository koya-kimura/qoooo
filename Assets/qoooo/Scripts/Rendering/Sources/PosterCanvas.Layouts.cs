using UnityEngine;

namespace qoooo.Rendering.Sources
{
    // Builds active Poster Text and graphic primitives; this is distinct from inactive Legacy Text.
    // Document elements are listed front-to-back and created in reverse so index zero stays on top.
    public partial class PosterCanvas
    {
        // Layout code uses zero-based columns throughout: 0..11.
        private readonly struct PosterGrid
        {
            public const int ColumnCount = 12;

            public readonly float Margin;
            public readonly float Gutter;
            public readonly float Baseline;
            public readonly float XS;
            public readonly float S;
            public readonly float M;
            public readonly float L;
            public readonly float XL;

            private readonly float _width;
            private readonly float _columnWidth;

            public PosterGrid(float width, float minSize)
            {
                _width = width;
                Margin = minSize * 0.06f;
                Gutter = minSize * 0.02f;
                Baseline = minSize * 0.04f;
                XS = minSize * 0.01f;
                S = minSize * 0.02f;
                M = minSize * 0.04f;
                L = minSize * 0.08f;
                XL = minSize * 0.16f;
                _columnWidth = (width - Margin * 2f - Gutter * (ColumnCount - 1)) / ColumnCount;
            }

            public float Left => Margin;
            public float Right => _width - Margin;

            public float BaselineAt(int index) => Margin + Baseline * index;

            public float ColumnStart(int column)
            {
                return Margin + column * (_columnWidth + Gutter);
            }

            public float ColumnEnd(int column)
            {
                return ColumnStart(column) + _columnWidth;
            }

            public float ColumnCenter(int startColumn, int columnSpan)
            {
                return ColumnSpanRect(startColumn, columnSpan, 0f, 0f).center.x;
            }

            public Rect ColumnSpanRect(int startColumn, int columnSpan, float y, float height)
            {
                var width = _columnWidth * columnSpan + Gutter * (columnSpan - 1);
                return new Rect(ColumnStart(startColumn), y, width, height);
            }

            public Rect Rect(int startColumn, int columnSpan, int baselineStart, int baselineSpan)
            {
                return ColumnSpanRect(
                    startColumn,
                    columnSpan,
                    BaselineAt(baselineStart),
                    Baseline * baselineSpan);
            }
        }

        private PosterGrid Grid => new(Width, SizeUnit);

    }
}
