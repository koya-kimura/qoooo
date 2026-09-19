using System;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    [Serializable]
    public struct EffectParams
    {
        public bool IsInvert;
        public bool IsMosaic;
        public float MosaicSize;
        public bool IsTiling;

        public Vector2 TileNum;
        public Vector2 Offset;

        public bool HasEnabledEffect => IsInvert
                                        || IsMosaic
                                        || IsTiling;
    }

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
            EffectParams effectParams)
        {
            if (texture == null) return null;
            if (_material == null) return texture;
            if (!effectParams.HasEnabledEffect) return texture;

            EnsureTexture(texture.width, texture.height);
            SetUniforms(effectParams);
            Graphics.Blit(texture, _effectTexture, _material);
            return _effectTexture;
        }

        private void SetUniforms(EffectParams effectParams)
        {
            _material.SetInt("_IsInvert", effectParams.IsInvert ? 1 : 0);
            _material.SetInt("_IsMosaic", effectParams.IsMosaic ? 1 : 0);
            _material.SetFloat("_MosaicSize", Mathf.Max(1f, effectParams.MosaicSize));
            _material.SetInt("_IsTiling", effectParams.IsTiling ? 1 : 0);
            _material.SetVector("_TileNum", effectParams.TileNum);
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