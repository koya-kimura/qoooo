using qoooo.Textures;
using UnityEngine;
using UnityEngine.UI;

namespace qoooo.Preview
{
    public class PreviewController : MonoBehaviour
    {
        [SerializeField] private TextureCompositor _compositor;
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private AspectRatioFitter _aspectRatioFitter;

        private Texture _currentTexture;

        private void Update()
        {
            var texture = _compositor != null ? _compositor.Texture : null;
            if (_currentTexture == texture) return;

            _currentTexture = texture;
            _rawImage.texture = texture;

            if (texture != null && _aspectRatioFitter != null)
            {
                _aspectRatioFitter.aspectRatio = texture.width / (float)texture.height;
            }
        }

        private void OnValidate()
        {
            if (_rawImage == null) _rawImage = GetComponentInChildren<RawImage>();
            if (_aspectRatioFitter == null) _aspectRatioFitter = GetComponentInChildren<AspectRatioFitter>();
        }
    }
}
