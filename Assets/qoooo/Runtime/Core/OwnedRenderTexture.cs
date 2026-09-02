using System;
using UnityEngine;

namespace Qoooo.VJ
{
    /// <summary>
    /// Owns exactly one persistent render texture and recreates it only when its
    /// descriptor changes. Borrowers must never release the returned texture.
    /// </summary>
    public sealed class OwnedRenderTexture : IDisposable
    {
        private readonly string _name;
        private RenderTexture _texture;
        private RenderTextureDescriptor _descriptor;
        private bool _hasDescriptor;

        public OwnedRenderTexture(string name)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "Owned Render Texture" : name;
        }

        public RenderTexture Texture => _texture;

        public RenderTexture GetOrCreate(
            RenderTextureDescriptor descriptor,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            descriptor.width = Mathf.Max(1, descriptor.width);
            descriptor.height = Mathf.Max(1, descriptor.height);
            descriptor.msaaSamples = Mathf.Max(1, descriptor.msaaSamples);

            if (_texture != null && _hasDescriptor && DescriptorsMatch(_descriptor, descriptor))
            {
                if (!_texture.IsCreated()) _texture.Create();
                return _texture;
            }

            Release();
            _descriptor = descriptor;
            _hasDescriptor = true;
            _texture = new RenderTexture(descriptor)
            {
                name = _name,
                filterMode = filterMode,
                wrapMode = wrapMode,
                hideFlags = HideFlags.DontSave
            };
            _texture.Create();
            return _texture;
        }

        public void Release()
        {
            if (_texture == null) return;
            if (_texture.IsCreated()) _texture.Release();

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_texture);
            else
                UnityEngine.Object.DestroyImmediate(_texture);

            _texture = null;
            _hasDescriptor = false;
        }

        public void Dispose() => Release();

        private static bool DescriptorsMatch(
            RenderTextureDescriptor left,
            RenderTextureDescriptor right)
        {
            return left.width == right.width &&
                   left.height == right.height &&
                   left.graphicsFormat == right.graphicsFormat &&
                   left.depthStencilFormat == right.depthStencilFormat &&
                   left.msaaSamples == right.msaaSamples &&
                   left.dimension == right.dimension &&
                   left.volumeDepth == right.volumeDepth &&
                   left.sRGB == right.sRGB &&
                   left.useMipMap == right.useMipMap;
        }
    }
}
