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

    public enum PosterAnchor
    {
        TopLeft = 0,
        Top = 1,
        TopRight = 2,
        Left = 3,
        Center = 4,
        Right = 5,
        BottomLeft = 6,
        Bottom = 7,
        BottomRight = 8
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

    public enum PosterTextMotion
    {
        None,
        MoveX,
        MoveY
    }

    public enum PosterMoveEasing
    {
        Linear,
        SmoothStep,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad
    }

    public enum PosterMotionPattern
    {
        PingPong,
        Traverse
    }

    public enum PosterMotionTiming
    {
        Seconds,
        BeatCycle,
        BeatLeap
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
        public PosterGridDefinition Grid = new();
        public List<PosterElement> Elements = new();
    }

    [Serializable]
    public sealed class PosterGridDefinition
    {
        public int Columns = 4;
        public int Rows = 6;
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
        public PosterAnchor Anchor;
        public int GridPointX;
        public int GridPointY;
        public float X;
        public float Y;
        public float Width;
        public float Height;
    }

    [Serializable]
    // active Poster Textのserializable設定。inactive Legacy Text subsystemの設定ではない。
    public sealed class PosterTextSettings
    {
        public PosterTextSource Source;
        public string Literal;
        public string Format;
        public string FontId;
        public float FontSize = 0.04f;
        public PosterHorizontalAlign HorizontalAlign;
        public PosterVerticalAlign VerticalAlign;
        public PosterTextAnimationSettings Animation = new();
    }

    [Serializable]
    public sealed class PosterTextAnimationSettings
    {
        public bool Enabled;
        public PosterTextMotion Motion;
        public PosterMotionPattern Pattern;
        public float MoveAmount = 0.1f;
        public float MoveSpeed = 1f;
        public PosterMoveEasing MoveEasing = PosterMoveEasing.SmoothStep;
        public bool MoveLoop = true;
        public PosterMotionTiming Timing;
        public float BeatsPerCycle = 1f;
        public float IntervalBeats = 4f;
        public float MoveBeats = 1f;
        public bool LeapEnabled;
        [Range(0f, 1f)] public float LeapProbability = 0.25f;
        public uint LeapSeed;
        public bool TypewriterEnabled;
        public float CharactersPerSecond = 12f;
        public bool TypewriterLoop;
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
