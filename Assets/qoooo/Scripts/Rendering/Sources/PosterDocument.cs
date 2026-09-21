using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
    public enum PosterElementType
    {
        Text,
        Rect,
        Line,
        Texture
    }

    public enum PosterPlacementMode
    {
        Grid,
        Free
    }

    public enum PosterRotation
    {
        Zero = 0,
        Ninety = 90,
        OneEighty = 180,
        TwoSeventy = 270
    }

    public enum PosterColorRef
    {
        PaletteMain,
        PaletteSub,
        White,
        Black,
        Custom
    }

    public enum PosterTextSource
    {
        Literal,
        MainText,
        CurrentDate,
        CurrentTime
    }

    public enum PosterTextureSource
    {
        Model,
        Background
    }

    [Serializable]
    public sealed class PosterLibrary
    {
        public PosterDocument Editorial;
        public PosterDocument Split;
        public PosterDocument Modular;
        public PosterDocument Custom;

        public PosterDocument GetDocument(int index)
        {
            return index switch
            {
                1 => Split,
                2 => Modular,
                3 => Custom,
                _ => Editorial
            };
        }
    }

    [Serializable]
    public sealed class PosterDocument
    {
        public string Name;
        public List<PosterElement> Elements = new();
    }

    [Serializable]
    public sealed class PosterElement
    {
        public string Name;
        public bool Enabled = true;
        public PosterElementType Type;
        public PosterPlacement Placement = new();
        public PosterRotation Rotation;
        public PosterColorRef Color;
        public Color CustomColor = UnityEngine.Color.white;
        public PosterTextSettings Text = new();
        public PosterTextureSettings Texture = new();
        public PosterLineSettings Line = new();
    }

    [Serializable]
    public sealed class PosterPlacement
    {
        public PosterPlacementMode Mode;
        public int Column;
        public int ColumnSpan = 1;
        public int Baseline;
        public int BaselineSpan = 1;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float OffsetX;
        public float OffsetY;
    }

    [Serializable]
    public sealed class PosterTextSettings
    {
        public PosterTextSource Source;
        public string Literal;
        public string Format;
        public float FontSize = 0.04f;
        public PosterHorizontalAlign HorizontalAlign;
        public PosterVerticalAlign VerticalAlign;
    }

    [Serializable]
    public sealed class PosterTextureSettings
    {
        public PosterTextureSource Source;
        public PosterFit Fit = PosterFit.Stretch;
    }

    [Serializable]
    public sealed class PosterLineSettings
    {
        public bool UsePlacementBounds = true;
        public Vector2 Start;
        public Vector2 End;
        public float Thickness = 1f;
    }
}
