using System;
using System.Collections.Generic;
using TMPro;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Foundation.Timing;
using qoooo.Foundation.Utilities;
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
        [SerializeField] private ProcessedTextureSource _modelTextureSource;
        [SerializeField] private ProcessedTextureSource _backgroundTextureSource;
        [SerializeField] private TMP_FontAsset _posterFont;
        [SerializeField] private PosterFontCatalog _fontCatalog;
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
        private IBpmSource _bpmSource;
        private int _builtLayout = -1;
        private int _builtMainText = -1;
        private Vector2Int _builtResolution;
        private Texture _builtModelTexture;
        private Texture _builtBackgroundTexture;
        private Color _builtMain;
        private Color _builtSub;
        private Color _builtBase;
        private Color _builtAccent;
        private bool _hasBuiltPalette;
        private bool _documentDirty = true;
        private bool _mainTextLibraryMigrated;
        private TextMeshProUGUI _textMeasurer;
        private readonly List<PosterTextRuntime> _textRuntimes = new();

        public float Width => _resolutionContainer?.Resolution.x ?? 1920f;
        public float Height => _resolutionContainer?.Resolution.y ?? 1080f;
        public float SizeUnit => Mathf.Min(Width, Height);
        public int ActiveDocumentIndex => _layout.Value;
        public PosterFontCatalog FontCatalog => _fontCatalog;
        public PosterLibrary Library
        {
            get
            {
                EnsureLibrary();
                return _library;
            }
        }

        public PosterDocument ActiveDocument => Library.GetDocument(ActiveDocumentIndex);
        public int ActiveMainTextIndex => _mainText.Value;
        public IReadOnlyList<string> MainTextOptions => _mainText.Options;

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
        public void Construct(
            IResolutionContainer resolutionContainer,
            IPaletteProvider palette,
            IBpmSource bpmSource)
        {
            _resolutionContainer = resolutionContainer;
            _palette = palette;
            _bpmSource = bpmSource;
        }

        private void Update()
        {
            var modelTexture = _modelTextureSource?.Texture;
            var backgroundTexture = _backgroundTextureSource?.Texture;
            var document = _library?.GetDocument(_layout.Value);
            var mainText = _mainText.Value;
            var main = PaletteMain;
            var sub = PaletteSub;
            var baseColor = PaletteBase;
            var accent = PaletteAccent;
            var resolution = new Vector2Int(Mathf.RoundToInt(Width), Mathf.RoundToInt(Height));
            if (document == null) return;

            if (_documentDirty
                || _builtLayout != _layout.Value
                || _builtMainText != mainText
                || _builtResolution != resolution
                || _builtModelTexture != modelTexture
                || _builtBackgroundTexture != backgroundTexture
                || !_hasBuiltPalette
                || _builtMain != main
                || _builtSub != sub
                || _builtBase != baseColor
                || _builtAccent != accent)
            {
                RenderDocument(document, modelTexture, backgroundTexture);
            }

            UpdateTextRuntimes();
        }

        public void MarkDirty()
        {
            _documentDirty = true;
        }

        public void SelectDocument(int index)
        {
            _layout.TrySetValue(index);
        }

        public void SelectMainText(int index)
        {
            _mainText.TrySetValue(index);
        }

        public string MainTextAt(int index)
        {
            EnsureMainTextLibrary();
            return index >= 0 && index < _library.MainTexts.Count
                ? _library.MainTexts[index].Text ?? string.Empty
                : string.Empty;
        }

        public void SetMainText(int index, string text)
        {
            EnsureMainTextLibrary();
            if (index < 0 || index >= _library.MainTexts.Count) return;
            var next = text ?? string.Empty;
            if (_library.MainTexts[index].Text == next) return;
            _library.MainTexts[index].Text = next;
            SynchronizeMainTextOptions(index);
            MarkDirty();
        }

        public void CreateMainText()
        {
            EnsureMainTextLibrary();
            _library.MainTexts.Add(CreateMainTextEntry("New Text"));
            SynchronizeMainTextOptions(_library.MainTexts.Count - 1);
            MarkDirty();
        }

        public void DuplicateMainText()
        {
            EnsureMainTextLibrary();
            var index = Mathf.Clamp(_mainText.Value, 0, _library.MainTexts.Count - 1);
            _library.MainTexts.Insert(index + 1, CreateMainTextEntry(_library.MainTexts[index].Text));
            SynchronizeMainTextOptions(index + 1);
            MarkDirty();
        }

        public void DeleteMainText()
        {
            EnsureMainTextLibrary();
            if (_library.MainTexts.Count <= 1) return;
            var index = Mathf.Clamp(_mainText.Value, 0, _library.MainTexts.Count - 1);
            _library.MainTexts.RemoveAt(index);
            SynchronizeMainTextOptions(Mathf.Min(index, _library.MainTexts.Count - 1));
            MarkDirty();
        }

        public void SynchronizeMainTextLibrary()
        {
            EnsureMainTextLibrary();
            SynchronizeMainTextOptions(_mainText.Value);
            MarkDirty();
        }

        public void ReplaceLibrary(PosterLibrary library)
        {
            _library = library;
            _mainTextLibraryMigrated = false;
            EnsureLibrary();
            SynchronizeParameterOptions();
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

            EnsureMainTextLibrary();
            _library.Documents.RemoveAll(document => document == null);
            if (_library.Documents.Count == 0)
                _library.Documents.Add(new PosterDocument { Name = "Untitled" });

            var usedNames = new HashSet<string>();
            foreach (var document in _library.Documents)
            {
                document.Grid ??= new PosterGridDefinition();
                document.Elements ??= new List<PosterElement>();
                foreach (var element in document.Elements)
                {
                    if (element == null) continue;
                    element.Text ??= new PosterTextSettings();
                    NormalizeTextOffset(element.Text);
                    NormalizeTextAnimations(element.Text);
                    if (string.IsNullOrWhiteSpace(element.Text.FontId))
                        element.Text.FontId = _fontCatalog?.DefaultFontId;
                }
                document.Name = UniqueDocumentName(document.Name, usedNames);
                usedNames.Add(document.Name);
            }
        }

        private void EnsureMainTextLibrary()
        {
            _library ??= new PosterLibrary();
            _library.MainTexts ??= new List<PosterMainTextEntry>();

            if (!_mainTextLibraryMigrated)
            {
                _mainTextLibraryMigrated = true;
                if (IsFixedSlotLibrary(_library.MainTexts))
                    _library.MainTexts.RemoveAll(entry => string.IsNullOrEmpty(entry?.Text));
            }

            _library.MainTexts.RemoveAll(entry => entry == null);
            if (_library.MainTexts.Count == 0)
            {
                _library.MainTexts.Add(CreateMainTextEntry("QOOOO"));
                _library.MainTexts.Add(CreateMainTextEntry("TRIP26"));
                _library.MainTexts.Add(CreateMainTextEntry("LIVE VISUAL"));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < _library.MainTexts.Count; index++)
            {
                var id = _library.MainTexts[index].Id?.Trim();
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    do id = Guid.NewGuid().ToString("N");
                    while (!ids.Add(id));
                }
                _library.MainTexts[index].Id = id;
                _library.MainTexts[index].Text ??= string.Empty;
            }
        }

        private static PosterMainTextEntry CreateMainTextEntry(string text)
        {
            return new PosterMainTextEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = text ?? string.Empty
            };
        }

        private static bool IsFixedSlotLibrary(IReadOnlyList<PosterMainTextEntry> entries)
        {
            if (entries == null || entries.Count != 8) return false;
            for (var index = 0; index < entries.Count; index++)
                if (entries[index]?.Id != $"main-text-{index + 1:00}") return false;
            return true;
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
            _textRuntimes.Clear();

            // listは将来のComposer UI向けにfront-to-back。後から作ったCanvas siblingが前面に描画される。
            for (var index = document.Elements.Count - 1; index >= 0; index--)
            {
                var element = document.Elements[index];
                if (element == null || !element.Enabled) continue;

                RenderElement(document, element, modelTexture, backgroundTexture);
            }

            _builtLayout = _layout.Value;
            _builtMainText = _mainText.Value;
            _builtResolution = new Vector2Int(Mathf.RoundToInt(Width), Mathf.RoundToInt(Height));
            _builtModelTexture = modelTexture;
            _builtBackgroundTexture = backgroundTexture;
            _builtMain = PaletteMain;
            _builtSub = PaletteSub;
            _builtBase = PaletteBase;
            _builtAccent = PaletteAccent;
            _hasBuiltPalette = true;
            _documentDirty = false;
        }

        private void RenderElement(
            PosterDocument document,
            PosterElement element,
            Texture modelTexture,
            Texture backgroundTexture)
        {
            var placement = PosterPlacementResolver.Resolve(
                document.Grid,
                element.Placement,
                element.Rotation,
                Width,
                Height);
            var color = ResolveColor(element.Color, element.CustomColor);
            var rotation = (float)element.Rotation;

            switch (element.Type)
            {
                case PosterElementType.Texture:
                    var texture = element.Texture?.Source == PosterTextureSource.Background
                        ? backgroundTexture
                        : modelTexture;
                    if (texture != null)
                        Image(texture, placement,
                            element.Texture?.Fit ?? PosterFit.Stretch, rotation);
                    break;

                case PosterElementType.Rect:
                    SolidRect(placement, color, rotation);
                    break;

                case PosterElementType.Line:
                    RenderLine(element, placement, color);
                    break;

                case PosterElementType.Text:
                    RenderText(element, placement, color, rotation);
                    break;
            }
        }

        private void RenderLine(PosterElement element, PosterResolvedPlacement placement, Color color)
        {
            var line = element.Line ?? new PosterLineSettings();
            if (line.UsePlacementBounds)
            {
                Line(placement, line.Thickness, color, (float)element.Rotation);
                return;
            }

            Line(
                line.Start.x * Width,
                line.Start.y * Height,
                line.End.x * Width,
                line.End.y * Height,
                line.Thickness,
                color);
        }

        private void RenderText(
            PosterElement element,
            PosterResolvedPlacement placement,
            Color color,
            float rotation)
        {
            var settings = element.Text ?? new PosterTextSettings();
            var text = ResolveText(settings);
            var font = _fontCatalog?.Resolve(settings.FontId, _posterFont) ?? _posterFont;
            var textMesh = Text(text, placement, settings.FontSize * SizeUnit, color,
                settings.HorizontalAlign, settings.VerticalAlign, rotation, font);
            if (textMesh != null)
            {
                AlignGlyphBoundsToPlacement(
                    textMesh,
                    placement,
                    element.Placement?.Anchor ?? PosterAnchor.TopLeft,
                    element.Rotation,
                    settings.TextOffset,
                    new Vector2(Width, Height));
                _textRuntimes.Add(new PosterTextRuntime(
                    settings,
                    textMesh,
                    text,
                    Time.time,
                    (float)(_bpmSource?.Beat ?? 0d)));
            }
        }

        private static void AlignGlyphBoundsToPlacement(
            TextMeshProUGUI textMesh,
            PosterResolvedPlacement placement,
            PosterAnchor anchor,
            PosterRotation rotation,
            Vector2 textOffset,
            Vector2 canvasSize)
        {
            textMesh.maxVisibleCharacters = int.MaxValue;
            textMesh.ForceMeshUpdate();

            var anchorPivot = PosterPlacementResolver.ResolvePivot(anchor);
            var visualSize = rotation is PosterRotation.Ninety or PosterRotation.TwoSeventy
                ? new Vector2(placement.Size.y, placement.Size.x)
                : placement.Size;
            var placementPoint = placement.Position - new Vector2(
                (0.5f - anchorPivot.x) * visualSize.x,
                (anchorPivot.y - 0.5f) * visualSize.y);
            var targetPosition = new Vector2(placementPoint.x, -placementPoint.y);

            var bounds = textMesh.textBounds;
            var rotationTransform = textMesh.rectTransform.localRotation;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            AddRotatedBoundsCorner(bounds.min.x, bounds.min.y, rotationTransform, ref min, ref max);
            AddRotatedBoundsCorner(bounds.min.x, bounds.max.y, rotationTransform, ref min, ref max);
            AddRotatedBoundsCorner(bounds.max.x, bounds.min.y, rotationTransform, ref min, ref max);
            AddRotatedBoundsCorner(bounds.max.x, bounds.max.y, rotationTransform, ref min, ref max);

            var glyphAnchor = new Vector2(
                Mathf.Lerp(min.x, max.x, anchorPivot.x),
                Mathf.Lerp(min.y, max.y, anchorPivot.y));
            var canvasOffset = new Vector2(
                (float.IsFinite(textOffset.x) ? textOffset.x : 0f) * canvasSize.x,
                -(float.IsFinite(textOffset.y) ? textOffset.y : 0f) * canvasSize.y);
            textMesh.rectTransform.anchoredPosition = targetPosition - glyphAnchor + canvasOffset;
        }

        private static void AddRotatedBoundsCorner(
            float x,
            float y,
            Quaternion rotation,
            ref Vector2 min,
            ref Vector2 max)
        {
            var corner = (Vector2)(rotation * new Vector3(x, y));
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }

        private Color ResolveColor(PosterColorRef color, Color customColor)
        {
            return color switch
            {
                PosterColorRef.Main => PaletteMain,
                PosterColorRef.Sub => PaletteSub,
                PosterColorRef.Base => PaletteBase,
                PosterColorRef.Accent => PaletteAccent,
                PosterColorRef.Custom => customColor,
                _ => PaletteMain
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

        private void UpdateTextRuntimes()
        {
            foreach (var runtime in _textRuntimes)
            {
                if (runtime.TextMesh == null) continue;

                if (IsDynamicText(runtime.Settings.Source))
                {
                    var text = ResolveText(runtime.Settings);
                    if (runtime.FullText != text)
                    {
                        runtime.FullText = text;
                        runtime.TextMesh.text = text;
                        runtime.RefreshCenterOffset();
                    }
                }

                var elapsed = Mathf.Max(0f, Time.time - runtime.StartTime);
                var currentBeat = (float)(_bpmSource?.Beat ?? runtime.StartBeat);
                var positionOffset = Vector2.zero;
                var rotationOffset = 0f;
                var scaleMultiplier = 1f;
                var alphaMultiplier = 1f;
                var trackingOffset = 0f;
                var visibleCharacters = runtime.FullText.Length;

                foreach (var animation in runtime.Settings.Animations)
                {
                    if (animation == null || !animation.Enabled) continue;

                    switch (animation.Type)
                    {
                        case PosterTextAnimationType.Move:
                            positionOffset += ResolveMoveOffset(runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Typewriter:
                            visibleCharacters = ResolveVisibleCharacterCount(
                                runtime.FullText.Length, runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Fade:
                            alphaMultiplier *= ResolveFadeMultiplier(runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Scale:
                            scaleMultiplier *= ResolveScaleMultiplier(runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Rotate:
                            rotationOffset += ResolveRotationOffset(runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Shake:
                            ResolveShake(runtime, animation, elapsed, currentBeat,
                                out var shakePosition, out var shakeRotation);
                            positionOffset += shakePosition;
                            rotationOffset += shakeRotation;
                            break;
                        case PosterTextAnimationType.Blink:
                            alphaMultiplier *= ResolveBlinkMultiplier(runtime, animation, elapsed, currentBeat);
                            break;
                        case PosterTextAnimationType.Tracking:
                            trackingOffset += ResolveTrackingOffset(runtime, animation, elapsed, currentBeat);
                            break;
                    }
                }

                var animatedRotation = runtime.BaseRotation * Quaternion.Euler(0f, 0f, -rotationOffset);
                var animatedScale = runtime.BaseScale * scaleMultiplier;
                var baseCenterOffset = runtime.BaseRotation
                                       * Vector3.Scale(runtime.CenterOffset, runtime.BaseScale);
                var animatedCenterOffset = animatedRotation
                                           * Vector3.Scale(runtime.CenterOffset, animatedScale);
                runtime.RectTransform.anchoredPosition = runtime.BasePosition + positionOffset
                    + (Vector2)(baseCenterOffset - animatedCenterOffset);
                runtime.RectTransform.localRotation = animatedRotation;
                runtime.RectTransform.localScale = animatedScale;
                var color = runtime.BaseColor;
                color.a *= Mathf.Clamp01(alphaMultiplier);
                runtime.TextMesh.color = color;
                runtime.TextMesh.characterSpacing = runtime.BaseCharacterSpacing + trackingOffset;
                runtime.TextMesh.maxVisibleCharacters = visibleCharacters;
            }
        }

        private Vector2 ResolveMoveOffset(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var move = animation.Move;
            if (move == null) return Vector2.zero;

            var timing = ResolveTiming(
                animation.Timing,
                elapsed,
                runtime.StartBeat,
                currentBeat,
                move.Speed,
                move.Loop,
                move.Easing);
            if (animation.Timing?.Mode == PosterMotionTiming.BeatLeap
                && (!timing.IsActive || timing.IsCompleted))
                return Vector2.zero;
            var offset = EvaluateValuePattern(move.Pattern, move.Envelope, move.Easing, timing) * move.Amount;
            return move.Axis == PosterTextMotion.MoveX
                ? new Vector2(offset * Width, 0f)
                : new Vector2(0f, offset * Height);
        }

        private static AnimationPhase ResolveTiming(
            PosterAnimationTimingSettings settings,
            float elapsed,
            float startBeat,
            float currentBeat,
            float secondsSpeed,
            bool loop,
            PosterMoveEasing easing)
        {
            settings ??= new PosterAnimationTimingSettings();
            var beat = Mathf.Max(0f, currentBeat);
            var elapsedBeat = Mathf.Max(0f, beat - startBeat);
            switch (settings.Mode)
            {
                case PosterMotionTiming.BeatCycle:
                    var beatCycle = elapsedBeat / Mathf.Max(0.001f, settings.BeatsPerCycle);
                    return CreateCyclePhase(beatCycle, loop);

                case PosterMotionTiming.BeatLeap:
                    var interval = Mathf.Max(0.001f, settings.IntervalBeats);
                    var active = Mathf.Clamp(settings.ActiveBeats, 0.001f, interval);
                    var leapPhase = Mathf.Repeat(Rhythm.GetPhase(
                        beat,
                        interval,
                        active,
                        Linear), 1f);
                    var localBeat = Mathf.Repeat(beat, interval);
                    var isActive = localBeat >= interval - active;
                    var startCycle = Mathf.FloorToInt(Mathf.Max(0f, startBeat) / interval);
                    if (!loop && beat >= (startCycle + 1) * interval)
                        return new AnimationPhase(1f, startCycle, false, true);
                    return new AnimationPhase(
                        leapPhase,
                        Mathf.FloorToInt(beat / interval),
                        isActive,
                        false);

                default:
                    return CreateCyclePhase(elapsed * Mathf.Max(0f, secondsSpeed), loop);
            }
        }

        private static AnimationPhase CreateCyclePhase(float cycle, bool loop)
        {
            cycle = Mathf.Max(0f, cycle);
            var completed = !loop && cycle >= 1f;
            return new AnimationPhase(
                loop ? Mathf.Repeat(cycle, 1f) : Mathf.Clamp01(cycle),
                Mathf.FloorToInt(cycle),
                true,
                completed);
        }

        private static float EvaluateValuePattern(
            PosterValuePattern pattern,
            PosterEnvelopeSettings envelope,
            PosterMoveEasing easing,
            AnimationPhase timing)
        {
            if (timing.IsCompleted)
                return pattern == PosterValuePattern.Traverse ? 1f : 0f;

            var progress = timing.Phase;
            if (pattern == PosterValuePattern.Traverse)
                return Ease(easing, progress);

            if (pattern == PosterValuePattern.PingPong)
            {
                if (progress < 0.5f) return Ease(easing, progress * 2f);
                return 1f - Ease(easing, (progress - 0.5f) * 2f);
            }

            envelope ??= new PosterEnvelopeSettings();
            var rise = Mathf.Max(0f, envelope.Rise);
            var highHold = Mathf.Max(0f, envelope.HighHold);
            var fall = Mathf.Max(0f, envelope.Fall);
            var lowHold = Mathf.Max(0f, envelope.LowHold);
            var total = rise + highHold + fall + lowHold;
            if (total <= 0f)
            {
                rise = fall = 0.1f;
                highHold = lowHold = 0.4f;
                total = 1f;
            }

            var position = progress * total;
            if (position < rise) return rise <= 0f ? 1f : Ease(easing, position / rise);
            position -= rise;
            if (position < highHold) return 1f;
            position -= highHold;
            if (position < fall) return fall <= 0f ? 0f : 1f - Ease(easing, position / fall);
            return 0f;
        }

        private static EasingFn ResolveEasingFn(PosterMoveEasing easing)
        {
            return easing switch
            {
                PosterMoveEasing.SmoothStep => SmoothStep,
                PosterMoveEasing.EaseInSine => Easing.EaseInSine,
                PosterMoveEasing.EaseOutSine => Easing.EaseOutSine,
                PosterMoveEasing.EaseInOutSine => Easing.EaseInOutSine,
                PosterMoveEasing.EaseInQuad => Easing.EaseInQuad,
                PosterMoveEasing.EaseOutQuad => Easing.EaseOutQuad,
                PosterMoveEasing.EaseInOutQuad => Easing.EaseInOutQuad,
                _ => Linear
            };
        }

        private static float Linear(float value) => value;
        private static float SmoothStep(float value) => Mathf.SmoothStep(0f, 1f, value);

        private static float Ease(PosterMoveEasing easing, float progress)
        {
            return easing switch
            {
                PosterMoveEasing.SmoothStep => Mathf.SmoothStep(0f, 1f, progress),
                PosterMoveEasing.EaseInSine => Easing.EaseInSine(progress),
                PosterMoveEasing.EaseOutSine => Easing.EaseOutSine(progress),
                PosterMoveEasing.EaseInOutSine => Easing.EaseInOutSine(progress),
                PosterMoveEasing.EaseInQuad => Easing.EaseInQuad(progress),
                PosterMoveEasing.EaseOutQuad => Easing.EaseOutQuad(progress),
                PosterMoveEasing.EaseInOutQuad => Easing.EaseInOutQuad(progress),
                _ => progress
            };
        }

        private static int ResolveVisibleCharacterCount(
            int characterCount,
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            if (characterCount <= 0) return 0;

            var typewriter = animation.Typewriter;
            if (typewriter == null) return characterCount;

            if (animation.Timing?.Mode == PosterMotionTiming.Seconds)
            {
                var visible = Mathf.FloorToInt(
                    elapsed * Mathf.Max(0.01f, typewriter.CharactersPerSecond));
                return typewriter.Loop
                    ? visible % (characterCount + 1)
                    : Mathf.Min(visible, characterCount);
            }

            var timing = ResolveTiming(
                animation.Timing,
                elapsed,
                runtime.StartBeat,
                currentBeat,
                1f,
                typewriter.Loop,
                typewriter.Easing);
            if (animation.Timing.Mode == PosterMotionTiming.BeatLeap
                && (!timing.IsActive || timing.IsCompleted))
                return characterCount;
            var phase = Ease(typewriter.Easing, timing.Phase);
            return Mathf.Clamp(Mathf.FloorToInt(phase * characterCount), 0, characterCount);
        }

        private static float ResolveFadeMultiplier(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var fade = animation.Fade;
            if (fade == null) return 1f;
            return ResolveTween(
                runtime,
                animation.Timing,
                elapsed,
                currentBeat,
                fade.Speed,
                fade.Loop,
                fade.Easing,
                fade.Pattern,
                fade.Envelope,
                Mathf.Clamp01(fade.FromAlpha),
                Mathf.Clamp01(fade.ToAlpha),
                1f);
        }

        private static float ResolveScaleMultiplier(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var scale = animation.Scale;
            if (scale == null) return 1f;
            return ResolveTween(
                runtime,
                animation.Timing,
                elapsed,
                currentBeat,
                scale.Speed,
                scale.Loop,
                scale.Easing,
                scale.Pattern,
                scale.Envelope,
                scale.FromScale,
                scale.ToScale,
                1f);
        }

        private static float ResolveRotationOffset(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var rotate = animation.Rotate;
            if (rotate == null) return 0f;
            return ResolveTween(
                runtime,
                animation.Timing,
                elapsed,
                currentBeat,
                rotate.Speed,
                rotate.Loop,
                rotate.Easing,
                rotate.Pattern,
                rotate.Envelope,
                rotate.FromDegrees,
                rotate.ToDegrees,
                0f);
        }

        private static float ResolveTween(
            PosterTextRuntime runtime,
            PosterAnimationTimingSettings timingSettings,
            float elapsed,
            float currentBeat,
            float speed,
            bool loop,
            PosterMoveEasing easing,
            PosterValuePattern pattern,
            PosterEnvelopeSettings envelope,
            float from,
            float to,
            float inactiveValue)
        {
            var timing = ResolveTiming(
                timingSettings,
                elapsed,
                runtime.StartBeat,
                currentBeat,
                speed,
                loop,
                easing);
            if (timingSettings?.Mode == PosterMotionTiming.BeatLeap
                && (!timing.IsActive || timing.IsCompleted))
                return inactiveValue;
            var value01 = EvaluateValuePattern(pattern, envelope, easing, timing);
            return Mathf.LerpUnclamped(from, to, value01);
        }

        private void ResolveShake(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat,
            out Vector2 positionOffset,
            out float rotationOffset)
        {
            positionOffset = Vector2.zero;
            rotationOffset = 0f;
            var shake = animation.Shake;
            if (shake == null) return;

            var timing = ResolveTiming(animation.Timing, elapsed, runtime.StartBeat, currentBeat,
                shake.Speed, shake.Loop, shake.Easing);
            if (animation.Timing?.Mode == PosterMotionTiming.BeatLeap
                && (!timing.IsActive || timing.IsCompleted))
                return;

            var amplitude = EvaluateValuePattern(shake.Pattern, shake.Envelope, shake.Easing, timing);
            var frequency = Mathf.Max(0.001f, shake.Frequency);
            var noise = Rhythm.GetLerpNoise3D(
                elapsed,
                1f / frequency,
                0.5f / frequency,
                shake.Seed,
                SmoothStep);
            positionOffset = new Vector2(
                noise.x * shake.PositionAmountX * Width,
                noise.y * shake.PositionAmountY * Height) * amplitude;
            rotationOffset = noise.z * shake.RotationAmount * amplitude;
        }

        private static float ResolveBlinkMultiplier(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var blink = animation.Blink;
            if (blink == null) return 1f;
            var timing = ResolveTiming(animation.Timing, elapsed, runtime.StartBeat, currentBeat,
                blink.Speed, blink.Loop, PosterMoveEasing.Linear);
            if (timing.IsCompleted
                || animation.Timing?.Mode == PosterMotionTiming.BeatLeap && !timing.IsActive)
                return 1f;
            return timing.Phase < Mathf.Clamp01(blink.OnRatio) ? 1f : 0f;
        }

        private static float ResolveTrackingOffset(
            PosterTextRuntime runtime,
            PosterTextAnimationEntry animation,
            float elapsed,
            float currentBeat)
        {
            var tracking = animation.Tracking;
            if (tracking == null) return 0f;
            return ResolveTween(runtime, animation.Timing, elapsed, currentBeat,
                tracking.Speed, tracking.Loop, tracking.Easing,
                tracking.Pattern, tracking.Envelope,
                tracking.FromSpacing, tracking.ToSpacing, 0f);
        }

        internal static void NormalizeTextAnimations(PosterTextSettings settings)
        {
            settings.Animations ??= new List<PosterTextAnimationEntry>();
            if (!settings.AnimationsMigrated)
            {
                var legacy = settings.Animation;
                if (settings.Animations.Count == 0 && legacy != null)
                {
                    if (legacy.Motion != PosterTextMotion.None)
                    {
                        settings.Animations.Add(new PosterTextAnimationEntry
                        {
                            Type = PosterTextAnimationType.Move,
                            Timing = new PosterAnimationTimingSettings
                            {
                                Mode = legacy.Timing,
                                BeatsPerCycle = legacy.BeatsPerCycle,
                                IntervalBeats = legacy.IntervalBeats,
                                ActiveBeats = legacy.MoveBeats
                            },
                            Move = new PosterMoveAnimationSettings
                            {
                                Axis = legacy.Motion,
                                Pattern = legacy.Pattern,
                                Amount = legacy.MoveAmount,
                                Speed = legacy.MoveSpeed,
                                Easing = legacy.MoveEasing,
                                Loop = legacy.MoveLoop
                            }
                        });
                    }

                    if (legacy.TypewriterEnabled)
                    {
                        settings.Animations.Add(new PosterTextAnimationEntry
                        {
                            Type = PosterTextAnimationType.Typewriter,
                            Typewriter = new PosterTypewriterAnimationSettings
                            {
                                CharactersPerSecond = Mathf.Max(0.01f, legacy.CharactersPerSecond),
                                Loop = legacy.TypewriterLoop
                            }
                        });
                    }
                }

                settings.AnimationsMigrated = true;
            }

            foreach (var animation in settings.Animations)
            {
                if (animation == null) continue;
                if (string.IsNullOrWhiteSpace(animation.Id))
                    animation.Id = Guid.NewGuid().ToString("N");
                animation.Timing ??= new PosterAnimationTimingSettings();
                animation.Move ??= new PosterMoveAnimationSettings();
                animation.Move.Envelope ??= new PosterEnvelopeSettings();
                animation.Typewriter ??= new PosterTypewriterAnimationSettings();
                animation.Fade ??= new PosterFadeAnimationSettings();
                animation.Fade.Envelope ??= new PosterEnvelopeSettings();
                animation.Scale ??= new PosterScaleAnimationSettings();
                animation.Scale.Envelope ??= new PosterEnvelopeSettings();
                animation.Rotate ??= new PosterRotateAnimationSettings();
                animation.Rotate.Envelope ??= new PosterEnvelopeSettings();
                animation.Shake ??= new PosterShakeAnimationSettings();
                animation.Shake.Envelope ??= new PosterEnvelopeSettings();
                animation.Blink ??= new PosterBlinkAnimationSettings();
                animation.Tracking ??= new PosterTrackingAnimationSettings();
                animation.Tracking.Envelope ??= new PosterEnvelopeSettings();
            }
        }

        internal static void NormalizeTextOffset(PosterTextSettings settings)
        {
            if (!settings.TextOffsetInitialized)
            {
                var legacyOffset = settings.OpticalOffsetInitialized
                    ? settings.OpticalOffset
                    : new Vector2(0.5f, 0.5f);
                settings.TextOffset = settings.OpticalOffsetUsesSignedRange
                    ? legacyOffset
                    : new Vector2(
                        Mathf.Clamp01(legacyOffset.x) - 0.5f,
                        Mathf.Clamp01(legacyOffset.y) - 0.5f);
                settings.TextOffsetInitialized = true;
            }

            settings.TextOffset = new Vector2(
                float.IsFinite(settings.TextOffset.x) ? settings.TextOffset.x : 0f,
                float.IsFinite(settings.TextOffset.y) ? settings.TextOffset.y : 0f);
        }

        private static bool IsDynamicText(PosterTextSource source)
        {
            return source == PosterTextSource.CurrentDate || source == PosterTextSource.CurrentTime;
        }

        private sealed class PosterTextRuntime
        {
            public readonly PosterTextSettings Settings;
            public readonly TextMeshProUGUI TextMesh;
            public readonly RectTransform RectTransform;
            public readonly Vector2 BasePosition;
            public readonly Quaternion BaseRotation;
            public readonly Vector3 BaseScale;
            public readonly Color BaseColor;
            public Vector3 CenterOffset { get; private set; }
            public readonly float BaseCharacterSpacing;
            public readonly float StartTime;
            public readonly float StartBeat;
            public string FullText;

            public PosterTextRuntime(
                PosterTextSettings settings,
                TextMeshProUGUI textMesh,
                string fullText,
                float startTime,
                float startBeat)
            {
                Settings = settings;
                TextMesh = textMesh;
                RectTransform = textMesh.rectTransform;
                BasePosition = RectTransform.anchoredPosition;
                BaseRotation = RectTransform.localRotation;
                BaseScale = RectTransform.localScale;
                BaseColor = textMesh.color;
                BaseCharacterSpacing = textMesh.characterSpacing;
                StartTime = startTime;
                StartBeat = startBeat;
                FullText = fullText ?? string.Empty;
                RefreshCenterOffset();
            }

            public void RefreshCenterOffset()
            {
                TextMesh.characterSpacing = BaseCharacterSpacing;
                TextMesh.maxVisibleCharacters = int.MaxValue;
                TextMesh.ForceMeshUpdate();
                CenterOffset = TextMesh.textBounds.center;
            }
        }

        private readonly struct AnimationPhase
        {
            public readonly float Phase;
            public readonly int CycleIndex;
            public readonly bool IsActive;
            public readonly bool IsCompleted;

            public AnimationPhase(
                float phase,
                int cycleIndex,
                bool isActive,
                bool isCompleted)
            {
                Phase = phase;
                CycleIndex = cycleIndex;
                IsActive = isActive;
                IsCompleted = isCompleted;
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
            SynchronizeMainTextOptions();
        }

        private void SynchronizeLayoutOptions()
        {
            var names = new List<string>(_library.Documents.Count);
            foreach (var document in _library.Documents) names.Add(document.Name);
            _layout.ReplaceOptions(names);
        }

        private void SynchronizeMainTextOptions(int selectedIndex = -1)
        {
            EnsureMainTextLibrary();
            var index = selectedIndex >= 0 ? selectedIndex : _mainText.Value;
            var labels = new List<string>(_library.MainTexts.Count);
            foreach (var entry in _library.MainTexts)
                labels.Add(string.IsNullOrWhiteSpace(entry.Text) ? "(Empty)" : entry.Text);
            _mainText.ReplaceOptions(labels);
            _mainText.TrySetValue(Mathf.Clamp(index, 0, labels.Count - 1));
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

        private void Image(
            Texture texture,
            PosterResolvedPlacement placement,
            PosterFit fit,
            float rotation)
        {
            if (texture == null) return;

            var imageObject = new GameObject("Poster Image", typeof(RectTransform), typeof(RawImage));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            ConfigurePlacementRect(rectTransform, placement);
            SetRotation(rectTransform, rotation);

            var image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.uvRect = fit == PosterFit.Cover
                ? CalculateCoverUvRect(texture, placement.Size.x, placement.Size.y)
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

        private void SolidRect(PosterResolvedPlacement placement, Color color, float rotation)
        {
            var imageObject = new GameObject("Poster Solid Rect", typeof(RectTransform), typeof(Image));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            ConfigurePlacementRect(rectTransform, placement);
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

        private void Line(
            PosterResolvedPlacement placement,
            float thickness,
            Color color,
            float rotation)
        {
            if (placement.Size.x <= 0f) return;

            var lineObject = new GameObject("Poster Line", typeof(RectTransform), typeof(Image));
            var rectTransform = lineObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = placement.Pivot;
            rectTransform.anchoredPosition = new Vector2(placement.Position.x, -placement.Position.y);
            rectTransform.sizeDelta = new Vector2(placement.Size.x, thickness);
            SetRotation(rectTransform, rotation);

            lineObject.GetComponent<Image>().color = color;
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
            var placement = new PosterResolvedPlacement(
                new Vector2(x, y),
                Vector2.zero,
                new Vector2(GetHorizontalPivot(horizontalAlign), GetVerticalPivot(verticalAlign)));
            return Text(text, placement, fontSize, color, horizontalAlign, verticalAlign, rotation, _posterFont);
        }

        private TextMeshProUGUI Text(
            string text,
            PosterResolvedPlacement placement,
            float fontSize,
            Color color,
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign,
            float rotation,
            TMP_FontAsset font)
        {
            if (font == null) return null;

            var textObject = new GameObject("Poster Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textMesh = textObject.GetComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(transform, false);
            ConfigureText(textMesh, text, fontSize, color, font);
            textMesh.horizontalAlignment = GetHorizontalAlignment(horizontalAlign);
            textMesh.verticalAlignment = GetVerticalAlignment(verticalAlign);
            ConfigurePlacementRect(textMesh.rectTransform, placement);
            SetRotation(textMesh.rectTransform, rotation);
            return textMesh;
        }

        public float TextWidth(string text, float fontSize)
        {
            if (_posterFont == null) return 0f;

            var textMeasurer = GetTextMeasurer();
            ConfigureText(textMeasurer, text, fontSize, Color.white, _posterFont);
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

        private static void ConfigurePlacementRect(
            RectTransform rectTransform,
            PosterResolvedPlacement placement)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = placement.Pivot;
            rectTransform.anchoredPosition = new Vector2(placement.Position.x, -placement.Position.y);
            rectTransform.sizeDelta = placement.Size;
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
            Color color,
            TMP_FontAsset font)
        {
            textMesh.font = font;
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

        private static HorizontalAlignmentOptions GetHorizontalAlignment(PosterHorizontalAlign alignment)
        {
            return alignment switch
            {
                PosterHorizontalAlign.Center => HorizontalAlignmentOptions.Center,
                PosterHorizontalAlign.Right => HorizontalAlignmentOptions.Right,
                _ => HorizontalAlignmentOptions.Left
            };
        }

        private static VerticalAlignmentOptions GetVerticalAlignment(PosterVerticalAlign alignment)
        {
            return alignment switch
            {
                PosterVerticalAlign.Center => VerticalAlignmentOptions.Middle,
                PosterVerticalAlign.Bottom => VerticalAlignmentOptions.Bottom,
                _ => VerticalAlignmentOptions.Top
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
            return MainTextAt(_mainText.Value);
        }

        private Color PaletteMain => _palette?.Main ?? new Color(0.9f, 0.22f, 0.16f, 1f);
        private Color PaletteSub => _palette?.Sub ?? new Color(1f, 0.78f, 0.2f, 1f);
        private Color PaletteBase => _palette?.Base ?? Color.black;
        private Color PaletteAccent => _palette?.Accent ?? Color.white;

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
