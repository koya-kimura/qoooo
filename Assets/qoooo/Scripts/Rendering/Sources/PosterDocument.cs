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
        Main,
        Sub,
        Base,
        Accent,
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

    public enum PosterValuePattern
    {
        PingPong = 0,
        Traverse = 1,
        Envelope = 2
    }

    public enum PosterMotionTiming
    {
        Seconds,
        BeatCycle,
        BeatLeap
    }

    public enum PosterTextAnimationType
    {
        Move = 0,
        Typewriter = 1,
        Fade = 2,
        Scale = 3,
        Rotate = 4,
        Shake = 5,
        Blink = 6,
        Tracking = 7,
        CharacterScramble = 8
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
        public List<PosterMainTextEntry> MainTexts = new();

        public PosterDocument GetDocument(int index)
        {
            if (Documents == null || Documents.Count == 0) return null;
            return Documents[Mathf.Clamp(index, 0, Documents.Count - 1)];
        }
    }

    [Serializable]
    public sealed class PosterMainTextEntry
    {
        public string Id;
        public string Text;
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
        public float CharacterSpacing;
        public PosterHorizontalAlign HorizontalAlign;
        public PosterVerticalAlign VerticalAlign;
        public Vector2 TextOffset;
        public bool TextOffsetInitialized;
        public Vector2 OpticalOffset = new(0.5f, 0.5f);
        public bool OpticalOffsetInitialized;
        public bool OpticalOffsetUsesSignedRange;
        public List<PosterTextAnimationEntry> Animations = new();
        public bool AnimationsMigrated;
        public PosterTextAnimationSettings Animation = new();
    }

    [Serializable]
    public sealed class PosterTextAnimationEntry
    {
        public string Id;
        public bool Enabled = true;
        public PosterTextAnimationType Type;
        public PosterAnimationTimingSettings Timing = new();
        public PosterMoveAnimationSettings Move = new();
        public PosterTypewriterAnimationSettings Typewriter = new();
        public PosterFadeAnimationSettings Fade = new();
        public PosterScaleAnimationSettings Scale = new();
        public PosterRotateAnimationSettings Rotate = new();
        public PosterShakeAnimationSettings Shake = new();
        public PosterBlinkAnimationSettings Blink = new();
        public PosterTrackingAnimationSettings Tracking = new();
        public PosterCharacterScrambleAnimationSettings CharacterScramble = new();
    }

    [Serializable]
    public sealed class PosterAnimationTimingSettings
    {
        public PosterMotionTiming Mode;
        public float BeatsPerCycle = 1f;
        public float IntervalBeats = 4f;
        public float ActiveBeats = 1f;
    }

    [Serializable]
    public sealed class PosterEnvelopeSettings
    {
        public float Rise = 0.1f;
        public float HighHold = 0.4f;
        public float Fall = 0.1f;
        public float LowHold = 0.4f;
    }

    [Serializable]
    public sealed class PosterMoveAnimationSettings
    {
        public PosterTextMotion Axis = PosterTextMotion.MoveX;
        public PosterValuePattern Pattern;
        public PosterEnvelopeSettings Envelope = new();
        public float Amount = 0.1f;
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterTypewriterAnimationSettings
    {
        public float CharactersPerSecond = 12f;
        public PosterMoveEasing Easing;
        public bool Loop;
    }

    [Serializable]
    public sealed class PosterFadeAnimationSettings
    {
        public float FromAlpha = 1f;
        public float ToAlpha;
        public PosterValuePattern Pattern = PosterValuePattern.Traverse;
        public PosterEnvelopeSettings Envelope = new();
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterScaleAnimationSettings
    {
        public float FromScale = 1f;
        public float ToScale = 1.2f;
        public PosterValuePattern Pattern = PosterValuePattern.Traverse;
        public PosterEnvelopeSettings Envelope = new();
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterRotateAnimationSettings
    {
        public float FromDegrees;
        public float ToDegrees = 90f;
        public PosterValuePattern Pattern = PosterValuePattern.Traverse;
        public PosterEnvelopeSettings Envelope = new();
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterShakeAnimationSettings
    {
        public float PositionAmountX = 0.01f;
        public float PositionAmountY = 0.01f;
        public float RotationAmount = 2f;
        public float Frequency = 12f;
        public uint Seed;
        public PosterValuePattern Pattern = PosterValuePattern.Envelope;
        public PosterEnvelopeSettings Envelope = new();
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterBlinkAnimationSettings
    {
        [Range(0f, 1f)] public float OnRatio = 0.5f;
        public float Speed = 1f;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterTrackingAnimationSettings
    {
        public float FromSpacing;
        public float ToSpacing = 10f;
        public PosterValuePattern Pattern = PosterValuePattern.Traverse;
        public PosterEnvelopeSettings Envelope = new();
        public float Speed = 1f;
        public PosterMoveEasing Easing = PosterMoveEasing.SmoothStep;
        public bool Loop = true;
    }

    [Serializable]
    public sealed class PosterCharacterScrambleAnimationSettings
    {
        [Min(1)] public int Steps = 16;
        public uint Seed;
    }

    [Serializable]
    public sealed class PosterTextAnimationSettings
    {
        public bool Enabled;
        public PosterTextMotion Motion;
        public PosterValuePattern Pattern;
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
