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

    [DefaultExecutionOrder(-9200)]
    // 選択したPosterLibrary documentをCanvas primitiveへ渡すruntime bridge。
    // source of truthはPosterLibrary.Documentsで、list indexはposter.layoutと一致する。
    public partial class PosterCanvas : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private TextureLayer _modelTextureLayer;
        [SerializeField] private MonoBehaviour _backgroundTextureSource;
        [SerializeField] private TMP_FontAsset _posterFont;
        [SerializeField] private PosterLibrary _library = new();
        [SerializeField]
        private RadioParameter _layout = new(
            "poster.layout",
            "Poster Layout",
            new[] { "Editorial", "Split", "Modular", "Custom" });

        [SerializeField]
        private RadioParameter _mainText = new(
            "poster.main-text",
            "Poster Main Text",
            new[] { "QOOOO", "TRIP26", "LIVE VISUAL" });

        private IResolutionContainer _resolutionContainer;
        private IPaletteProvider _palette;
        private int _builtLayout = -1;
        private int _builtMainText = -1;
        private Vector2Int _builtResolution;
        private Texture _builtModelTexture;
        private Texture _builtBackgroundTexture;
        private Color _builtPaletteMain;
        private Color _builtPaletteSub;
        private bool _hasBuiltPalette;
        private bool _documentDirty = true;
        private TextMeshProUGUI _textMeasurer;
        private readonly List<DynamicTextBinding> _dynamicTexts = new();

        public float Width => _resolutionContainer?.Resolution.x ?? 1920f;
        public float Height => _resolutionContainer?.Resolution.y ?? 1080f;
        public float SizeUnit => Mathf.Min(Width, Height);
        public int ActiveDocumentIndex => _layout.Value;
        public PosterLibrary Library
        {
            get
            {
                EnsureLibrary();
                return _library;
            }
        }

        public PosterDocument ActiveDocument => Library.GetDocument(ActiveDocumentIndex);

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _layout;
                yield return _mainText;
            }
        }

        private void Awake()
        {
            EnsureLibrary();
            SynchronizeParameterOptions();
        }

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer, IPaletteProvider palette)
        {
            _resolutionContainer = resolutionContainer;
            _palette = palette;
        }

        private void Update()
        {
            var modelTexture = _modelTextureLayer?.Render();
            var backgroundTexture = (_backgroundTextureSource as ITextureContainer)?.Texture;
            var document = _library?.GetDocument(_layout.Value);
            var mainText = _mainText.Value;
            var paletteMain = PaletteMain;
            var paletteSub = PaletteSub;
            var resolution = new Vector2Int(Mathf.RoundToInt(Width), Mathf.RoundToInt(Height));
            if (document == null) return;

            if (_documentDirty
                || _builtLayout != _layout.Value
                || _builtMainText != mainText
                || _builtResolution != resolution
                || _builtModelTexture != modelTexture
                || _builtBackgroundTexture != backgroundTexture
                || !_hasBuiltPalette
                || _builtPaletteMain != paletteMain
                || _builtPaletteSub != paletteSub)
            {
                RenderDocument(document, modelTexture, backgroundTexture);
            }

            UpdateDynamicText();
        }

        public void MarkDirty()
        {
            _documentDirty = true;
        }

        public void SelectDocument(int index)
        {
            _layout.TrySetValue(index);
        }

        public void ReplaceLibrary(PosterLibrary library)
        {
            _library = library;
            EnsureLibrary();
            SynchronizeLayoutOptions();
            MarkDirty();
        }

        public void SynchronizeLibrary(int selectedIndex = -1)
        {
            EnsureLibrary();
            SynchronizeLayoutOptions();
            if (selectedIndex >= 0) _layout.TrySetValue(selectedIndex);
            MarkDirty();
        }

        private void EnsureLibrary()
        {
            if (_library?.Documents == null || _library.Documents.Count == 0)
                _library = PosterDocumentFactory.CreateDefaultLibrary();

            _library.Documents.RemoveAll(document => document == null);
            if (_library.Documents.Count == 0)
                _library.Documents.Add(new PosterDocument { Name = "Untitled" });

            var usedNames = new HashSet<string>();
            foreach (var document in _library.Documents)
            {
                document.Elements ??= new List<PosterElement>();
                document.Name = UniqueDocumentName(document.Name, usedNames);
                usedNames.Add(document.Name);
            }
        }

        private static string UniqueDocumentName(string name, HashSet<string> usedNames)
        {
            var baseName = string.IsNullOrWhiteSpace(name) ? "Untitled" : name.Trim();
            var candidate = baseName;
            for (var suffix = 2; usedNames.Contains(candidate); suffix++)
                candidate = $"{baseName} {suffix}";
            return candidate;
        }

        private void RenderDocument(PosterDocument document, Texture modelTexture, Texture backgroundTexture)
        {
            GetTextMeasurer();
            ClearLayout();
            _dynamicTexts.Clear();

            // listは将来のComposer UI向けにfront-to-back。後から作ったCanvas siblingが前面に描画される。
            for (var index = document.Elements.Count - 1; index >= 0; index--)
            {
                var element = document.Elements[index];
                if (element == null || !element.Enabled) continue;

                RenderElement(element, modelTexture, backgroundTexture);
            }

            _builtLayout = _layout.Value;
            _builtMainText = _mainText.Value;
            _builtResolution = new Vector2Int(Mathf.RoundToInt(Width), Mathf.RoundToInt(Height));
            _builtModelTexture = modelTexture;
            _builtBackgroundTexture = backgroundTexture;
            _builtPaletteMain = PaletteMain;
            _builtPaletteSub = PaletteSub;
            _hasBuiltPalette = true;
            _documentDirty = false;
        }

        private void RenderElement(PosterElement element, Texture modelTexture, Texture backgroundTexture)
        {
            var placement = ResolvePlacement(element.Placement);
            var color = ResolveColor(element.Color, element.CustomColor);
            var rotation = (float)element.Rotation;

            switch (element.Type)
            {
                case PosterElementType.Texture:
                    var texture = element.Texture?.Source == PosterTextureSource.Background
                        ? backgroundTexture
                        : modelTexture;
                    if (texture != null)
                        Image(texture, placement.x, placement.y, placement.width, placement.height,
                            element.Texture?.Fit ?? PosterFit.Stretch, rotation);
                    break;

                case PosterElementType.Rect:
                    SolidRect(placement.x, placement.y, placement.width, placement.height, color, rotation);
                    break;

                case PosterElementType.Line:
                    RenderLine(element, placement, color);
                    break;

                case PosterElementType.Text:
                    RenderText(element, placement, color, rotation);
                    break;
            }
        }

        private void RenderLine(PosterElement element, Rect placement, Color color)
        {
            var line = element.Line ?? new PosterLineSettings();
            if (line.UsePlacementBounds)
            {
                var isVertical = element.Rotation == PosterRotation.Ninety
                                 || element.Rotation == PosterRotation.TwoSeventy;
                if (isVertical)
                    Line(placement.center.x, placement.y, placement.center.x, placement.yMax, line.Thickness, color);
                else
                    Line(placement.x, placement.center.y, placement.xMax, placement.center.y, line.Thickness, color);
                return;
            }

            Line(line.Start.x, line.Start.y, line.End.x, line.End.y, line.Thickness, color);
        }

        private void RenderText(PosterElement element, Rect placement, Color color, float rotation)
        {
            var settings = element.Text ?? new PosterTextSettings();
            var text = ResolveText(settings);
            var x = settings.HorizontalAlign switch
            {
                PosterHorizontalAlign.Center => placement.center.x,
                PosterHorizontalAlign.Right => placement.xMax,
                _ => placement.x
            };
            var y = settings.VerticalAlign switch
            {
                PosterVerticalAlign.Center => placement.center.y,
                PosterVerticalAlign.Bottom => placement.yMax,
                _ => placement.y
            };

            var textMesh = Text(text, x, y, settings.FontSize * SizeUnit, color,
                settings.HorizontalAlign, settings.VerticalAlign, rotation);
            if (textMesh != null && IsDynamicText(settings.Source))
                _dynamicTexts.Add(new DynamicTextBinding(settings, textMesh));
        }

        private Rect ResolvePlacement(PosterPlacement placement)
        {
            placement ??= new PosterPlacement();
            Rect rect;
            if (placement.Mode == PosterPlacementMode.Grid)
            {
                var column = Mathf.Clamp(placement.Column, 0, PosterGrid.ColumnCount - 1);
                var columnSpan = Mathf.Clamp(placement.ColumnSpan, 1, PosterGrid.ColumnCount - column);
                rect = Grid.Rect(column, columnSpan, Mathf.Max(0, placement.Baseline), Mathf.Max(1, placement.BaselineSpan));
            }
            else
            {
                var width = placement.Width > 0f ? placement.Width : Width - placement.X;
                var height = placement.Height > 0f ? placement.Height : Height - placement.Y;
                rect = new Rect(placement.X, placement.Y, Mathf.Max(0f, width), Mathf.Max(0f, height));
            }

            rect.position += new Vector2(placement.OffsetX * SizeUnit, placement.OffsetY * SizeUnit);
            return rect;
        }

        private Color ResolveColor(PosterColorRef color, Color customColor)
        {
            return color switch
            {
                PosterColorRef.PaletteMain => PaletteMain,
                PosterColorRef.PaletteSub => PaletteSub,
                PosterColorRef.Black => Color.black,
                PosterColorRef.Custom => customColor,
                _ => Color.white
            };
        }

        private string ResolveText(PosterTextSettings settings)
        {
            return settings.Source switch
            {
                PosterTextSource.MainText => GetMainText(),
                PosterTextSource.CurrentDate => System.DateTime.Now.ToString(
                    string.IsNullOrWhiteSpace(settings.Format) ? "yyyy.MM.dd" : settings.Format),
                PosterTextSource.CurrentTime => System.DateTime.Now.ToString(
                    string.IsNullOrWhiteSpace(settings.Format) ? "HH:mm" : settings.Format),
                _ => settings.Literal ?? string.Empty
            };
        }

        private void UpdateDynamicText()
        {
            foreach (var binding in _dynamicTexts)
            {
                if (binding.TextMesh == null) continue;
                var text = ResolveText(binding.Settings);
                if (binding.TextMesh.text != text) binding.TextMesh.text = text;
            }
        }

        private static bool IsDynamicText(PosterTextSource source)
        {
            return source == PosterTextSource.CurrentDate || source == PosterTextSource.CurrentTime;
        }

        private readonly struct DynamicTextBinding
        {
            public readonly PosterTextSettings Settings;
            public readonly TextMeshProUGUI TextMesh;

            public DynamicTextBinding(PosterTextSettings settings, TextMeshProUGUI textMesh)
            {
                Settings = settings;
                TextMesh = textMesh;
            }
        }

        private void SynchronizeParameterOptions()
        {
            _layout ??= new RadioParameter(
                "poster.layout",
                "Poster Layout",
                new[] { "Editorial", "Split", "Modular", "Custom" });
            _mainText ??= new RadioParameter(
                "poster.main-text",
                "Poster Main Text",
                new[] { "QOOOO", "TRIP26", "LIVE VISUAL" });

            SynchronizeLayoutOptions();
            _mainText.ReplaceOptions(new[] { "QOOOO", "TRIP26", "LIVE VISUAL" });
        }

        private void SynchronizeLayoutOptions()
        {
            var names = new List<string>(_library.Documents.Count);
            foreach (var document in _library.Documents) names.Add(document.Name);
            _layout.ReplaceOptions(names);
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

        public TextMeshProUGUI Text(
            string text,
            float x,
            float y,
            float fontSize,
            Color color,
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign)
        {
            return Text(text, x, y, fontSize, color, horizontalAlign, verticalAlign, 0f);
        }

        public TextMeshProUGUI Text(
            string text,
            float x,
            float y,
            float fontSize,
            Color color,
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign,
            float rotation)
        {
            if (_posterFont == null) return null;

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
            return textMesh;
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

        private string GetMainText()
        {
            return _mainText.Value switch
            {
                1 => "TRIP26",
                2 => "LIVE VISUAL",
                _ => "QOOOO"
            };
        }

        private Color PaletteMain => _palette?.Main ?? new Color(0.9f, 0.22f, 0.16f, 1f);
        private Color PaletteSub => _palette?.Sub ?? new Color(1f, 0.78f, 0.2f, 1f);

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
