using System;
using UnityEngine;

namespace Qoooo.VJ
{
    [Serializable]
    public sealed class VjRuntimeSettings
    {
        public const int MinDimension = 16;
        public const int MaxDimension = 8192;

        [Min(MinDimension)] public int outputWidth = 1920;
        [Min(MinDimension)] public int outputHeight = 1080;
        public RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
        public FilterMode filterMode = FilterMode.Bilinear;

        public int ValidatedWidth => Mathf.Clamp(outputWidth, MinDimension, MaxDimension);
        public int ValidatedHeight => Mathf.Clamp(outputHeight, MinDimension, MaxDimension);

        public RenderTextureDescriptor CreateOutputDescriptor()
        {
            return new RenderTextureDescriptor(
                ValidatedWidth,
                ValidatedHeight,
                outputFormat,
                0)
            {
                msaaSamples = 1,
                depthBufferBits = 0,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                useMipMap = false,
                autoGenerateMips = false
            };
        }
    }
}
