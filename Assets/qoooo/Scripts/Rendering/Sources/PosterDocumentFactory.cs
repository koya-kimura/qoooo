using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
    // 初期編集可能documentを作る。runtimeでは他のPosterLibrary documentと同じように扱う。
    internal static class PosterDocumentFactory
    {
        public static PosterLibrary CreateDefaultLibrary()
        {
            return new PosterLibrary
            {
                Documents = new List<PosterDocument>
                {
                    CreateDefaultEditorial(),
                    CreateDefaultSplit(),
                    CreateDefaultModular(),
                    CreateDefaultCustom()
                }
            };
        }

        public static PosterDocument CreateDefaultEditorial()
        {
            var elements = new List<PosterElement>();
            AddText(elements, "Top Left Meta", "09.26 / SATURDAY", LegacyGrid(0, 12, 1, 2, 0.02f), 0.025f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddText(elements, "Top Right Meta", "UNITY / 2026", LegacyGrid(0, 12, 1, 2, -0.02f), 0.025f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddMainText(elements, "Hero", LegacyGrid(1, 10, 3, 5), 0.225f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddText(elements, "Secondary", "HAPILABO / DJ RØKI", LegacyGrid(4, 4, 8, 2), 0.042f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddText(elements, "Bottom Left Meta", "NAEBA / NIIGATA", LegacyGrid(0, 12, 20, 2, 0.02f), 0.026f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddText(elements, "Bottom Right Meta", "128 BPM / LIVE", LegacyGrid(0, 12, 20, 2, -0.02f), 0.026f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Base);

            AddRect(elements, "Top Meta Band", LegacyGrid(0, 12, 1, 2), PosterColorRef.Sub);
            AddRect(elements, "Hero Band", LegacyGrid(1, 10, 3, 5), PosterColorRef.Main);
            AddRect(elements, "Secondary Band", LegacyGrid(4, 4, 8, 2), PosterColorRef.Sub);
            AddRect(elements, "Bottom Meta Band", LegacyGrid(0, 12, 20, 2), PosterColorRef.Sub);
            AddTexture(elements, "Hero Model", PosterTextureSource.Model, LegacyGrid(2, 8, 7, 12), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Editorial", elements);
        }

        public static PosterDocument CreateDefaultSplit()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Hero", LegacyGrid(0, 5, 2, 6, 0.04f), 0.15f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Artist", "HAPILABO", LegacyGrid(0, 5, 10, 1, 0.04f), 0.052f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Artist", "DJ RØKI", LegacyGrid(0, 5, 11, 1, 0.04f), 0.052f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Date", "09.26 / SATURDAY", LegacyGrid(0, 5, 14, 1, 0.04f), 0.03f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Location", "NAEBA / NIIGATA", LegacyGrid(0, 5, 21, 1, 0.04f), 0.03f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Side Meta", "UNITY", LegacyGrid(11, 1, 3, 15), 0.032f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Base, PosterRotation.TwoSeventy);
            AddText(elements, "Bottom Meta", "128 BPM / LIVE", LegacyGrid(6, 5, 20, 2), 0.03f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Base);

            AddRect(elements, "Typography Block", LegacyGrid(0, 5, 2, 20), PosterColorRef.Main);
            AddRect(elements, "Side Meta Panel", LegacyGrid(11, 1, 3, 15), PosterColorRef.Sub);
            AddRect(elements, "Bottom Meta Panel", LegacyGrid(6, 5, 20, 2), PosterColorRef.Sub);
            AddTexture(elements, "Split Model", PosterTextureSource.Model, LegacyGrid(6, 6, 2, 20), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Split", elements);
        }

        public static PosterDocument CreateDefaultModular()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Headline", LegacyGrid(0, 4, 1, 5, 0.04f), 0.115f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Secondary", "HAPILABO / DJ RØKI", LegacyGrid(0, 4, 5, 1, 0.04f), 0.036f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Date", "09.26", LegacyGrid(9, 3, 1, 1), 0.03f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Year", "2026", LegacyGrid(9, 3, 2, 1), 0.024f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top, PosterColorRef.Base);
            AddText(elements, "Bottom Left Meta", "VISUALS BY QOOOO", LegacyGrid(0, 12, 20, 2, 0.02f), 0.026f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddText(elements, "Bottom Right Meta", "128 BPM / LIVE", LegacyGrid(0, 12, 20, 2, -0.02f), 0.026f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Base);

            AddRect(elements, "Headline Panel", LegacyGrid(0, 4, 1, 5), PosterColorRef.Main);
            AddRect(elements, "Date Panel", LegacyGrid(9, 3, 1, 3), PosterColorRef.Sub);
            AddRect(elements, "Bottom Meta Panel", LegacyGrid(0, 12, 20, 2), PosterColorRef.Sub);
            AddTexture(elements, "Large Model", PosterTextureSource.Model, LegacyGrid(4, 6, 6, 13), PosterFit.Cover);
            AddTexture(elements, "Small Model Left", PosterTextureSource.Model, LegacyGrid(0, 3, 9, 4), PosterFit.Cover);
            AddTexture(elements, "Small Model Right", PosterTextureSource.Model, LegacyGrid(9, 3, 3, 3), PosterFit.Cover);
            AddTexture(elements, "Small Model Bottom", PosterTextureSource.Model, LegacyGrid(1, 3, 16, 3), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Modular", elements);
        }

        public static PosterDocument CreateDefaultCustom()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Headline", LegacyGrid(1, 10, 2, 4), 0.16f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Base);
            AddRect(elements, "Headline Band", LegacyGrid(1, 10, 2, 4), PosterColorRef.Main);
            AddTexture(elements, "Model", PosterTextureSource.Model, LegacyGrid(3, 6, 7, 12), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Custom", elements);
        }

        private static PosterDocument Document(string name, List<PosterElement> elements)
        {
            return new PosterDocument
            {
                Name = name,
                Grid = new PosterGridDefinition(),
                Elements = elements
            };
        }

        private static PosterPlacement LegacyGrid(int column, int columnSpan, int baseline, int baselineSpan, float offsetX = 0f)
        {
            return PosterLegacyPlacementConverter.FromGrid(
                column,
                columnSpan,
                baseline,
                baselineSpan,
                offsetX);
        }

        private static PosterPlacement FullCanvas()
        {
            return new PosterPlacement
            {
                Mode = PosterPlacementMode.Free,
                Anchor = PosterAnchor.TopLeft,
                Width = 1f,
                Height = 1f
            };
        }

        private static void AddTexture(List<PosterElement> elements, string name, PosterTextureSource source, PosterPlacement placement, PosterFit fit)
        {
            elements.Add(new PosterElement
            {
                Name = name,
                Type = PosterElementType.Texture,
                Placement = placement,
                Texture = new PosterTextureSettings { Source = source, Fit = fit }
            });
        }

        private static void AddRect(List<PosterElement> elements, string name, PosterPlacement placement, PosterColorRef color)
        {
            elements.Add(new PosterElement
            {
                Name = name,
                Type = PosterElementType.Rect,
                Placement = placement,
                Color = color
            });
        }

        private static void AddMainText(List<PosterElement> elements, string name, PosterPlacement placement, float fontSize,
            PosterHorizontalAlign horizontalAlign, PosterVerticalAlign verticalAlign, PosterColorRef color)
        {
            AddTextElement(elements, name, PosterTextSource.MainText, null, placement, fontSize,
                horizontalAlign, verticalAlign, color, PosterRotation.Zero);
        }

        private static void AddText(List<PosterElement> elements, string name, string literal, PosterPlacement placement, float fontSize,
            PosterHorizontalAlign horizontalAlign, PosterVerticalAlign verticalAlign, PosterColorRef color,
            PosterRotation rotation = PosterRotation.Zero)
        {
            AddTextElement(elements, name, PosterTextSource.Literal, literal, placement, fontSize,
                horizontalAlign, verticalAlign, color, rotation);
        }

        private static void AddTextElement(List<PosterElement> elements, string name, PosterTextSource source, string literal,
            PosterPlacement placement, float fontSize, PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign, PosterColorRef color, PosterRotation rotation)
        {
            placement = PosterLegacyPlacementConverter.WithAnchor(
                placement,
                ResolveTextAnchor(horizontalAlign, verticalAlign));
            elements.Add(new PosterElement
            {
                Name = name,
                Type = PosterElementType.Text,
                Placement = placement,
                Rotation = rotation,
                Color = color,
                Text = new PosterTextSettings
                {
                    Source = source,
                    Literal = literal,
                    FontSize = fontSize,
                    HorizontalAlign = horizontalAlign,
                    VerticalAlign = verticalAlign
                }
            });
        }

        private static PosterAnchor ResolveTextAnchor(
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign)
        {
            return (horizontalAlign, verticalAlign) switch
            {
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Top) => PosterAnchor.Top,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Top) => PosterAnchor.TopRight,
                (PosterHorizontalAlign.Left, PosterVerticalAlign.Center) => PosterAnchor.Left,
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Center) => PosterAnchor.Center,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Center) => PosterAnchor.Right,
                (PosterHorizontalAlign.Left, PosterVerticalAlign.Bottom) => PosterAnchor.BottomLeft,
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Bottom) => PosterAnchor.Bottom,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Bottom) => PosterAnchor.BottomRight,
                _ => PosterAnchor.TopLeft
            };
        }
    }
}
