using System;
using System.Collections.Generic;
using qoooo.Parameters.Model;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    public enum TextureUvEffect
    {
        None,
        Tiling,
        Mirror,
        Kaleidoscope,
        Wave,
        Mosaic,
        HorizontalSlice,
        VerticalSlice,
        BlockOffset,
        BlockRepeat,
        NoiseDisplacement
    }

    public enum TextureSamplingEffect
    {
        None,
        ChannelOffset,
        ChromaticOffset,
        PrintMisregistration,
        BoxBlur,
        GaussianBlur,
        DirectionalBlur,
        Bloom,
        SobelEdge,
        Outline,
        Sharpen,
        Echo,
        LumaSmear
    }

    public enum TextureColorEffect
    {
        None,
        Invert,
        Posterize,
        Threshold,
        SoftThreshold,
        Duotone,
        Tritone,
        Palette4,
        Solarize,
        Contrast,
        Saturation,
        Grain,
        OrderedDitherMono,
        OrderedDitherColor,
        HalftoneDot,
        HalftoneLine,
        HalftoneCross
    }

    public enum TextureAlphaEffect
    {
        None,
        Threshold,
        Noise,
        NoiseCutout,
        Slice,
        BlockSkip,
        OrderedDither,
        HalftoneDot
    }

    [Serializable]
    // OWNER / 値の所有者: source単位のTexture処理設定。実行側とは独立してserializeされる。
    public struct TextureEffectParams
    {
        public bool IsInvert;
        public bool IsMosaic;
        public float MosaicSize;
        public bool IsTiling;

        public Vector2 TileNum;
        public Vector2 Offset;
        public bool IsPosterize;
        public float PosterizeLevels;

        public TextureUvEffect UvEffect;
        public TextureSamplingEffect SamplingEffect;
        public TextureColorEffect ColorEffect;
        public TextureAlphaEffect AlphaEffect;
        public float Amount;
        public float Scale;
        public float Threshold;
        public float Strength;
        public float Probability;
        public uint Seed;
        public Vector2 Direction;
        public Vector2 Grid;
        public Vector3 Thresholds;
        public Color ColorA;
        public Color ColorB;
        public Color ColorC;
        public Color ColorD;

        public bool HasEnabledEffect => IsInvert
                                        || IsMosaic
                                        || IsTiling
                                        || IsPosterize
                                        || UvEffect != TextureUvEffect.None
                                        || SamplingEffect != TextureSamplingEffect.None
                                        || ColorEffect != TextureColorEffect.None
                                        || AlphaEffect != TextureAlphaEffect.None;
    }

    [Serializable]
    public sealed class TextureEffectParameters
    {
        private static readonly string[] UvOptions = Enum.GetNames(typeof(TextureUvEffect));
        private static readonly string[] SamplingOptions = Enum.GetNames(typeof(TextureSamplingEffect));
        private static readonly string[] ColorOptions = Enum.GetNames(typeof(TextureColorEffect));
        private static readonly string[] AlphaOptions = Enum.GetNames(typeof(TextureAlphaEffect));

        [SerializeField] private RadioParameter _uvMode;
        [SerializeField] private RadioParameter _samplingMode;
        [SerializeField] private RadioParameter _colorMode;
        [SerializeField] private RadioParameter _alphaMode;
        [SerializeField] private FloatParameter _amount;
        [SerializeField] private FloatParameter _scale;
        [SerializeField] private FloatParameter _threshold;
        [SerializeField] private FloatParameter _strength;
        [SerializeField] private FloatParameter _probability;
        [SerializeField] private FloatParameter _seed;

        public RadioParameter UvMode => _uvMode;
        public RadioParameter SamplingMode => _samplingMode;
        public RadioParameter ColorMode => _colorMode;
        public RadioParameter AlphaMode => _alphaMode;
        public FloatParameter Amount => _amount;
        public FloatParameter Scale => _scale;
        public FloatParameter Threshold => _threshold;
        public FloatParameter Strength => _strength;
        public FloatParameter Probability => _probability;
        public FloatParameter Seed => _seed;

        public IEnumerable<IMidiBindableParameter> All
        {
            get
            {
                yield return _uvMode;
                yield return _samplingMode;
                yield return _colorMode;
                yield return _alphaMode;
                yield return _amount;
                yield return _scale;
                yield return _threshold;
                yield return _strength;
                yield return _probability;
                yield return _seed;
            }
        }

        public static TextureEffectParameters Create(string idPrefix, TextureEffectParams defaults)
        {
            return new TextureEffectParameters
            {
                _uvMode = new RadioParameter($"{idPrefix}.uv", "UV Effect", UvOptions, (int)defaults.UvEffect),
                _samplingMode = new RadioParameter($"{idPrefix}.sampling", "Sampling Effect", SamplingOptions, (int)defaults.SamplingEffect),
                _colorMode = new RadioParameter($"{idPrefix}.color", "Color Effect", ColorOptions, (int)defaults.ColorEffect),
                _alphaMode = new RadioParameter($"{idPrefix}.alpha", "Alpha Effect", AlphaOptions, (int)defaults.AlphaEffect),
                _amount = new FloatParameter($"{idPrefix}.amount", "Effect Amount", 0f, 128f, defaults.Amount > 0f ? defaults.Amount : 8f),
                _scale = new FloatParameter($"{idPrefix}.scale", "Effect Scale", 1f, 128f, defaults.Scale > 0f ? defaults.Scale : 8f),
                _threshold = new FloatParameter($"{idPrefix}.threshold", "Effect Threshold", 0f, 1f, defaults.Threshold > 0f ? defaults.Threshold : 0.5f),
                _strength = new FloatParameter($"{idPrefix}.strength", "Effect Strength", 0f, 2f, defaults.Strength > 0f ? defaults.Strength : 1f),
                _probability = new FloatParameter($"{idPrefix}.probability", "Effect Probability", 0f, 1f, defaults.Probability > 0f ? defaults.Probability : 0.5f),
                _seed = new FloatParameter($"{idPrefix}.seed", "Effect Seed", 0f, 1024f, defaults.Seed)
            };
        }

        public void ApplyTo(ref TextureEffectParams effectParams)
        {
            effectParams.UvEffect = (TextureUvEffect)_uvMode.Value;
            effectParams.SamplingEffect = (TextureSamplingEffect)_samplingMode.Value;
            effectParams.ColorEffect = (TextureColorEffect)_colorMode.Value;
            effectParams.AlphaEffect = (TextureAlphaEffect)_alphaMode.Value;
            effectParams.Amount = _amount.Value;
            effectParams.Scale = _scale.Value;
            effectParams.Threshold = _threshold.Value;
            effectParams.Strength = _strength.Value;
            effectParams.Probability = _probability.Value;
            effectParams.Seed = (uint)Mathf.RoundToInt(_seed.Value);
        }
    }

    // Layer共通の処理を適用し、同じ解像度の一時RenderTextureを所有する。
    // DO NOT DUPLICATE / 重複禁止: Model処理をここで複製しない。Posterは処理済みModel Layerの結果を使う。
    public class TextureEffect : MonoBehaviour
    {
        [SerializeField] private Material _material;

        private RenderTexture _effectTexture;

        public void OnDestroy()
        {
            ReleaseTexture();
        }

        public Texture ApplyTexture(
            Texture texture,
            TextureEffectParams effectParams)
        {
            if (texture == null) return null;
            if (_material == null) return texture;
            if (!effectParams.HasEnabledEffect) return texture;

            EnsureTexture(texture.width, texture.height);
            SetUniforms(effectParams);
            Graphics.Blit(texture, _effectTexture, _material);
            return _effectTexture;
        }

        private void SetUniforms(TextureEffectParams effectParams)
        {
            _material.SetInt("_IsInvert", effectParams.IsInvert ? 1 : 0);
            _material.SetInt("_IsMosaic", effectParams.IsMosaic ? 1 : 0);
            _material.SetFloat("_MosaicSize", Mathf.Max(1f, effectParams.MosaicSize));
            _material.SetInt("_IsTiling", effectParams.IsTiling ? 1 : 0);
            // TileNumはserialized互換の名前で保持し、UIではTiling Scaleとして扱う。
            _material.SetVector("_TileNum", new Vector2(
                Mathf.Max(1f, effectParams.TileNum.x),
                Mathf.Max(1f, effectParams.TileNum.y)));
            _material.SetVector("_Offset", effectParams.Offset);
            _material.SetInt("_IsPosterize", effectParams.IsPosterize ? 1 : 0);
            _material.SetFloat("_PosterizeLevels", Mathf.Max(2f, effectParams.PosterizeLevels));
            _material.SetInt("_UvEffect", (int)effectParams.UvEffect);
            _material.SetInt("_SamplingEffect", (int)effectParams.SamplingEffect);
            _material.SetInt("_ColorEffect", (int)effectParams.ColorEffect);
            _material.SetInt("_AlphaEffect", (int)effectParams.AlphaEffect);
            _material.SetFloat("_EffectAmount", Mathf.Max(0f, effectParams.Amount));
            _material.SetFloat("_EffectScale", Mathf.Max(1f, effectParams.Scale));
            _material.SetFloat("_EffectThreshold", Mathf.Clamp01(effectParams.Threshold));
            _material.SetFloat("_EffectStrength", Mathf.Max(0f, effectParams.Strength));
            _material.SetFloat("_EffectProbability", Mathf.Clamp01(effectParams.Probability));
            _material.SetInt("_EffectSeed", unchecked((int)effectParams.Seed));
            _material.SetVector("_EffectDirection", effectParams.Direction.sqrMagnitude > 0f
                ? effectParams.Direction
                : Vector2.right);
            _material.SetVector("_EffectGrid", new Vector2(
                Mathf.Max(1f, effectParams.Grid.x),
                Mathf.Max(1f, effectParams.Grid.y)));
            _material.SetVector("_EffectThresholds", effectParams.Thresholds == Vector3.zero
                ? new Vector3(0.25f, 0.5f, 0.75f)
                : effectParams.Thresholds);
            _material.SetColor("_EffectColorA", effectParams.ColorA == default ? Color.black : effectParams.ColorA);
            _material.SetColor("_EffectColorB", effectParams.ColorB == default ? Color.white : effectParams.ColorB);
            _material.SetColor("_EffectColorC", effectParams.ColorC == default ? Color.gray : effectParams.ColorC);
            _material.SetColor("_EffectColorD", effectParams.ColorD == default ? Color.white : effectParams.ColorD);
            _material.SetVector("_TextureSize", new Vector4(
                _effectTexture.width,
                _effectTexture.height,
                1f / _effectTexture.width,
                1f / _effectTexture.height));
        }

        private void EnsureTexture(int width, int height)
        {
            if (_effectTexture != null
                && _effectTexture.width == width
                && _effectTexture.height == height) return;

            ReleaseTexture();
            _effectTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name} Effect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private void ReleaseTexture()
        {
            if (_effectTexture == null) return;

            _effectTexture.Release();
            Destroy(_effectTexture);
            _effectTexture = null;
        }
    }
}