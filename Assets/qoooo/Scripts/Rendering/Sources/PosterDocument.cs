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
    // authored posterのsource of truth。poster.layoutはこのordered Documents listをindexする。
    public sealed class PosterLibrary
    {
        public List<PosterDocument> Documents = new();

        public PosterDocument GetDocument(int index)
        {
            if (Documents == null || Documents.Count == 0) return null;
            return Documents[Mathf.Clamp(index, 0, Documents.Count - 1)];
        }
    }

    [Serializable]
    // 1つのauthored poster。このsystemではDocument、Layout、Presetは同じ概念。
    public sealed class PosterDocument
    {
        public string Name;
        public List<PosterElement> Elements = new();
    }

    [Serializable]
    // Document内のprimitive layer。list orderはfront-to-backで、先頭elementが最上面。
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
    // grid座標またはtop-left基準の自由座標でprimitiveを配置するserializable設定。
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
    // active Poster Textのserializable設定。inactive Legacy Text subsystemの設定ではない。
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
    // poster primitiveに既存のModelまたはBackground textureを選択する。
    public sealed class PosterTextureSettings
    {
        public PosterTextureSource Source;
        public PosterFit Fit = PosterFit.Stretch;
    }

    [Serializable]
    // poster line primitiveのserializableな端点とthickness。
    public sealed class PosterLineSettings
    {
        public bool UsePlacementBounds = true;
        public Vector2 Start;
        public Vector2 End;
        public float Thickness = 1f;
    }
}
