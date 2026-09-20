using qoooo.Rendering.Composition;
using UnityEngine;
using UnityEngine.UI;
using UnitySimpleContainer;

namespace qoooo.Rendering.Sources
{
    public enum PosterFit
    {
        Stretch,
        Cover
    }

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
            Image(texture, x, y, width, height, PosterFit.Stretch);
        }

        public void Image(
            Texture texture,
            float x,
            float y,
            float width,
            float height,
            PosterFit fit)
        {
            if (texture == null) return;

            var image = CreateImage(texture, x, y, width, height);
            image.uvRect = fit == PosterFit.Cover
                ? CalculateCoverUvRect(texture, width, height)
                : new Rect(0f, 0f, 1f, 1f);
        }

        public void Image(
            Texture texture,
            float x,
            float y,
            float width,
            float height,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight)
        {
            if (texture == null) return;

            var image = CreateImage(texture, x, y, width, height);
            image.uvRect = CalculateCropUvRect(
                texture, sourceX, sourceY, sourceWidth, sourceHeight);
        }

        private RawImage CreateImage(
            Texture texture,
            float x,
            float y,
            float width,
            float height)
        {

            var imageObject = new GameObject("Poster Image", typeof(RectTransform), typeof(RawImage));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);

            var image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            return image;
        }

        private static Rect CalculateCoverUvRect(Texture texture, float destinationWidth, float destinationHeight)
        {
            if (destinationWidth <= 0f || destinationHeight <= 0f)
                return new Rect(0f, 0f, 1f, 1f);

            var sourceAspect = texture.width / (float)texture.height;
            var destinationAspect = destinationWidth / destinationHeight;

            if (sourceAspect > destinationAspect)
            {
                var uvWidth = destinationAspect / sourceAspect;
                return new Rect((1f - uvWidth) * 0.5f, 0f, uvWidth, 1f);
            }

            var uvHeight = sourceAspect / destinationAspect;
            return new Rect(0f, (1f - uvHeight) * 0.5f, 1f, uvHeight);
        }

        private static Rect CalculateCropUvRect(
            Texture texture,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight)
        {
            var uvX = sourceX / texture.width;
            var uvWidth = sourceWidth / texture.width;
            var uvHeight = sourceHeight / texture.height;
            var uvY = 1f - (sourceY + sourceHeight) / texture.height;
            return new Rect(uvX, uvY, uvWidth, uvHeight);
        }

        private void CreateVerificationImages()
        {
            if (_verificationImagesCreated) return;

            var texture = (_verificationTextureSource as ITextureContainer)?.Texture;
            if (texture == null) return;

            Image(texture, Width * 0.1f, Height * 0.15f, Width * 0.4f, Height * 0.6f);
            Image(texture, Width * 0.62f, Height * 0.15f, Width * 0.23f, Height * 0.35f, PosterFit.Cover);
            Image(texture, Width * 0.37f, Height * 0.62f, Width * 0.25f, Height * 0.22f,
                texture.width * 0.5f, 0f, texture.width * 0.5f, texture.height);
            _verificationImagesCreated = true;
        }
    }
}
