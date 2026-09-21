using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
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
            AddText(elements, "Top Left Meta", "09.26 / SATURDAY", Grid(0, 12, 1, 2, 0.02f), 0.025f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddText(elements, "Top Right Meta", "UNITY / 2026", Grid(0, 12, 1, 2, -0.02f), 0.025f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddMainText(elements, "Hero", Grid(1, 10, 3, 5), 0.225f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddText(elements, "Secondary", "HAPILABO / DJ RØKI", Grid(4, 4, 8, 2), 0.042f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddText(elements, "Bottom Left Meta", "NAEBA / NIIGATA", Grid(0, 12, 20, 2, 0.02f), 0.026f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddText(elements, "Bottom Right Meta", "128 BPM / LIVE", Grid(0, 12, 20, 2, -0.02f), 0.026f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Black);

            AddRect(elements, "Top Meta Band", Grid(0, 12, 1, 2), PosterColorRef.PaletteSub);
            AddRect(elements, "Hero Band", Grid(1, 10, 3, 5), PosterColorRef.PaletteMain);
            AddRect(elements, "Secondary Band", Grid(4, 4, 8, 2), PosterColorRef.PaletteSub);
            AddRect(elements, "Bottom Meta Band", Grid(0, 12, 20, 2), PosterColorRef.PaletteSub);
            AddTexture(elements, "Hero Model", PosterTextureSource.Model, Grid(2, 8, 7, 12), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Editorial", elements);
        }

        public static PosterDocument CreateDefaultSplit()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Hero", Grid(0, 5, 2, 6, 0.04f), 0.15f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Artist", "HAPILABO", Grid(0, 5, 10, 1, 0.04f), 0.052f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Artist", "DJ RØKI", Grid(0, 5, 11, 1, 0.04f), 0.052f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Date", "09.26 / SATURDAY", Grid(0, 5, 14, 1, 0.04f), 0.03f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Location", "NAEBA / NIIGATA", Grid(0, 5, 21, 1, 0.04f), 0.03f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Side Meta", "UNITY", Grid(11, 1, 3, 15), 0.032f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Black, PosterRotation.TwoSeventy);
            AddText(elements, "Bottom Meta", "128 BPM / LIVE", Grid(6, 5, 20, 2), 0.03f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Black);

            AddRect(elements, "Typography Block", Grid(0, 5, 2, 20), PosterColorRef.PaletteMain);
            AddRect(elements, "Side Meta Panel", Grid(11, 1, 3, 15), PosterColorRef.PaletteSub);
            AddRect(elements, "Bottom Meta Panel", Grid(6, 5, 20, 2), PosterColorRef.PaletteSub);
            AddTexture(elements, "Split Model", PosterTextureSource.Model, Grid(6, 6, 2, 20), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Split", elements);
        }

        public static PosterDocument CreateDefaultModular()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Headline", Grid(0, 4, 1, 5, 0.04f), 0.115f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Secondary", "HAPILABO / DJ RØKI", Grid(0, 4, 5, 1, 0.04f), 0.036f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Date", "09.26", Grid(9, 3, 1, 1), 0.03f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Year", "2026", Grid(9, 3, 2, 1), 0.024f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top, PosterColorRef.Black);
            AddText(elements, "Bottom Left Meta", "VISUALS BY QOOOO", Grid(0, 12, 20, 2, 0.02f), 0.026f,
                PosterHorizontalAlign.Left, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddText(elements, "Bottom Right Meta", "128 BPM / LIVE", Grid(0, 12, 20, 2, -0.02f), 0.026f,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Center, PosterColorRef.Black);

            AddRect(elements, "Headline Panel", Grid(0, 4, 1, 5), PosterColorRef.PaletteMain);
            AddRect(elements, "Date Panel", Grid(9, 3, 1, 3), PosterColorRef.PaletteSub);
            AddRect(elements, "Bottom Meta Panel", Grid(0, 12, 20, 2), PosterColorRef.PaletteSub);
            AddTexture(elements, "Large Model", PosterTextureSource.Model, Grid(4, 6, 6, 13), PosterFit.Cover);
            AddTexture(elements, "Small Model Left", PosterTextureSource.Model, Grid(0, 3, 9, 4), PosterFit.Cover);
            AddTexture(elements, "Small Model Right", PosterTextureSource.Model, Grid(9, 3, 3, 3), PosterFit.Cover);
            AddTexture(elements, "Small Model Bottom", PosterTextureSource.Model, Grid(1, 3, 16, 3), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Modular", elements);
        }

        public static PosterDocument CreateDefaultCustom()
        {
            var elements = new List<PosterElement>();
            AddMainText(elements, "Headline", Grid(1, 10, 2, 4), 0.16f,
                PosterHorizontalAlign.Center, PosterVerticalAlign.Center, PosterColorRef.Black);
            AddRect(elements, "Headline Band", Grid(1, 10, 2, 4), PosterColorRef.PaletteMain);
            AddTexture(elements, "Model", PosterTextureSource.Model, Grid(3, 6, 7, 12), PosterFit.Cover);
            AddTexture(elements, "Background", PosterTextureSource.Background, FullCanvas(), PosterFit.Stretch);
            return Document("Custom", elements);
        }

        private static PosterDocument Document(string name, List<PosterElement> elements)
        {
            return new PosterDocument { Name = name, Elements = elements };
        }

        private static PosterPlacement Grid(int column, int columnSpan, int baseline, int baselineSpan, float offsetX = 0f)
        {
            return new PosterPlacement
            {
                Mode = PosterPlacementMode.Grid,
                Column = column,
                ColumnSpan = columnSpan,
                Baseline = baseline,
                BaselineSpan = baselineSpan,
                OffsetX = offsetX
            };
        }

        private static PosterPlacement FullCanvas()
        {
            return new PosterPlacement { Mode = PosterPlacementMode.Free };
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
    }
}
