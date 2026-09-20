using System.Collections.Generic;
using TMPro;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
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

    public partial class PosterCanvas : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private MonoBehaviour _verificationTextureSource;
        [SerializeField] private TMP_FontAsset _posterFont;
        [SerializeField]
        private RadioParameter _layout = new(
            "poster.layout",
            "Poster Layout",
            new[] { "Editorial", "Split", "Modular", "Custom" });

        [SerializeField]
        private RadioParameter _modelLayout = new(
            "poster.model-layout",
            "Model Texture Layout",
            new[] { "Hero", "Split", "Repeat" });

        [SerializeField]
        private RadioParameter _typographyLayout = new(
            "poster.typography-layout",
            "Typography Layout",
            new[] { "Poster Copy", "Headline", "Labels" });

        private IResolutionContainer _resolutionContainer;
        private int _currentPreset = -1;
        private int _currentModelLayout = -1;
        private int _currentTypographyLayout = -1;
        private int _builtModelLayout = -1;
        private int _builtTypographyLayout = -1;
        private bool _isApplyingPreset;
        private TextMeshProUGUI _textMeasurer;

        public float Width => _resolutionContainer?.Resolution.x ?? 1920f;
        public float Height => _resolutionContainer?.Resolution.y ?? 1080f;
        public float SizeUnit => Mathf.Min(Width, Height);

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _layout;
                yield return _modelLayout;
                yield return _typographyLayout;
            }
        }

        private void Awake()
        {
            SynchronizeParameterOptions();
        }

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer)
        {
            _resolutionContainer = resolutionContainer;
        }

        private void Update()
        {
            SynchronizeLayoutParameters();

            var texture = (_verificationTextureSource as ITextureContainer)?.Texture;
            if (texture == null) return;

            var modelLayout = _modelLayout.Value;
            var typographyLayout = _typographyLayout.Value;
            if (_builtModelLayout == modelLayout
                && _builtTypographyLayout == typographyLayout) return;

            RebuildLayout(texture, modelLayout, typographyLayout);
        }

        private void SynchronizeParameterOptions()
        {
            _layout ??= new RadioParameter(
                "poster.layout",
                "Poster Layout",
                new[] { "Editorial", "Split", "Modular", "Custom" });
            _modelLayout ??= new RadioParameter(
                "poster.model-layout",
                "Model Texture Layout",
                new[] { "Hero", "Split", "Repeat" });
            _typographyLayout ??= new RadioParameter(
                "poster.typography-layout",
                "Typography Layout",
                new[] { "Poster Copy", "Headline", "Labels" });

            _layout.ReplaceOptions(new[] { "Editorial", "Split", "Modular", "Custom" });
            _modelLayout.ReplaceOptions(new[] { "Hero", "Split", "Repeat" });
            _typographyLayout.ReplaceOptions(new[] { "Poster Copy", "Headline", "Labels" });
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
            Image(texture, x, y, width, height, fit, 0f);
        }

        public void Image(
            Texture texture,
            float x,
            float y,
            float width,
            float height,
            PosterFit fit,
            float rotation)
        {
            if (texture == null) return;

            var image = CreateImage(texture, x, y, width, height);
            image.uvRect = fit == PosterFit.Cover
                ? CalculateCoverUvRect(texture, width, height)
                : new Rect(0f, 0f, 1f, 1f);
            SetRotation(image.rectTransform, rotation);
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
            Image(texture, x, y, width, height,
                sourceX, sourceY, sourceWidth, sourceHeight, 0f);
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
            float sourceHeight,
            float rotation)
        {
            if (texture == null) return;

            var image = CreateImage(texture, x, y, width, height);
            image.uvRect = CalculateCropUvRect(
                texture, sourceX, sourceY, sourceWidth, sourceHeight);
            SetRotation(image.rectTransform, rotation);
        }

        public void SolidRect(
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            SolidRect(x, y, width, height, color, 0f);
        }

        public void SolidRect(
            float x,
            float y,
            float width,
            float height,
            Color color,
            float rotation)
        {
            var imageObject = new GameObject("Poster Solid Rect", typeof(RectTransform), typeof(Image));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            ConfigureTopLeftRect(rectTransform, x, y, width, height);
            SetRotation(rectTransform, rotation);

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
            Text(text, x, y, fontSize, color, horizontalAlign, verticalAlign, 0f);
        }

        public void Text(
            string text,
            float x,
            float y,
            float fontSize,
            Color color,
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign,
            float rotation)
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
            SetRotation(textMesh.rectTransform, rotation);
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

        private static void SetRotation(RectTransform rectTransform, float rotation)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rotation);
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

        private void SynchronizeLayoutParameters()
        {
            var preset = _layout.Value;
            if (_currentPreset != preset)
            {
                _currentPreset = preset;
                if (preset < 3)
                {
                    ApplyPreset(preset);
                    _currentModelLayout = _modelLayout.Value;
                    _currentTypographyLayout = _typographyLayout.Value;
                    return;
                }
            }

            var modelLayout = _modelLayout.Value;
            var typographyLayout = _typographyLayout.Value;
            var axisChanged = (_currentModelLayout >= 0 && _currentModelLayout != modelLayout)
                              || (_currentTypographyLayout >= 0
                                  && _currentTypographyLayout != typographyLayout);

            if (axisChanged && !_isApplyingPreset && _layout.Value != 3)
            {
                _layout.TrySetValue(3);
                _currentPreset = 3;
            }

            _currentModelLayout = modelLayout;
            _currentTypographyLayout = typographyLayout;
        }

        private void ApplyPreset(int preset)
        {
            _isApplyingPreset = true;
            _modelLayout.TrySetValue(preset);
            _typographyLayout.TrySetValue(preset);
            _isApplyingPreset = false;
        }

        private void RebuildLayout(Texture texture, int modelLayout, int typographyLayout)
        {
            GetTextMeasurer();
            ClearLayout();

            switch (modelLayout)
            {
                case 1:
                    BuildModelSplit(texture);
                    break;
                case 2:
                    BuildModelRepeat(texture);
                    break;
                default:
                    BuildModelHero(texture);
                    break;
            }

            switch (typographyLayout)
            {
                case 1:
                    BuildTypographyHeadline();
                    break;
                case 2:
                    BuildTypographyLabels();
                    break;
                default:
                    BuildTypographyPosterCopy();
                    break;
            }

            _builtModelLayout = modelLayout;
            _builtTypographyLayout = typographyLayout;
        }

        private void ClearLayout()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (_textMeasurer != null && child == _textMeasurer.transform) continue;

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

    }
}
