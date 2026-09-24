using System;
using System.Collections.Generic;
using qoooo.Parameters.Model;
using UnityEngine;
using UnityEngine.Serialization;

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
        public int UvEffects;
        public int SamplingEffects;
        public int ColorEffects;
        public int AlphaEffects;
        public float Amount;
        public float Scale;
        public float Threshold;
        public float Strength;
        public float Probability;
        public uint Seed;
        public Vector2 Direction;
        public Vector2 Grid;
        public Vector3 Thresholds;
        public Vector4 OutputEffectMixA;
        public Vector4 OutputEffectMixB;
        public float OutputEffectMixC;
        [FormerlySerializedAs("ColorA")] public Color Main;
        [FormerlySerializedAs("ColorB")] public Color Sub;
        [FormerlySerializedAs("ColorC")] public Color Base;
        [FormerlySerializedAs("ColorD")] public Color Accent;

        public bool HasEnabledEffect => IsInvert
                                        || IsMosaic
                                        || IsTiling
                                        || IsPosterize
                                        || UvEffect != TextureUvEffect.None
                                        || SamplingEffect != TextureSamplingEffect.None
                                        || ColorEffect != TextureColorEffect.None
                                        || AlphaEffect != TextureAlphaEffect.None
                                        || UvEffects != 0
                                        || SamplingEffects != 0
                                        || ColorEffects != 0
                                        || AlphaEffects != 0
                                        || OutputEffectMixA != Vector4.zero
                                        || OutputEffectMixB != Vector4.zero
                                        || OutputEffectMixC > 0f;
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

        private BoolParameter[] _uvEffects;
        private BoolParameter[] _samplingEffects;
        private BoolParameter[] _colorEffects;
        private BoolParameter[] _alphaEffects;
        private FloatParameter[] _outputEffectIntensities = Array.Empty<FloatParameter>();

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
        public IReadOnlyList<FloatParameter> OutputEffectIntensities => _outputEffectIntensities;

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
                foreach (var parameter in _uvEffects) yield return parameter;
                foreach (var parameter in _samplingEffects) yield return parameter;
                foreach (var parameter in _colorEffects) yield return parameter;
                foreach (var parameter in _alphaEffects) yield return parameter;
                foreach (var parameter in _outputEffectIntensities) yield return parameter;
            }
        }

        public static TextureEffectParameters Create(string idPrefix, TextureEffectParams defaults)
        {
            var parameters = new TextureEffectParameters
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
                _seed = new FloatParameter($"{idPrefix}.seed", "Effect Seed", 0f, 1024f, defaults.Seed),
                _uvEffects = CreateEffectParameters(idPrefix, "uv", UvOptions),
                _samplingEffects = CreateEffectParameters(idPrefix, "sampling", SamplingOptions),
                _colorEffects = CreateEffectParameters(idPrefix, "color", ColorOptions),
                _alphaEffects = CreateEffectParameters(idPrefix, "alpha", AlphaOptions)
            };
            if (idPrefix == "effect.output")
                parameters._outputEffectIntensities = CreateOutputEffectIntensities(idPrefix);
            return parameters;
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
            effectParams.UvEffects = EnabledMask(_uvEffects);
            effectParams.SamplingEffects = EnabledMask(_samplingEffects);
            effectParams.ColorEffects = EnabledMask(_colorEffects);
            effectParams.AlphaEffects = EnabledMask(_alphaEffects);
            effectParams.OutputEffectMixA = OutputMix(0);
            effectParams.OutputEffectMixB = OutputMix(4);
            effectParams.OutputEffectMixC = _outputEffectIntensities.Length > 8
                ? _outputEffectIntensities[8].Value
                : 0f;
        }

        internal bool IsEffectEnabled(TextureEffectStage stage, int value)
        {
            return GetEffectParameter(stage, value)?.Value == true || LegacyModeValue(stage) == value;
        }

        internal void SetEffectEnabled(TextureEffectStage stage, int value, bool enabled)
        {
            var parameter = GetEffectParameter(stage, value);
            if (parameter == null) return;

            parameter.TrySetValue(enabled);
            if (!enabled && LegacyModeValue(stage) == value) GetLegacyMode(stage).TrySetValue(0);
        }

        private static BoolParameter[] CreateEffectParameters(string idPrefix, string stage, IReadOnlyList<string> options)
        {
            var parameters = new BoolParameter[options.Count];
            for (var index = 1; index < options.Count; index++)
                parameters[index] = new BoolParameter($"{idPrefix}.{stage}.{options[index].ToLowerInvariant()}",
                    $"{stage} {options[index]}");
            return parameters;
        }

        private static FloatParameter[] CreateOutputEffectIntensities(string idPrefix)
        {
            var names = new[]
            {
                "Tiling", "Mosaic", "Block Offset", "Chromatic Offset",
                "Bloom", "Invert", "Posterize", "Grain", "Echo"
            };
            var ids = new[]
            {
                "tiling", "mosaic", "block-offset", "chromatic-offset",
                "bloom", "invert", "posterize", "grain", "echo"
            };
            var parameters = new FloatParameter[names.Length];
            for (var index = 0; index < parameters.Length; index++)
                parameters[index] = new FloatParameter(
                    $"{idPrefix}.intensity.{ids[index]}",
                    $"Output {names[index]}", 0f, 1f);
            return parameters;
        }

        private Vector4 OutputMix(int start)
        {
            if (_outputEffectIntensities.Length < start + 4) return Vector4.zero;
            return new Vector4(
                _outputEffectIntensities[start].Value,
                _outputEffectIntensities[start + 1].Value,
                _outputEffectIntensities[start + 2].Value,
                _outputEffectIntensities[start + 3].Value);
        }

        private static int EnabledMask(IReadOnlyList<BoolParameter> parameters)
        {
            var mask = 0;
            for (var index = 1; index < parameters.Count; index++)
                if (parameters[index].Value) mask |= 1 << (index - 1);
            return mask;
        }

        private BoolParameter GetEffectParameter(TextureEffectStage stage, int value)
        {
            var parameters = stage switch
            {
                TextureEffectStage.Uv => _uvEffects,
                TextureEffectStage.Sampling => _samplingEffects,
                TextureEffectStage.Color => _colorEffects,
                _ => _alphaEffects
            };
            return value > 0 && value < parameters.Length ? parameters[value] : null;
        }

        private int LegacyModeValue(TextureEffectStage stage)
        {
            return GetLegacyMode(stage).Value;
        }

        private RadioParameter GetLegacyMode(TextureEffectStage stage)
        {
            return stage switch
            {
                TextureEffectStage.Uv => _uvMode,
                TextureEffectStage.Sampling => _samplingMode,
                TextureEffectStage.Color => _colorMode,
                _ => _alphaMode
            };
        }
    }

    // Layer共通の処理を適用し、同じ解像度の一時RenderTextureを所有する。
    // DO NOT DUPLICATE / 重複禁止: Model処理をここで複製しない。Posterは処理済みModel Layerの結果を使う。
    public class TextureEffect : MonoBehaviour
    {
        [SerializeField] private Material _material;

        private RenderTexture _effectTexture;
        private RenderTexture _scratchTexture;

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
            var read = texture;
            var write = _effectTexture;

            if (effectParams.IsTiling || effectParams.IsMosaic)
                Apply(ref read, ref write, effectParams, TextureUvEffect.None,
                    TextureSamplingEffect.None, TextureColorEffect.None, TextureAlphaEffect.None, true);

            ApplyStage(ref read, ref write, effectParams, TextureEffectStage.Uv);
            ApplyStage(ref read, ref write, effectParams, TextureEffectStage.Sampling);
            ApplyStage(ref read, ref write, effectParams, TextureEffectStage.Color);

            if (effectParams.IsInvert || effectParams.IsPosterize)
                Apply(ref read, ref write, effectParams, TextureUvEffect.None,
                    TextureSamplingEffect.None, TextureColorEffect.None, TextureAlphaEffect.None, true);

            ApplyStage(ref read, ref write, effectParams, TextureEffectStage.Alpha);
            return read;
        }

        private void ApplyStage(ref Texture read, ref RenderTexture write, TextureEffectParams effectParams, TextureEffectStage stage)
        {
            var effectCount = stage switch
            {
                TextureEffectStage.Uv => Enum.GetValues(typeof(TextureUvEffect)).Length,
                TextureEffectStage.Sampling => Enum.GetValues(typeof(TextureSamplingEffect)).Length,
                TextureEffectStage.Color => Enum.GetValues(typeof(TextureColorEffect)).Length,
                _ => Enum.GetValues(typeof(TextureAlphaEffect)).Length
            };

            for (var value = 1; value < effectCount; value++)
            {
                var mix = EffectMix(effectParams, stage, value);
                if (mix <= 0f) continue;
                Apply(ref read, ref write, effectParams,
                    stage == TextureEffectStage.Uv ? (TextureUvEffect)value : TextureUvEffect.None,
                    stage == TextureEffectStage.Sampling ? (TextureSamplingEffect)value : TextureSamplingEffect.None,
                    stage == TextureEffectStage.Color ? (TextureColorEffect)value : TextureColorEffect.None,
                    stage == TextureEffectStage.Alpha ? (TextureAlphaEffect)value : TextureAlphaEffect.None,
                    false, mix);
            }
        }

        private static float EffectMix(TextureEffectParams effectParams, TextureEffectStage stage, int value)
        {
            var outputMix = OutputEffectMix(effectParams, stage, value);
            if (outputMix >= 0f) return outputMix;
            return IsEffectEnabled(effectParams, stage, value) ? 1f : 0f;
        }

        private static float OutputEffectMix(TextureEffectParams effectParams, TextureEffectStage stage, int value)
        {
            if (stage == TextureEffectStage.Uv && value == (int)TextureUvEffect.Tiling) return effectParams.OutputEffectMixA.x;
            if (stage == TextureEffectStage.Uv && value == (int)TextureUvEffect.Mosaic) return effectParams.OutputEffectMixA.y;
            if (stage == TextureEffectStage.Uv && value == (int)TextureUvEffect.BlockOffset) return effectParams.OutputEffectMixA.z;
            if (stage == TextureEffectStage.Sampling && value == (int)TextureSamplingEffect.ChromaticOffset) return effectParams.OutputEffectMixA.w;
            if (stage == TextureEffectStage.Sampling && value == (int)TextureSamplingEffect.Bloom) return effectParams.OutputEffectMixB.x;
            if (stage == TextureEffectStage.Sampling && value == (int)TextureSamplingEffect.Echo) return effectParams.OutputEffectMixC;
            if (stage == TextureEffectStage.Color && value == (int)TextureColorEffect.Invert) return effectParams.OutputEffectMixB.y;
            if (stage == TextureEffectStage.Color && value == (int)TextureColorEffect.Posterize) return effectParams.OutputEffectMixB.z;
            if (stage == TextureEffectStage.Color && value == (int)TextureColorEffect.Grain) return effectParams.OutputEffectMixB.w;
            return -1f;
        }

        private static bool IsEffectEnabled(TextureEffectParams effectParams, TextureEffectStage stage, int value)
        {
            var legacyValue = stage switch
            {
                TextureEffectStage.Uv => (int)effectParams.UvEffect,
                TextureEffectStage.Sampling => (int)effectParams.SamplingEffect,
                TextureEffectStage.Color => (int)effectParams.ColorEffect,
                _ => (int)effectParams.AlphaEffect
            };
            var enabledMask = stage switch
            {
                TextureEffectStage.Uv => effectParams.UvEffects,
                TextureEffectStage.Sampling => effectParams.SamplingEffects,
                TextureEffectStage.Color => effectParams.ColorEffects,
                _ => effectParams.AlphaEffects
            };
            return legacyValue == value || (enabledMask & (1 << (value - 1))) != 0;
        }

        private void Apply(
            ref Texture read,
            ref RenderTexture write,
            TextureEffectParams effectParams,
            TextureUvEffect uvEffect,
            TextureSamplingEffect samplingEffect,
            TextureColorEffect colorEffect,
            TextureAlphaEffect alphaEffect,
            bool includeLegacyEffects,
            float mix = 1f)
        {
            SetUniforms(effectParams, uvEffect, samplingEffect, colorEffect, alphaEffect, includeLegacyEffects, mix);
            Graphics.Blit(read, write, _material);
            read = write;
            write = write == _effectTexture ? _scratchTexture : _effectTexture;
        }

        private void SetUniforms(
            TextureEffectParams effectParams,
            TextureUvEffect uvEffect,
            TextureSamplingEffect samplingEffect,
            TextureColorEffect colorEffect,
            TextureAlphaEffect alphaEffect,
            bool includeLegacyEffects,
            float mix)
        {
            mix = Mathf.Clamp01(mix);
            _material.SetInt("_IsInvert", includeLegacyEffects && effectParams.IsInvert ? 1 : 0);
            _material.SetInt("_IsMosaic", includeLegacyEffects && effectParams.IsMosaic ? 1 : 0);
            _material.SetFloat("_MosaicSize", Mathf.Max(1f, effectParams.MosaicSize));
            _material.SetInt("_IsTiling", includeLegacyEffects && effectParams.IsTiling ? 1 : 0);
            // TileNumはserialized互換の名前で保持し、UIではTiling Scaleとして扱う。
            var tileCount = uvEffect == TextureUvEffect.Tiling
                ? Mathf.Lerp(1f, 12f, mix)
                : Mathf.Max(1f, effectParams.TileNum.x);
            _material.SetVector("_TileNum", uvEffect == TextureUvEffect.Tiling
                ? new Vector2(tileCount, tileCount)
                : new Vector2(Mathf.Max(1f, effectParams.TileNum.x), Mathf.Max(1f, effectParams.TileNum.y)));
            _material.SetVector("_Offset", effectParams.Offset);
            _material.SetInt("_IsPosterize", includeLegacyEffects && effectParams.IsPosterize ? 1 : 0);
            _material.SetFloat("_PosterizeLevels", Mathf.Max(2f, effectParams.PosterizeLevels));
            _material.SetInt("_UvEffect", (int)uvEffect);
            _material.SetInt("_SamplingEffect", (int)samplingEffect);
            _material.SetInt("_ColorEffect", (int)colorEffect);
            _material.SetInt("_AlphaEffect", (int)alphaEffect);
                        var amount = samplingEffect == TextureSamplingEffect.Echo
                                ? Mathf.Lerp(0f, 128f, mix)
                                : uvEffect == TextureUvEffect.BlockOffset
                                    || samplingEffect == TextureSamplingEffect.ChromaticOffset
                                        ? Mathf.Lerp(0f, 96f, mix)
                                        : Mathf.Max(0f, effectParams.Amount);
            var scale = uvEffect == TextureUvEffect.Mosaic
                ? Mathf.Lerp(1f, 160f, mix)
                : colorEffect == TextureColorEffect.Posterize
                    ? Mathf.Lerp(32f, 2f, mix)
                    : Mathf.Max(1f, effectParams.Scale);
            var strength = samplingEffect == TextureSamplingEffect.Bloom
                ? Mathf.Lerp(0f, 12f, mix)
                : colorEffect == TextureColorEffect.Grain
                    ? Mathf.Lerp(0f, 2f, mix)
                : Mathf.Max(0f, effectParams.Strength);
            _material.SetFloat("_EffectAmount", amount);
            _material.SetFloat("_EffectScale", scale);
            _material.SetFloat("_EffectThreshold", samplingEffect == TextureSamplingEffect.Bloom
                ? Mathf.Lerp(0.8f, 0.05f, mix)
                : Mathf.Clamp01(effectParams.Threshold));
            _material.SetFloat("_EffectStrength", strength);
            var probability = uvEffect == TextureUvEffect.BlockOffset
                ? mix
                : samplingEffect == TextureSamplingEffect.Echo
                    ? Mathf.Lerp(0.1f, 0.9f, mix)
                    : Mathf.Clamp01(effectParams.Probability);
            _material.SetFloat("_EffectProbability", probability);
            _material.SetFloat("_EffectMix", mix);
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
            _material.SetColor("_EffectColorA", effectParams.Main == default ? Color.white : effectParams.Main);
            _material.SetColor("_EffectColorB", effectParams.Sub == default ? Color.gray : effectParams.Sub);
            _material.SetColor("_EffectColorC", effectParams.Base == default ? Color.black : effectParams.Base);
            _material.SetColor("_EffectColorD", effectParams.Accent == default ? Color.white : effectParams.Accent);
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
            _effectTexture.Create();
            _scratchTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name} Effect Scratch",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _scratchTexture.Create();
        }

        private void ReleaseTexture()
        {
            if (_effectTexture == null) return;

            _effectTexture.Release();
            Destroy(_effectTexture);
            _effectTexture = null;
            _scratchTexture.Release();
            Destroy(_scratchTexture);
            _scratchTexture = null;
        }
    }
}