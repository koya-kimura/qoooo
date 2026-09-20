using TMPro;
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

    public enum PosterHorizontalAlign
    {
        Left,
        Center,
        Right
    }

    public enum PosterVerticalAlign
    {
        Top,
        Center,
        Bottom
    }

    public class PosterCanvas : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _verificationTextureSource;
        [SerializeField] private TMP_FontAsset _posterFont;

        private IResolutionContainer _resolutionContainer;
        private bool _posterLayoutCreated;
        private TextMeshProUGUI _textMeasurer;

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
            CreatePosterLayout();
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

        public void SolidRect(
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            var imageObject = new GameObject("Poster Solid Rect", typeof(RectTransform), typeof(Image));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            ConfigureTopLeftRect(rectTransform, x, y, width, height);

            imageObject.GetComponent<Image>().color = color;
        }

        public void Line(
            float x1,
            float y1,
            float x2,
            float y2,
            float thickness,
            Color color)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length <= 0f) return;

            var imageObject = new GameObject("Poster Line", typeof(RectTransform), typeof(Image));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(
                (x1 + x2) * 0.5f,
                -(y1 + y2) * 0.5f);
            rectTransform.sizeDelta = new Vector2(length, thickness);
            rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                -Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);

            imageObject.GetComponent<Image>().color = color;
        }

        public void Text(
            string text,
            float x,
            float y,
            float fontSize,
            Color color,
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign)
        {
            if (_posterFont == null) return;

            var textObject = new GameObject("Poster Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textMesh = textObject.GetComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(transform, false);
            ConfigureText(textMesh, text, fontSize, color);

            var preferred = textMesh.GetPreferredValues(text ?? string.Empty);
            var pivot = new Vector2(
                GetHorizontalPivot(horizontalAlign),
                GetVerticalPivot(verticalAlign));
            textMesh.rectTransform.anchorMin = new Vector2(0f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0f, 1f);
            textMesh.rectTransform.pivot = pivot;
            textMesh.rectTransform.anchoredPosition = new Vector2(x, -y);
            textMesh.rectTransform.sizeDelta = preferred;
        }

        public float TextWidth(string text, float fontSize)
        {
            if (_posterFont == null) return 0f;

            var textMeasurer = GetTextMeasurer();
            ConfigureText(textMeasurer, text, fontSize, Color.white);
            return textMeasurer.GetPreferredValues(text ?? string.Empty).x;
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
            ConfigureTopLeftRect(rectTransform, x, y, width, height);

            var image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            return image;
        }

        private static void ConfigureTopLeftRect(
            RectTransform rectTransform,
            float x,
            float y,
            float width,
            float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private TextMeshProUGUI GetTextMeasurer()
        {
            if (_textMeasurer != null) return _textMeasurer;

            var measureObject = new GameObject("Poster Text Measurer", typeof(RectTransform), typeof(TextMeshProUGUI));
            measureObject.transform.SetParent(transform, false);
            _textMeasurer = measureObject.GetComponent<TextMeshProUGUI>();
            _textMeasurer.enabled = false;
            return _textMeasurer;
        }

        private void ConfigureText(
            TextMeshProUGUI textMesh,
            string text,
            float fontSize,
            Color color)
        {
            textMesh.font = _posterFont;
            textMesh.text = text ?? string.Empty;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.enableAutoSizing = false;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.richText = false;
            textMesh.raycastTarget = false;
        }

        private static float GetHorizontalPivot(PosterHorizontalAlign alignment)
        {
            return alignment switch
            {
                PosterHorizontalAlign.Center => 0.5f,
                PosterHorizontalAlign.Right => 1f,
                _ => 0f
            };
        }

        private static float GetVerticalPivot(PosterVerticalAlign alignment)
        {
            return alignment switch
            {
                PosterVerticalAlign.Center => 0.5f,
                PosterVerticalAlign.Bottom => 0f,
                _ => 1f
            };
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

        private void CreatePosterLayout()
        {
            if (_posterLayoutCreated) return;

            var texture = (_verificationTextureSource as ITextureContainer)?.Texture;
            if (texture == null) return;

            var width = Width;
            var height = Height;
            var unit = SizeUnit;
            var margin = width * 0.04f;
            var column = (width - margin * 2f) / 12f;

            SolidRect(0f, 0f, width, height, new Color(0.035f, 0.045f, 0.07f, 1f));
            SolidRect(margin + column * 2f, height * 0.28f, column * 8f, height * 0.54f,
                new Color(0.08f, 0.42f, 0.38f, 1f));
            SolidRect(0f, height * 0.87f, width, height * 0.13f,
                new Color(0.9f, 0.22f, 0.16f, 1f));
            SolidRect(margin, height * 0.16f, column * 1.8f, unit * 0.018f,
                new Color(1f, 0.78f, 0.2f, 1f));

            Image(texture, margin + column * 3f, height * 0.3f, column * 5.6f, height * 0.53f,
                PosterFit.Cover);
            Image(texture, margin, height * 0.43f, column * 2.15f, column * 2.15f,
                PosterFit.Cover);
            Image(texture, margin + column * 9.65f, height * 0.36f, column * 1.7f, height * 0.3f,
                texture.width * 0.5f, texture.height * 0.1f,
                texture.width * 0.5f, texture.height * 0.8f);
            Image(texture, margin + column * 0.8f, height * 0.7f, column * 2.7f, height * 0.11f,
                PosterFit.Cover);

            Line(margin, height * 0.24f, width - margin, height * 0.24f,
                unit * 0.004f, Color.white);
            Line(width - margin - column * 0.7f, height * 0.3f,
                width - margin, height * 0.78f, unit * 0.006f,
                new Color(1f, 0.78f, 0.2f, 1f));
            Text("QOOOO", width - margin, height * 0.045f, unit * 0.12f, Color.white,
                PosterHorizontalAlign.Right, PosterVerticalAlign.Top);
            Text("LIVE VISUAL / 026", width * 0.5f, height * 0.185f, unit * 0.025f,
                new Color(1f, 0.78f, 0.2f, 1f),
                PosterHorizontalAlign.Center, PosterVerticalAlign.Top);
            Text("GENERATIVE VISUAL SYSTEM", margin, height * 0.91f, unit * 0.025f,
                new Color(0.035f, 0.045f, 0.07f, 1f),
                PosterHorizontalAlign.Left, PosterVerticalAlign.Top);
            _posterLayoutCreated = true;
        }
    }
}
