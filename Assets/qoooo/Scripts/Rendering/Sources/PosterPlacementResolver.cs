using UnityEngine;

namespace qoooo.Rendering.Sources
{
    internal readonly struct PosterResolvedPlacement
    {
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly Vector2 Pivot;

        public PosterResolvedPlacement(Vector2 position, Vector2 size, Vector2 pivot)
        {
            Position = position;
            Size = size;
            Pivot = pivot;
        }

        public Rect Rect => new(
            Position.x - Size.x * Pivot.x,
            Position.y - Size.y * (1f - Pivot.y),
            Size.x,
            Size.y);
    }

    internal static class PosterPlacementResolver
    {
        public static PosterResolvedPlacement Resolve(
            PosterGridDefinition grid,
            PosterPlacement placement,
            float canvasWidth,
            float canvasHeight)
        {
            grid ??= new PosterGridDefinition();
            placement ??= new PosterPlacement();

            var normalizedPosition = placement.Mode == PosterPlacementMode.Grid
                ? ResolveGridPoint(grid, placement)
                : new Vector2(Mathf.Clamp01(placement.X), Mathf.Clamp01(placement.Y));
            var position = Vector2.Scale(normalizedPosition, new Vector2(canvasWidth, canvasHeight));
            var normalizedSize = placement.Mode == PosterPlacementMode.Grid
                ? new Vector2(
                    Mathf.Max(0f, placement.Width) / Mathf.Max(1, grid.Columns),
                    Mathf.Max(0f, placement.Height) / Mathf.Max(1, grid.Rows))
                : new Vector2(Mathf.Clamp01(placement.Width), Mathf.Clamp01(placement.Height));
            var size = Vector2.Scale(normalizedSize, new Vector2(canvasWidth, canvasHeight));

            return new PosterResolvedPlacement(position, size, ResolvePivot(placement.Anchor));
        }

        private static Vector2 ResolveGridPoint(PosterGridDefinition grid, PosterPlacement placement)
        {
            var columns = Mathf.Max(1, grid.Columns);
            var rows = Mathf.Max(1, grid.Rows);
            var pointX = Mathf.Clamp(placement.GridPointX, 0, columns - 1);
            var pointY = Mathf.Clamp(placement.GridPointY, 0, rows - 1);
            return new Vector2(
                columns == 1 ? 0f : pointX / (float)(columns - 1),
                rows == 1 ? 0f : pointY / (float)(rows - 1));
        }

        internal static Vector2 ResolvePivot(PosterAnchor anchor)
        {
            return anchor switch
            {
                PosterAnchor.Top => new Vector2(0.5f, 1f),
                PosterAnchor.TopRight => new Vector2(1f, 1f),
                PosterAnchor.Left => new Vector2(0f, 0.5f),
                PosterAnchor.Center => new Vector2(0.5f, 0.5f),
                PosterAnchor.Right => new Vector2(1f, 0.5f),
                PosterAnchor.BottomLeft => new Vector2(0f, 0f),
                PosterAnchor.Bottom => new Vector2(0.5f, 0f),
                PosterAnchor.BottomRight => new Vector2(1f, 0f),
                _ => new Vector2(0f, 1f)
            };
        }
    }

    internal static class PosterLegacyPlacementConverter
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        private const int LegacyColumnCount = 12;

        public static PosterPlacement FromGrid(
            int column,
            int columnSpan,
            int baseline,
            int baselineSpan,
            float offsetX = 0f,
            float offsetY = 0f)
        {
            var sizeUnit = Mathf.Min(ReferenceWidth, ReferenceHeight);
            var margin = sizeUnit * 0.06f;
            var gutter = sizeUnit * 0.02f;
            var baselineHeight = sizeUnit * 0.04f;
            var columnWidth = (ReferenceWidth - margin * 2f - gutter * (LegacyColumnCount - 1))
                              / LegacyColumnCount;
            column = Mathf.Clamp(column, 0, LegacyColumnCount - 1);
            columnSpan = Mathf.Clamp(columnSpan, 1, LegacyColumnCount - column);

            var rect = new Rect(
                margin + column * (columnWidth + gutter) + offsetX * sizeUnit,
                margin + Mathf.Max(0, baseline) * baselineHeight + offsetY * sizeUnit,
                columnWidth * columnSpan + gutter * (columnSpan - 1),
                baselineHeight * Mathf.Max(1, baselineSpan));
            return FromPixelRect(rect, PosterAnchor.TopLeft);
        }

        public static PosterPlacement FromFree(
            float x,
            float y,
            float width,
            float height,
            float offsetX,
            float offsetY)
        {
            var sizeUnit = Mathf.Min(ReferenceWidth, ReferenceHeight);
            var resolvedWidth = width > 0f ? width : ReferenceWidth - x;
            var resolvedHeight = height > 0f ? height : ReferenceHeight - y;
            var rect = new Rect(
                x + offsetX * sizeUnit,
                y + offsetY * sizeUnit,
                Mathf.Max(0f, resolvedWidth),
                Mathf.Max(0f, resolvedHeight));
            return FromPixelRect(rect, PosterAnchor.TopLeft);
        }

        public static PosterPlacement WithAnchor(PosterPlacement placement, PosterAnchor anchor)
        {
            var pivot = PosterPlacementResolver.ResolvePivot(anchor);
            placement.X += placement.Width * pivot.x;
            placement.Y += placement.Height * (1f - pivot.y);
            placement.Anchor = anchor;
            return placement;
        }

        private static PosterPlacement FromPixelRect(Rect rect, PosterAnchor anchor)
        {
            var placement = new PosterPlacement
            {
                Mode = PosterPlacementMode.Free,
                Anchor = PosterAnchor.TopLeft,
                X = rect.x / ReferenceWidth,
                Y = rect.y / ReferenceHeight,
                Width = rect.width / ReferenceWidth,
                Height = rect.height / ReferenceHeight
            };
            return anchor == PosterAnchor.TopLeft ? placement : WithAnchor(placement, anchor);
        }
    }
}