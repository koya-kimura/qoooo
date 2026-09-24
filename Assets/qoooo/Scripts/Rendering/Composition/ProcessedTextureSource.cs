using System;
using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Composition
{
    // OWNER / 値の所有者: raw source参照とsource単位のTextureEffectParamsを所有する。
    // FLOW / 値の流れ: 1 frameに1回だけraw textureを加工し、同じprocessed textureを全consumerへ返す。
    public sealed class ProcessedTextureSource : MonoBehaviour, ITextureContainer, IMidiParameterContainer
    {
        private const float DefaultPosterizeLevels = 4f;

        [SerializeField] private MonoBehaviour _sourceComponent;
        [SerializeField] private TextureEffect _effect;
        [SerializeField] private TextureEffectParams _effectParams;
        [SerializeField] private string _parameterTargetName;
        [NonSerialized] private TextureEffectParameters _effectParameters;
        private IPaletteProvider _palette;

        private int _renderedFrame = -1;
        private Texture _processedTexture;

        public Texture Texture => Render();
        public TextureEffectParams EffectParams => BuildEffectParams();
        public TextureEffectParameters EffectParameters
        {
            get
            {
                EnsureEffectParameters();
                return _effectParameters;
            }
        }

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                EnsureEffectParameters();
                foreach (var parameter in _effectParameters.All)
                    if (parameter != null) yield return parameter;

                if (_sourceComponent is not IMidiParameterSource source) yield break;

                foreach (var parameter in source.MidiParameters)
                    if (parameter != null) yield return parameter;
            }
        }

        [Inject]
        public void Construct(IPaletteProvider palette)
        {
            _palette = palette;
        }

        private void OnValidate()
        {
            _effectParams.MosaicSize = Mathf.Max(1f, _effectParams.MosaicSize);
            _effectParams.PosterizeLevels = _effectParams.PosterizeLevels <= 0f
                ? DefaultPosterizeLevels
                : Mathf.Max(2f, _effectParams.PosterizeLevels);
            _effectParams.Amount = Mathf.Max(0f, _effectParams.Amount);
            _effectParams.Scale = Mathf.Max(1f, _effectParams.Scale);
            _effectParams.Threshold = Mathf.Clamp01(_effectParams.Threshold);
            _effectParams.Strength = Mathf.Max(0f, _effectParams.Strength);
            _effectParams.Probability = Mathf.Clamp01(_effectParams.Probability);

            if (_sourceComponent != null && _sourceComponent is not ITextureContainer)
                Debug.LogError("Source Component must implement ITextureContainer.", this);
        }

        public Texture Render()
        {
            if (_renderedFrame == Time.frameCount) return _processedTexture;

            _renderedFrame = Time.frameCount;
            var sourceTexture = (_sourceComponent as ITextureContainer)?.Texture;
            _processedTexture = _effect != null
                ? _effect.ApplyTexture(sourceTexture, BuildEffectParams())
                : sourceTexture;
            return _processedTexture;
        }

        public Texture Refresh()
        {
            _renderedFrame = -1;
            return Render();
        }

        public void SetEffectParams(TextureEffectParams effectParams)
        {
            effectParams.MosaicSize = Mathf.Max(1f, effectParams.MosaicSize);
            effectParams.TileNum = new Vector2(
                Mathf.Max(1f, effectParams.TileNum.x),
                Mathf.Max(1f, effectParams.TileNum.y));
            effectParams.PosterizeLevels = Mathf.Max(2f, effectParams.PosterizeLevels);
            _effectParams = effectParams;
            _renderedFrame = -1;
        }

        private TextureEffectParams BuildEffectParams()
        {
            EnsureEffectParameters();
            var effectParams = _effectParams;
            _effectParameters.ApplyTo(ref effectParams);
            var palette4Mask = 1 << ((int)TextureColorEffect.Palette4 - 1);
            if (_palette != null
                && (effectParams.ColorEffect == TextureColorEffect.Palette4
                    || (effectParams.ColorEffects & palette4Mask) != 0))
            {
                effectParams.Main = _palette.Main;
                effectParams.Sub = _palette.Sub;
                effectParams.Base = _palette.Base;
                effectParams.Accent = _palette.Accent;
            }
            return effectParams;
        }

        private void EnsureEffectParameters()
        {
            var prefix = $"effect.{DefaultParameterTargetName()}";
            if (_effectParameters != null
                && _effectParameters.UvMode != null
                && _effectParameters.UvMode.Id == $"{prefix}.uv") return;

            _effectParameters = TextureEffectParameters.Create(prefix, _effectParams);
        }

        private string DefaultParameterTargetName()
        {
            if (!string.IsNullOrWhiteSpace(_parameterTargetName))
                return _parameterTargetName.Trim().ToLowerInvariant();

            const string layerSuffix = "Layer";
            var targetName = gameObject.name;
            if (targetName.EndsWith(layerSuffix, System.StringComparison.Ordinal))
                targetName = targetName[..^layerSuffix.Length];
            return targetName.ToLowerInvariant();
        }
    }
}