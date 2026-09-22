using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;

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

        private int _renderedFrame = -1;
        private Texture _processedTexture;

        public Texture Texture => Render();
        public TextureEffectParams EffectParams => _effectParams;

        // WHY / この分離の理由: wrapperはParameterを作らず、raw sourceの同一instanceをdiscoveryへ中継する。
        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                if (_sourceComponent is not IMidiParameterSource source) yield break;

                foreach (var parameter in source.MidiParameters)
                    if (parameter != null) yield return parameter;
            }
        }

        private void OnValidate()
        {
            _effectParams.MosaicSize = Mathf.Max(1f, _effectParams.MosaicSize);
            _effectParams.PosterizeLevels = _effectParams.PosterizeLevels <= 0f
                ? DefaultPosterizeLevels
                : Mathf.Max(2f, _effectParams.PosterizeLevels);

            if (_sourceComponent != null && _sourceComponent is not ITextureContainer)
                Debug.LogError("Source Component must implement ITextureContainer.", this);
        }

        public Texture Render()
        {
            if (_renderedFrame == Time.frameCount) return _processedTexture;

            _renderedFrame = Time.frameCount;
            var sourceTexture = (_sourceComponent as ITextureContainer)?.Texture;
            _processedTexture = _effect != null
                ? _effect.ApplyTexture(sourceTexture, _effectParams)
                : sourceTexture;
            return _processedTexture;
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
    }
}