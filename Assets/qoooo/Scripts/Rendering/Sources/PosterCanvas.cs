using qoooo.Rendering.Composition;
using UnityEngine;
using UnityEngine.UI;
using UnitySimpleContainer;

namespace qoooo.Rendering.Sources
{
    public class PosterCanvas : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _verificationTextureSource;

        private IResolutionContainer _resolutionContainer;
        private bool _verificationImagesCreated;

        public float Width => _resolutionContainer?.Resolution.x ?? 1920f;
        public float Height => _resolutionContainer?.Resolution.y ?? 1080f;
        public float SizeUnit => Mathf.Min(Width, Height);

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer)
        {
            _resolutionContainer = resolutionContainer;
        }

        private void Update()
        {
            CreateVerificationImages();
        }

        public void Image(Texture texture, float x, float y, float width, float height)
        {
            if (texture == null) return;

            var imageObject = new GameObject("Poster Image", typeof(RectTransform), typeof(RawImage));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);

            imageObject.GetComponent<RawImage>().texture = texture;
        }

        private void CreateVerificationImages()
        {
            if (_verificationImagesCreated) return;

            var texture = (_verificationTextureSource as ITextureContainer)?.Texture;
            if (texture == null) return;

            Image(texture, Width * 0.1f, Height * 0.15f, Width * 0.4f, Height * 0.6f);
            Image(texture, Width * 0.62f, Height * 0.3f, Width * 0.2f, Height * 0.3f);
            _verificationImagesCreated = true;
        }
    }
}