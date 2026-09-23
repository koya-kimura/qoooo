using System;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    public enum ModelAnimationType
    {
        Translate,
        Rotate,
        Scale
    }

    public enum ModelAnimationTiming
    {
        ContinuousSeconds,
        ContinuousBeats,
        SecondsCycle,
        BeatCycle
    }

    public enum ModelAnimationPattern
    {
        Traverse,
        PingPong,
        Envelope
    }

    public enum ModelAnimationEasing
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

    [Serializable]
    public sealed class ModelAnimationEntry
    {
        public string Id;
        public bool Enabled = true;
        public ModelAnimationType Type;
        public ModelAnimationTiming Timing;
        public ModelAnimationPattern Pattern = ModelAnimationPattern.PingPong;
        public ModelAnimationEasing Easing = ModelAnimationEasing.SmoothStep;
        public bool Loop = true;
        public float SecondsPerCycle = 2f;
        public float BeatsPerCycle = 4f;
        public Vector3 Amount = Vector3.up;
        public float FromScale = 1f;
        public float ToScale = 1.2f;
        public ModelAnimationEnvelope Envelope = new();
        public uint Seed;
        [Range(0f, 1f)] public float RandomPhase;
    }

    [Serializable]
    public sealed class ModelAnimationEnvelope
    {
        public float Rise = 0.1f;
        public float HighHold = 0.4f;
        public float Fall = 0.1f;
        public float LowHold = 0.4f;
    }
}