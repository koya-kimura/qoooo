using System;
using System.Collections.Generic;

namespace qoooo.Rendering.Composition
{
    internal enum TextureEffectStage
    {
        Uv,
        Sampling,
        Color,
        Alpha
    }

    [Flags]
    internal enum TextureEffectUiParameter
    {
        None = 0,
        Amount = 1 << 0,
        Scale = 1 << 1,
        Threshold = 1 << 2,
        Strength = 1 << 3,
        Probability = 1 << 4,
        Seed = 1 << 5,
        Direction = 1 << 6,
        Grid = 1 << 7,
        TilingScale = 1 << 8,
        TilingOffset = 1 << 9,
        PaletteThresholds = 1 << 10,
        Main = 1 << 11,
        Sub = 1 << 12,
        Base = 1 << 13,
        Accent = 1 << 14
    }

    internal readonly struct TextureEffectUiDefinition
    {
        public readonly TextureEffectStage Stage;
        public readonly int Value;
        public readonly string Label;
        public readonly TextureEffectUiParameter Parameters;

        public TextureEffectUiDefinition(
            TextureEffectStage stage,
            int value,
            string label,
            TextureEffectUiParameter parameters = TextureEffectUiParameter.None)
        {
            Stage = stage;
            Value = value;
            Label = label;
            Parameters = parameters;
        }
    }

    internal static class TextureEffectUiMetadata
    {
        private const TextureEffectUiParameter Amount = TextureEffectUiParameter.Amount;
        private const TextureEffectUiParameter Scale = TextureEffectUiParameter.Scale;
        private const TextureEffectUiParameter Threshold = TextureEffectUiParameter.Threshold;
        private const TextureEffectUiParameter Strength = TextureEffectUiParameter.Strength;
        private const TextureEffectUiParameter Probability = TextureEffectUiParameter.Probability;
        private const TextureEffectUiParameter Seed = TextureEffectUiParameter.Seed;
        private const TextureEffectUiParameter Direction = TextureEffectUiParameter.Direction;
        private const TextureEffectUiParameter Grid = TextureEffectUiParameter.Grid;
        private const TextureEffectUiParameter TilingScale = TextureEffectUiParameter.TilingScale;
        private const TextureEffectUiParameter TilingOffset = TextureEffectUiParameter.TilingOffset;
        private const TextureEffectUiParameter PaletteThresholds = TextureEffectUiParameter.PaletteThresholds;
        private const TextureEffectUiParameter Main = TextureEffectUiParameter.Main;
        private const TextureEffectUiParameter Sub = TextureEffectUiParameter.Sub;
        private const TextureEffectUiParameter Base = TextureEffectUiParameter.Base;

        public static readonly IReadOnlyList<TextureEffectUiDefinition> Uv = new[]
        {
            Definition(TextureEffectStage.Uv, TextureUvEffect.Tiling, "Tiling", TilingScale | TilingOffset),
            Definition(TextureEffectStage.Uv, TextureUvEffect.Mirror, "Mirror", TilingScale | TilingOffset),
            Definition(TextureEffectStage.Uv, TextureUvEffect.Kaleidoscope, "Kaleidoscope", Scale | Amount),
            Definition(TextureEffectStage.Uv, TextureUvEffect.Wave, "Wave", Amount | Scale | Direction),
            Definition(TextureEffectStage.Uv, TextureUvEffect.Mosaic, "Mosaic", Scale),
            Definition(TextureEffectStage.Uv, TextureUvEffect.HorizontalSlice, "Horizontal Slice", Scale | Amount | Probability | Seed),
            Definition(TextureEffectStage.Uv, TextureUvEffect.VerticalSlice, "Vertical Slice", Scale | Amount | Probability | Seed),
            Definition(TextureEffectStage.Uv, TextureUvEffect.BlockOffset, "Block Offset", Grid | Amount | Probability | Seed),
            Definition(TextureEffectStage.Uv, TextureUvEffect.BlockRepeat, "Block Repeat", Grid | Amount | Probability | Seed),
            Definition(TextureEffectStage.Uv, TextureUvEffect.NoiseDisplacement, "Noise Displacement", Scale | Amount | Seed)
        };

        public static readonly IReadOnlyList<TextureEffectUiDefinition> Sampling = new[]
        {
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.ChannelOffset, "Channel Offset", Direction | Amount),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.ChromaticOffset, "Chromatic Offset", Direction | Amount),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.PrintMisregistration, "Print Misregistration", Direction | Amount | Main | Sub),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.BoxBlur, "Box Blur"),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.GaussianBlur, "Gaussian Blur"),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.DirectionalBlur, "Directional Blur", Direction | Amount),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.Bloom, "Bloom", Threshold | Strength),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.SobelEdge, "Sobel Edge"),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.Outline, "Outline", Amount | Main),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.Sharpen, "Sharpen", Strength),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.Echo, "Echo", Direction | Amount | Probability),
            Definition(TextureEffectStage.Sampling, TextureSamplingEffect.LumaSmear, "Luma Smear", Direction | Amount | Threshold | Strength)
        };

        public static readonly IReadOnlyList<TextureEffectUiDefinition> Color = new[]
        {
            Definition(TextureEffectStage.Color, TextureColorEffect.Invert, "Invert"),
            Definition(TextureEffectStage.Color, TextureColorEffect.Posterize, "Posterize", Scale),
            Definition(TextureEffectStage.Color, TextureColorEffect.Threshold, "Threshold", Threshold),
            Definition(TextureEffectStage.Color, TextureColorEffect.SoftThreshold, "Soft Threshold", Threshold | Strength),
            Definition(TextureEffectStage.Color, TextureColorEffect.Duotone, "Duotone", Main | Sub),
            Definition(TextureEffectStage.Color, TextureColorEffect.Tritone, "Tritone", Main | Sub | Base | Threshold),
            Definition(TextureEffectStage.Color, TextureColorEffect.Palette4, "Palette 4", PaletteThresholds),
            Definition(TextureEffectStage.Color, TextureColorEffect.Solarize, "Solarize", Threshold),
            Definition(TextureEffectStage.Color, TextureColorEffect.Contrast, "Contrast", Strength),
            Definition(TextureEffectStage.Color, TextureColorEffect.Saturation, "Saturation", Strength),
            Definition(TextureEffectStage.Color, TextureColorEffect.Grain, "Grain", Strength | Seed),
            Definition(TextureEffectStage.Color, TextureColorEffect.OrderedDitherMono, "Ordered Dither Mono", Scale | Main | Sub),
            Definition(TextureEffectStage.Color, TextureColorEffect.OrderedDitherColor, "Ordered Dither Color", Scale),
            Definition(TextureEffectStage.Color, TextureColorEffect.HalftoneDot, "Halftone Dot", Scale | Amount | Main | Sub),
            Definition(TextureEffectStage.Color, TextureColorEffect.HalftoneLine, "Halftone Line", Scale | Amount | Main | Sub),
            Definition(TextureEffectStage.Color, TextureColorEffect.HalftoneCross, "Halftone Cross", Scale | Amount | Main | Sub)
        };

        public static readonly IReadOnlyList<TextureEffectUiDefinition> Alpha = new[]
        {
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.Threshold, "Threshold", Threshold),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.Noise, "Noise", Strength | Seed),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.NoiseCutout, "Noise Cutout", Threshold | Seed),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.Slice, "Slice", Scale | Probability | Seed),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.BlockSkip, "Block Skip", Grid | Probability | Seed),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.OrderedDither, "Ordered Dither", Scale),
            Definition(TextureEffectStage.Alpha, TextureAlphaEffect.HalftoneDot, "Halftone Dot", Scale | Amount)
        };

        public static IReadOnlyList<TextureEffectUiDefinition> ForStage(TextureEffectStage stage)
        {
            return stage switch
            {
                TextureEffectStage.Uv => Uv,
                TextureEffectStage.Sampling => Sampling,
                TextureEffectStage.Color => Color,
                _ => Alpha
            };
        }

        public static TextureEffectUiDefinition? Find(TextureEffectStage stage, int value)
        {
            foreach (var definition in ForStage(stage))
                if (definition.Value == value) return definition;
            return null;
        }

        private static TextureEffectUiDefinition Definition<T>(
            TextureEffectStage stage,
            T value,
            string label,
            TextureEffectUiParameter parameters = TextureEffectUiParameter.None) where T : Enum
        {
            return new TextureEffectUiDefinition(stage, Convert.ToInt32(value), label, parameters);
        }
    }
}