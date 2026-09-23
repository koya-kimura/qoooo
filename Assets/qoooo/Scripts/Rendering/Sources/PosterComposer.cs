using System;
using System.Collections;
using System.Collections.Generic;
using PrefsGUI;
using qoooo.Presentation.Contracts;
using RosettaUI;
using RosettaUI.Reactive;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
    [DefaultExecutionOrder(-9100)]
    // PosterLibrary.Documentsを編集するauthoring UIと、明示的なQoooo Save participant。
    // Prepareでauthored documentをstageし、Commitで受け入れ、Abortで失敗したSaveを処理する。autosaveはしない。
    public sealed class PosterComposer : MonoBehaviour, IUiTarget, IPrefsSaveParticipant
    {
        private const string PrefsKey = "qoooo.poster.library.v3";
        private const string LegacyV2PrefsKey = "qoooo.poster.library.v2";
        private const string LegacyV1PrefsKey = "qoooo.poster.library.v1";
        private const int SchemaVersion = 3;
        private const float LeftPaneWidth = 300f;
        private static readonly Color SelectedColor = new(0.08f, 0.45f, 0.55f, 0.4f);
        private static readonly string[] TypeNames = { "Text", "Rect", "Line", "Texture" };
        private static readonly string[] PlacementNames = { "Grid", "Free" };
        private static readonly string[] AnchorNames =
        {
            "Top Left", "Top", "Top Right",
            "Left", "Center", "Right",
            "Bottom Left", "Bottom", "Bottom Right"
        };
        private static readonly string[] RotationNames = { "0", "90", "180", "270" };
        private static readonly string[] ColorNames = { "Palette Main", "Palette Sub", "White", "Black", "Custom" };
        private static readonly string[] TextSourceNames = { "Literal", "Main Text", "Current Date", "Current Time" };
        private static readonly string[] AnimationTypeNames =
            { "Move", "Typewriter", "Fade", "Scale", "Rotate", "Shake", "Blink", "Tracking" };
        private static readonly string[] MoveAxisNames = { "X", "Y" };
        private static readonly string[] ValuePatternNames = { "Ping Pong", "Traverse", "Envelope" };
        private static readonly string[] MotionTimingNames = { "Seconds", "Beat Cycle", "Beat Leap" };
        private static readonly string[] MoveEasingNames =
        {
            "Linear", "Smooth Step", "Ease In Sine", "Ease Out Sine", "Ease In Out Sine",
            "Ease In Quad", "Ease Out Quad", "Ease In Out Quad"
        };
        private static readonly string[] HorizontalAlignNames = { "Left", "Center", "Right" };
        private static readonly string[] VerticalAlignNames = { "Top", "Center", "Bottom" };
        private static readonly string[] TextureSourceNames = { "Model", "Background" };
        private static readonly string[] FitNames = { "Stretch", "Cover" };

        [SerializeField] private PosterCanvas _posterCanvas;

        private readonly PrefsAny<PosterLibraryRecord> _prefs = new(PrefsKey);
        private readonly PrefsAny<LegacyV2PosterLibraryRecord> _legacyV2Prefs = new(LegacyV2PrefsKey);
        private readonly PrefsAny<LegacyV1PosterLibraryRecord> _legacyV1Prefs = new(LegacyV1PrefsKey);
        private PosterLibraryRecord _recordBeforePrepare;
        private PosterDocument _observedDocument;
        private PosterElement _selectedElement;
        private int _observedOrderSignature;
        private int _animationTypeToAdd;
        private readonly Dictionary<string, bool> _animationFoldStates = new();

        private PosterDocument ActiveDocument => _posterCanvas?.ActiveDocument;

        private void Awake()
        {
            if (_posterCanvas == null)
            {
                Debug.LogError("[Poster] PosterComposer requires a PosterCanvas reference.", this);
                enabled = false;
                return;
            }

            LoadSavedLibrary();
            SynchronizeSelection();
        }

        private void Update()
        {
            var document = ActiveDocument;
            if (!ReferenceEquals(document, _observedDocument))
            {
                _observedDocument = document;
                _selectedElement = FirstElement(document);
                _observedOrderSignature = CalculateOrderSignature(document);
                return;
            }

            var signature = CalculateOrderSignature(document);
            if (signature == _observedOrderSignature) return;

            _observedOrderSignature = signature;
            SynchronizeSelection();
            _posterCanvas.MarkDirty();
        }

        public Element CreateElement()
        {
            var content = UI.Column(
                    CreateHeader(),
                    UI.Row(
                        CreateLayerPane().SetWidth(LeftPaneWidth).SetFlexShrink(0f),
                        UI.ScrollViewVertical(720f,
                                UI.DynamicElementOnStatusChanged(
                                    GetInspectorStatus,
                                    _ => CreateInspector()))
                            .SetMinWidth(520f)
                            .SetFlexGrow(1f)))
                .SetMinWidth(900f)
                .SetMinHeight(620f);

            return content;
        }

        public void PrepareSave()
        {
            if (_posterCanvas == null) return;

            _recordBeforePrepare = Copy(_prefs.Get()) ?? new PosterLibraryRecord();
            _prefs.Set(new PosterLibraryRecord
            {
                SchemaVersion = SchemaVersion,
                HasSavedData = true,
                Library = Copy(_posterCanvas.Library)
            });
        }

        public void CommitSave()
        {
            _recordBeforePrepare = null;
            Debug.Log("[Poster] Library staged for Prefs.Save.", this);
        }

        public void AbortSave()
        {
            if (_recordBeforePrepare == null) return;
            _prefs.Set(_recordBeforePrepare);
            _recordBeforePrepare = null;
        }

        private Element CreateHeader()
        {
            return UI.DynamicElementOnStatusChanged(
                GetLibraryStatus,
                _ => CreateHeaderContent());
        }

        private Element CreateHeaderContent()
        {
            var documents = _posterCanvas.Library.Documents;
            var names = new string[documents.Count];
            for (var index = 0; index < documents.Count; index++) names[index] = documents[index].Name;

            return UI.Column(
                UI.Row(
                    UI.Label("Layout"),
                    UI.Dropdown(null,
                        () => _posterCanvas.ActiveDocumentIndex,
                        index =>
                        {
                            _posterCanvas.SelectDocument(index);
                            SynchronizeSelection();
                        },
                        names),
                    UI.Field("Name", () => ActiveDocument?.Name ?? string.Empty, RenameActiveDocument),
                    UI.Button("New", CreateDocument),
                    UI.Button("Duplicate", DuplicateDocument),
                    UI.Button("Delete", DeleteDocument)),
                UI.Row(
                    UI.Field("Grid Columns", () => ActiveDocument.Grid.Columns,
                        value => SetDocumentGrid(columns: Mathf.Max(1, value))),
                    UI.Field("Grid Rows", () => ActiveDocument.Grid.Rows,
                        value => SetDocumentGrid(rows: Mathf.Max(1, value))),
                    UI.Space(),
                    UI.HelpBox("Use Qoooo > Save to persist poster changes.")));
        }

        private void CreateDocument()
        {
            var documents = _posterCanvas.Library.Documents;
            documents.Add(new PosterDocument { Name = "Untitled", Elements = new List<PosterElement>() });
            _posterCanvas.SynchronizeLibrary(documents.Count - 1);
            SynchronizeSelection();
        }

        private void DuplicateDocument()
        {
            var document = ActiveDocument;
            if (document == null) return;

            var documents = _posterCanvas.Library.Documents;
            var duplicate = Copy(document);
            duplicate.Name = $"{document.Name} Copy";
            RegenerateAnimationIds(duplicate);
            documents.Add(duplicate);
            _posterCanvas.SynchronizeLibrary(documents.Count - 1);
            SynchronizeSelection();
        }

        private void RenameActiveDocument(string name)
        {
            var document = ActiveDocument;
            if (document == null) return;

            document.Name = name;
            _posterCanvas.SynchronizeLibrary(_posterCanvas.ActiveDocumentIndex);
        }

        private void SetDocumentGrid(int? columns = null, int? rows = null)
        {
            var document = ActiveDocument;
            if (document == null) return;
            document.Grid ??= new PosterGridDefinition();
            if (columns.HasValue) document.Grid.Columns = columns.Value;
            if (rows.HasValue) document.Grid.Rows = rows.Value;
            DocumentChanged();
        }

        private void DeleteDocument()
        {
            var documents = _posterCanvas.Library.Documents;
            if (documents.Count <= 1) return;

            var index = _posterCanvas.ActiveDocumentIndex;
            documents.RemoveAt(index);
            _posterCanvas.SynchronizeLibrary(Mathf.Min(index, documents.Count - 1));
            SynchronizeSelection();
        }

        private Element CreateLayerPane()
        {
            var listOption = new ListViewOption(
                reorderable: true,
                fixedSize: true,
                header: false,
                suppressAutoIndent: true)
            {
                createItemElementFunc = CreateLayerRow
            };

            var list = UI.List(null, () => ActiveDocument.Elements, listOption)
                .RegisterValueChangeCallback(DocumentChanged);

            return UI.Column(
                UI.Label("LAYERS  ·  TOP = FRONT"),
                UI.Row(
                    UI.Button("+ Text", () => AddElement(PosterElementType.Text)),
                    UI.Button("+ Rect", () => AddElement(PosterElementType.Rect))),
                UI.Row(
                    UI.Button("+ Line", () => AddElement(PosterElementType.Line)),
                    UI.Button("+ Texture", () => AddElement(PosterElementType.Texture))),
                list.SetFlexGrow(1f));
        }

        private Element CreateLayerRow(IBinder binder, int _)
        {
            PosterElement Element() => binder.GetObject() as PosterElement;

            return UI.DynamicElementOnStatusChanged(
                () => ReferenceEquals(Element(), _selectedElement),
                isSelected => UI.Row(
                        UI.Field(null,
                            () => Element()?.Enabled ?? false,
                            value =>
                            {
                                var element = Element();
                                if (element == null) return;
                                element.Enabled = value;
                                DocumentChanged();
                            }),
                        UI.Button(
                            UI.Label(() =>
                            {
                                var element = Element();
                                return element == null
                                    ? "Missing Layer"
                                    : $"{element.Type,-8}  {DisplayName(element)}";
                            }),
                            () => SelectElement(Element())))
                    .SetBackgroundColor(isSelected ? SelectedColor : null));
        }

        private Element CreateInspector()
        {
            SynchronizeSelection();
            var element = _selectedElement;
            if (element == null)
                return UI.HelpBox("Add a layer to start editing.");

            EnsureSettings(element);
            return UI.Column(
                UI.Label("INSPECTOR"),
                UI.Fold("Layer",
                    UI.Field("Name", () => element.Name, value => Set(element, e => e.Name = value ?? string.Empty)),
                    UI.Field("Enabled", () => element.Enabled, value => Set(element, e => e.Enabled = value)),
                    UI.Dropdown("Type", () => (int)element.Type,
                        value => Set(element, e => e.Type = (PosterElementType)value), TypeNames)).Open(),
                UI.Fold("Placement", CreatePlacementInspector(element)).Open(),
                UI.Fold("Appearance", CreateAppearanceInspector(element)).Open(),
                UI.DynamicElementOnStatusChanged(
                    () => element.Type,
                    _ => CreateTypeInspector(element)),
                UI.Row(
                    UI.Button("Duplicate", DuplicateSelected),
                    UI.Button("Delete", DeleteSelected)));
        }

        private Element CreatePlacementInspector(PosterElement element)
        {
            var placement = element.Placement;
            return UI.Column(
                UI.Dropdown("Mode", () => (int)placement.Mode,
                    value => Set(element, _ => placement.Mode = (PosterPlacementMode)value), PlacementNames),
                UI.Dropdown("Anchor", () => (int)placement.Anchor,
                    value => Set(element, _ => placement.Anchor = (PosterAnchor)value), AnchorNames),
                UI.DynamicElementOnStatusChanged(
                    () => placement.Mode,
                    mode => mode == PosterPlacementMode.Grid
                        ? CreateGridPlacementInspector(element, placement)
                        : CreateFreePlacementInspector(element, placement)));
        }

        private Element CreateGridPlacementInspector(PosterElement element, PosterPlacement placement)
        {
            var grid = ActiveDocument?.Grid ?? new PosterGridDefinition();
            return UI.Column(
                UI.Field("Grid Point X", () => placement.GridPointX,
                    value => Set(element, _ => placement.GridPointX = Mathf.Clamp(value, 0, Mathf.Max(1, grid.Columns)))),
                UI.Field("Grid Point Y", () => placement.GridPointY,
                    value => Set(element, _ => placement.GridPointY = Mathf.Clamp(value, 0, Mathf.Max(1, grid.Rows)))),
                UI.Field("Grid Width (units)", () => placement.Width,
                    value => Set(element, _ => placement.Width = Mathf.Max(0.001f, value))),
                UI.Field("Grid Height (units)", () => placement.Height,
                    value => Set(element, _ => placement.Height = Mathf.Max(0.001f, value))));
        }

        private Element CreateFreePlacementInspector(PosterElement element, PosterPlacement placement)
        {
            return UI.Column(
                UI.Field("X (0..1)", () => placement.X,
                    value => Set(element, _ => placement.X = Mathf.Clamp01(value))),
                UI.Field("Y (0..1)", () => placement.Y,
                    value => Set(element, _ => placement.Y = Mathf.Clamp01(value))),
                UI.Field("Width (0..1)", () => placement.Width,
                    value => Set(element, _ => placement.Width = Mathf.Clamp01(value))),
                UI.Field("Height (0..1)", () => placement.Height,
                    value => Set(element, _ => placement.Height = Mathf.Clamp01(value))));
        }

        private Element CreateAppearanceInspector(PosterElement element)
        {
            return UI.Column(
                UI.Dropdown("Rotation", () => RotationIndex(element.Rotation),
                    value => Set(element, e => e.Rotation = (PosterRotation)(value * 90)), RotationNames),
                UI.Dropdown("Color", () => (int)element.Color,
                    value => Set(element, e => e.Color = (PosterColorRef)value), ColorNames),
                UI.DynamicElementIf(
                    () => element.Color == PosterColorRef.Custom,
                    () => UI.Field("Custom Color", () => element.CustomColor,
                        value => Set(element, e => e.CustomColor = value))));
        }

        private Element CreateTypeInspector(PosterElement element)
        {
            return element.Type switch
            {
                PosterElementType.Text => UI.Fold("Text", CreateTextInspector(element)).Open(),
                PosterElementType.Line => UI.Fold("Line", CreateLineInspector(element)).Open(),
                PosterElementType.Texture => UI.Fold("Texture", CreateTextureInspector(element)).Open(),
                _ => UI.HelpBox("Rect uses Placement and Appearance settings.")
            };
        }

        private Element CreateTextInspector(PosterElement element)
        {
            var settings = element.Text;
            return UI.Column(
                UI.Dropdown("Source", () => (int)settings.Source,
                    value => Set(element, _ => settings.Source = (PosterTextSource)value), TextSourceNames),
                UI.DynamicElementOnStatusChanged(
                    () => settings.Source,
                    source => source switch
                    {
                        PosterTextSource.Literal => UI.Field("Text", () => settings.Literal,
                            value => Set(element, _ => settings.Literal = value ?? string.Empty)),
                        PosterTextSource.CurrentDate or PosterTextSource.CurrentTime => UI.Field("Format", () => settings.Format,
                            value => Set(element, _ => settings.Format = value ?? string.Empty)),
                        _ => UI.HelpBox("Uses poster.main-text.")
                    }),
                CreateFontInspector(element, settings),
                UI.Field("Font Size", () => settings.FontSize,
                    value => Set(element, _ => settings.FontSize = Mathf.Max(0.001f, value))),
                UI.Dropdown("Horizontal Align", () => (int)settings.HorizontalAlign,
                    value => Set(element, _ => settings.HorizontalAlign = (PosterHorizontalAlign)value), HorizontalAlignNames),
                UI.Dropdown("Vertical Align", () => (int)settings.VerticalAlign,
                    value => Set(element, _ => settings.VerticalAlign = (PosterVerticalAlign)value), VerticalAlignNames),
                UI.Fold("Animation", CreateTextAnimationInspector(element, settings)).Open());
        }

        private Element CreateTextAnimationInspector(PosterElement element, PosterTextSettings settings)
        {
            return UI.Column(
                UI.Row(
                    UI.Dropdown("Add", () => _animationTypeToAdd,
                        value => _animationTypeToAdd = value, AnimationTypeNames),
                    UI.Button("+ Add Animation", () => AddTextAnimation(settings))),
                UI.DynamicElementOnStatusChanged(
                    () => AnimationListStatus(settings),
                    _ => CreateAnimationList(element, settings)));
        }

        private Element CreateAnimationList(PosterElement element, PosterTextSettings settings)
        {
            if (settings.Animations.Count == 0)
                return UI.HelpBox("No animations.");

            var entries = new Element[settings.Animations.Count];
            for (var index = 0; index < settings.Animations.Count; index++)
            {
                var entry = settings.Animations[index];
                var fold = UI.Fold(
                    $"{index + 1}. {AnimationSummary(entry)}",
                    CreateAnimationEntryInspector(element, settings, entry));
                fold.IsOpen = _animationFoldStates.TryGetValue(entry.Id, out var isOpen) && isOpen;
                fold.IsOpenRx.Subscribe(value => _animationFoldStates[entry.Id] = value);
                entries[index] = fold;
            }

            return UI.Column(entries);
        }

        private Element CreateAnimationEntryInspector(
            PosterElement element,
            PosterTextSettings settings,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("Enabled", () => animation.Enabled,
                    value => Set(element, _ => animation.Enabled = value)),
                UI.Dropdown("Type", () => (int)animation.Type,
                    value => Set(element, _ => animation.Type = (PosterTextAnimationType)value), AnimationTypeNames),
                UI.DynamicElementOnStatusChanged(
                    () => animation.Type,
                    type => CreateAnimationTypeInspector(element, animation, type)),
                UI.Row(
                    UI.Button("↑", () => MoveTextAnimation(settings, animation.Id, -1)),
                    UI.Button("↓", () => MoveTextAnimation(settings, animation.Id, 1)),
                    UI.Button("Duplicate", () => DuplicateTextAnimation(settings, animation.Id)),
                    UI.Button("Delete", () => DeleteTextAnimation(settings, animation.Id))));
        }

        private Element CreateAnimationTypeInspector(
            PosterElement element,
            PosterTextAnimationEntry animation,
            PosterTextAnimationType type)
        {
            return type switch
            {
                PosterTextAnimationType.Move => CreateMoveAnimationInspector(element, animation),
                PosterTextAnimationType.Typewriter => CreateTypewriterAnimationInspector(element, animation),
                PosterTextAnimationType.Fade => CreateFadeAnimationInspector(element, animation),
                PosterTextAnimationType.Scale => CreateScaleAnimationInspector(element, animation),
                PosterTextAnimationType.Rotate => CreateRotateAnimationInspector(element, animation),
                PosterTextAnimationType.Shake => CreateShakeAnimationInspector(element, animation),
                PosterTextAnimationType.Blink => CreateBlinkAnimationInspector(element, animation),
                PosterTextAnimationType.Tracking => CreateTrackingAnimationInspector(element, animation),
                _ => UI.HelpBox("Unsupported animation type.")
            };
        }

        private Element CreateMoveAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            var move = animation.Move;
            return UI.Column(
                UI.Dropdown("Axis", () => move.Axis == PosterTextMotion.MoveY ? 1 : 0,
                    value => Set(element, _ => move.Axis = value == 1
                        ? PosterTextMotion.MoveY
                        : PosterTextMotion.MoveX), MoveAxisNames),
                UI.Field("Amount (normalized)", () => move.Amount,
                    value => Set(element, _ => move.Amount = value)),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => move.Loop,
                    value => Set(element, _ => move.Loop = value)));
        }

        private Element CreateTypewriterAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Typewriter.Loop,
                    value => Set(element, _ => animation.Typewriter.Loop = value)));
        }

        private Element CreateFadeAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("From Alpha", () => animation.Fade.FromAlpha,
                    value => Set(element, _ => animation.Fade.FromAlpha = Mathf.Clamp01(value))),
                UI.Field("To Alpha", () => animation.Fade.ToAlpha,
                    value => Set(element, _ => animation.Fade.ToAlpha = Mathf.Clamp01(value))),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Fade.Loop,
                    value => Set(element, _ => animation.Fade.Loop = value)));
        }

        private Element CreateScaleAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("From Scale", () => animation.Scale.FromScale,
                    value => Set(element, _ => animation.Scale.FromScale = Mathf.Max(0f, value))),
                UI.Field("To Scale", () => animation.Scale.ToScale,
                    value => Set(element, _ => animation.Scale.ToScale = Mathf.Max(0f, value))),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Scale.Loop,
                    value => Set(element, _ => animation.Scale.Loop = value)));
        }

        private Element CreateRotateAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("From Degrees", () => animation.Rotate.FromDegrees,
                    value => Set(element, _ => animation.Rotate.FromDegrees = value)),
                UI.Field("To Degrees", () => animation.Rotate.ToDegrees,
                    value => Set(element, _ => animation.Rotate.ToDegrees = value)),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Rotate.Loop,
                    value => Set(element, _ => animation.Rotate.Loop = value)));
        }

        private Element CreateShakeAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            var shake = animation.Shake;
            return UI.Column(
                UI.Field("Position Amount X", () => shake.PositionAmountX,
                    value => Set(element, _ => shake.PositionAmountX = Mathf.Max(0f, value))),
                UI.Field("Position Amount Y", () => shake.PositionAmountY,
                    value => Set(element, _ => shake.PositionAmountY = Mathf.Max(0f, value))),
                UI.Field("Rotation Amount", () => shake.RotationAmount,
                    value => Set(element, _ => shake.RotationAmount = Mathf.Max(0f, value))),
                UI.Field("Frequency", () => shake.Frequency,
                    value => Set(element, _ => shake.Frequency = Mathf.Max(0.001f, value))),
                UI.Field("Seed", () => shake.Seed,
                    value => Set(element, _ => shake.Seed = value)),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => shake.Loop,
                    value => Set(element, _ => shake.Loop = value)));
        }

        private Element CreateBlinkAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("On Ratio", () => animation.Blink.OnRatio,
                    value => Set(element, _ => animation.Blink.OnRatio = Mathf.Clamp01(value))),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Blink.Loop,
                    value => Set(element, _ => animation.Blink.Loop = value)));
        }

        private Element CreateTrackingAnimationInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("From Spacing", () => animation.Tracking.FromSpacing,
                    value => Set(element, _ => animation.Tracking.FromSpacing = value)),
                UI.Field("To Spacing", () => animation.Tracking.ToSpacing,
                    value => Set(element, _ => animation.Tracking.ToSpacing = value)),
                CreateValuePatternInspector(element, animation),
                CreateAnimationTimingInspector(element, animation),
                UI.Field("Loop", () => animation.Tracking.Loop,
                    value => Set(element, _ => animation.Tracking.Loop = value)));
        }

        private Element CreateValuePatternInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Dropdown("Pattern", () => (int)GetAnimationPattern(animation),
                    value => Set(element, _ => SetAnimationPattern(animation, (PosterValuePattern)value)),
                    ValuePatternNames),
                UI.DynamicElementOnStatusChanged(
                    () => GetAnimationPattern(animation),
                    pattern => pattern == PosterValuePattern.Envelope
                        ? CreateEnvelopeInspector(element, GetAnimationEnvelope(animation))
                        : UI.Space()));
        }

        private Element CreateEnvelopeInspector(PosterElement element, PosterEnvelopeSettings envelope)
        {
            return UI.Column(
                UI.Field("Rise", () => envelope.Rise,
                    value => Set(element, _ => envelope.Rise = Mathf.Max(0f, value))),
                UI.Field("High Hold", () => envelope.HighHold,
                    value => Set(element, _ => envelope.HighHold = Mathf.Max(0f, value))),
                UI.Field("Fall", () => envelope.Fall,
                    value => Set(element, _ => envelope.Fall = Mathf.Max(0f, value))),
                UI.Field("Low Hold", () => envelope.LowHold,
                    value => Set(element, _ => envelope.LowHold = Mathf.Max(0f, value))));
        }

        private static PosterValuePattern GetAnimationPattern(PosterTextAnimationEntry animation)
        {
            return animation.Type switch
            {
                PosterTextAnimationType.Fade => animation.Fade.Pattern,
                PosterTextAnimationType.Scale => animation.Scale.Pattern,
                PosterTextAnimationType.Rotate => animation.Rotate.Pattern,
                PosterTextAnimationType.Shake => animation.Shake.Pattern,
                PosterTextAnimationType.Tracking => animation.Tracking.Pattern,
                _ => animation.Move.Pattern
            };
        }

        private static void SetAnimationPattern(
            PosterTextAnimationEntry animation,
            PosterValuePattern pattern)
        {
            switch (animation.Type)
            {
                case PosterTextAnimationType.Fade: animation.Fade.Pattern = pattern; break;
                case PosterTextAnimationType.Scale: animation.Scale.Pattern = pattern; break;
                case PosterTextAnimationType.Rotate: animation.Rotate.Pattern = pattern; break;
                case PosterTextAnimationType.Shake: animation.Shake.Pattern = pattern; break;
                case PosterTextAnimationType.Tracking: animation.Tracking.Pattern = pattern; break;
                default: animation.Move.Pattern = pattern; break;
            }
        }

        private static PosterEnvelopeSettings GetAnimationEnvelope(PosterTextAnimationEntry animation)
        {
            return animation.Type switch
            {
                PosterTextAnimationType.Fade => animation.Fade.Envelope,
                PosterTextAnimationType.Scale => animation.Scale.Envelope,
                PosterTextAnimationType.Rotate => animation.Rotate.Envelope,
                PosterTextAnimationType.Shake => animation.Shake.Envelope,
                PosterTextAnimationType.Tracking => animation.Tracking.Envelope,
                _ => animation.Move.Envelope
            };
        }

        private Element CreateAnimationTimingInspector(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Column(
                UI.Dropdown("Timing", () => (int)animation.Timing.Mode,
                    value => Set(element, _ => animation.Timing.Mode = (PosterMotionTiming)value), MotionTimingNames),
                UI.DynamicElementOnStatusChanged(
                    () => animation.Timing.Mode,
                    timing => CreateAnimationTimingFields(element, animation, timing)));
        }

        private Element CreateAnimationTimingFields(
            PosterElement element,
            PosterTextAnimationEntry animation,
            PosterMotionTiming timing)
        {
            if (timing == PosterMotionTiming.Seconds)
            {
                var speed = CreateSecondsSpeedField(element, animation);
                return animation.Type is PosterTextAnimationType.Typewriter or PosterTextAnimationType.Blink
                    ? speed
                    : UI.Column(speed, CreateEasingField(element, animation));
            }

            var easing = animation.Type == PosterTextAnimationType.Blink
                ? UI.Space()
                : CreateEasingField(element, animation);
            return timing == PosterMotionTiming.BeatCycle
                ? UI.Column(
                    UI.Field("Beats Per Cycle", () => animation.Timing.BeatsPerCycle,
                        value => Set(element, _ => animation.Timing.BeatsPerCycle = Mathf.Max(0.001f, value))),
                    easing)
                : UI.Column(
                    UI.Field("Interval Beats", () => animation.Timing.IntervalBeats,
                        value => Set(element, _ => animation.Timing.IntervalBeats = Mathf.Max(0.001f, value))),
                    UI.Field("Active Beats", () => animation.Timing.ActiveBeats,
                        value => Set(element, _ => animation.Timing.ActiveBeats = Mathf.Max(0.001f, value))),
                    easing);
        }

        private Element CreateSecondsSpeedField(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return animation.Type switch
            {
                PosterTextAnimationType.Typewriter => UI.Field(
                    "Characters / Sec",
                    () => animation.Typewriter.CharactersPerSecond,
                    value => Set(element, _ => animation.Typewriter.CharactersPerSecond = Mathf.Max(0f, value))),
                PosterTextAnimationType.Fade => UI.Field("Speed (cycles / sec)", () => animation.Fade.Speed,
                    value => Set(element, _ => animation.Fade.Speed = Mathf.Max(0f, value))),
                PosterTextAnimationType.Scale => UI.Field("Speed (cycles / sec)", () => animation.Scale.Speed,
                    value => Set(element, _ => animation.Scale.Speed = Mathf.Max(0f, value))),
                PosterTextAnimationType.Rotate => UI.Field("Speed (cycles / sec)", () => animation.Rotate.Speed,
                    value => Set(element, _ => animation.Rotate.Speed = Mathf.Max(0f, value))),
                PosterTextAnimationType.Shake => UI.Field("Speed (cycles / sec)", () => animation.Shake.Speed,
                    value => Set(element, _ => animation.Shake.Speed = Mathf.Max(0f, value))),
                PosterTextAnimationType.Blink => UI.Field("Speed (cycles / sec)", () => animation.Blink.Speed,
                    value => Set(element, _ => animation.Blink.Speed = Mathf.Max(0f, value))),
                PosterTextAnimationType.Tracking => UI.Field("Speed (cycles / sec)", () => animation.Tracking.Speed,
                    value => Set(element, _ => animation.Tracking.Speed = Mathf.Max(0f, value))),
                _ => UI.Field("Speed (cycles / sec)", () => animation.Move.Speed,
                    value => Set(element, _ => animation.Move.Speed = Mathf.Max(0f, value)))
            };
        }

        private Element CreateEasingField(
            PosterElement element,
            PosterTextAnimationEntry animation)
        {
            return UI.Dropdown("Easing", () => (int)GetAnimationEasing(animation),
                value => Set(element, _ => SetAnimationEasing(animation, (PosterMoveEasing)value)), MoveEasingNames);
        }

        private static PosterMoveEasing GetAnimationEasing(PosterTextAnimationEntry animation)
        {
            return animation.Type switch
            {
                PosterTextAnimationType.Typewriter => animation.Typewriter.Easing,
                PosterTextAnimationType.Fade => animation.Fade.Easing,
                PosterTextAnimationType.Scale => animation.Scale.Easing,
                PosterTextAnimationType.Rotate => animation.Rotate.Easing,
                PosterTextAnimationType.Shake => animation.Shake.Easing,
                PosterTextAnimationType.Tracking => animation.Tracking.Easing,
                _ => animation.Move.Easing
            };
        }

        private static void SetAnimationEasing(
            PosterTextAnimationEntry animation,
            PosterMoveEasing easing)
        {
            switch (animation.Type)
            {
                case PosterTextAnimationType.Typewriter: animation.Typewriter.Easing = easing; break;
                case PosterTextAnimationType.Fade: animation.Fade.Easing = easing; break;
                case PosterTextAnimationType.Scale: animation.Scale.Easing = easing; break;
                case PosterTextAnimationType.Rotate: animation.Rotate.Easing = easing; break;
                case PosterTextAnimationType.Shake: animation.Shake.Easing = easing; break;
                case PosterTextAnimationType.Tracking: animation.Tracking.Easing = easing; break;
                default: animation.Move.Easing = easing; break;
            }
        }

        private void AddTextAnimation(PosterTextSettings settings)
        {
            PosterCanvas.NormalizeTextAnimations(settings);
            var animation = new PosterTextAnimationEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = (PosterTextAnimationType)_animationTypeToAdd
            };
            settings.Animations.Add(animation);
            _animationFoldStates[animation.Id] = true;
            DocumentChanged();
        }

        private void DuplicateTextAnimation(PosterTextSettings settings, string id)
        {
            PosterCanvas.NormalizeTextAnimations(settings);
            var index = FindAnimationIndex(settings, id);
            if (index < 0 || index >= settings.Animations.Count) return;
            var duplicate = Copy(settings.Animations[index]);
            duplicate.Id = Guid.NewGuid().ToString("N");
            settings.Animations.Insert(index + 1, duplicate);
            _animationFoldStates[duplicate.Id] = true;
            DocumentChanged();
        }

        private void DeleteTextAnimation(PosterTextSettings settings, string id)
        {
            var index = FindAnimationIndex(settings, id);
            if (index < 0 || index >= settings.Animations.Count) return;
            settings.Animations.RemoveAt(index);
            _animationFoldStates.Remove(id);
            DocumentChanged();
        }

        private void MoveTextAnimation(
            PosterTextSettings settings,
            string id,
            int direction)
        {
            var index = FindAnimationIndex(settings, id);
            var target = index + direction;
            if (index < 0 || index >= settings.Animations.Count
                          || target < 0 || target >= settings.Animations.Count) return;
            var animation = settings.Animations[index];
            settings.Animations.RemoveAt(index);
            settings.Animations.Insert(target, animation);
            DocumentChanged();
        }

        private static int FindAnimationIndex(PosterTextSettings settings, string id)
        {
            return settings.Animations.FindIndex(animation => animation?.Id == id);
        }

        private static void RegenerateAnimationIds(PosterDocument document)
        {
            if (document?.Elements == null) return;
            foreach (var element in document.Elements) RegenerateAnimationIds(element);
        }

        private static void RegenerateAnimationIds(PosterElement element)
        {
            if (element?.Text?.Animations == null) return;
            foreach (var animation in element.Text.Animations)
                if (animation != null)
                    animation.Id = Guid.NewGuid().ToString("N");
        }

        private static int AnimationListStatus(PosterTextSettings settings)
        {
            unchecked
            {
                var hash = settings.Animations.Count;
                foreach (var animation in settings.Animations)
                {
                    hash = hash * 31 + (animation?.GetHashCode() ?? 0);
                    if (animation == null) continue;
                    hash = hash * 31 + (int)animation.Type;
                    hash = hash * 31 + (int)animation.Timing.Mode;
                    hash = hash * 31 + animation.Timing.BeatsPerCycle.GetHashCode();
                    hash = hash * 31 + animation.Timing.IntervalBeats.GetHashCode();
                    hash = hash * 31 + animation.Timing.ActiveBeats.GetHashCode();
                    hash = hash * 31 + (int)animation.Move.Axis;
                    if (animation.Type is not PosterTextAnimationType.Typewriter
                        and not PosterTextAnimationType.Blink)
                        hash = hash * 31 + (int)GetAnimationPattern(animation);
                }
                return hash;
            }
        }

        private static string AnimationSummary(PosterTextAnimationEntry animation)
        {
            if (animation == null) return "Missing";
            var timing = animation.Timing.Mode switch
            {
                PosterMotionTiming.BeatCycle => $"Beat Cycle {animation.Timing.BeatsPerCycle:g}",
                PosterMotionTiming.BeatLeap =>
                    $"Beat Leap {animation.Timing.IntervalBeats:g}/{animation.Timing.ActiveBeats:g}",
                _ => "Seconds"
            };
            return animation.Type switch
            {
                PosterTextAnimationType.Typewriter => $"Typewriter — {timing}",
                PosterTextAnimationType.Fade => $"Fade — {timing}",
                PosterTextAnimationType.Scale => $"Scale — {timing}",
                PosterTextAnimationType.Rotate => $"Rotate — {timing}",
                PosterTextAnimationType.Shake => $"Shake — {animation.Shake.Pattern} / {timing}",
                PosterTextAnimationType.Blink => $"Blink — {timing}",
                PosterTextAnimationType.Tracking => $"Tracking — {animation.Tracking.Pattern} / {timing}",
                _ => $"Move — {(animation.Move.Axis == PosterTextMotion.MoveY ? "Y" : "X")}"
                     + $" / {animation.Move.Pattern} / {timing}"
            };
        }

        private Element CreateFontInspector(PosterElement element, PosterTextSettings settings)
        {
            var catalog = _posterCanvas.FontCatalog;
            if (catalog == null || catalog.Fonts.Count == 0)
                return UI.HelpBox("Poster Font Catalog is not configured.");

            var labels = new string[catalog.Fonts.Count];
            for (var index = 0; index < labels.Length; index++)
            {
                var definition = catalog.Fonts[index];
                labels[index] = string.IsNullOrWhiteSpace(definition?.Label)
                    ? definition?.Id ?? "Missing Font"
                    : definition.Label;
            }

            return UI.Dropdown("Font", () => catalog.IndexOf(settings.FontId),
                value => Set(element, _ => settings.FontId = catalog.IdAt(value)), labels);
        }

        private Element CreateLineInspector(PosterElement element)
        {
            var settings = element.Line;
            return UI.Column(
                UI.Field("Use Placement Bounds", () => settings.UsePlacementBounds,
                    value => Set(element, _ => settings.UsePlacementBounds = value)),
                UI.Field("Thickness", () => settings.Thickness,
                    value => Set(element, _ => settings.Thickness = Mathf.Max(0.1f, value))),
                UI.DynamicElementIf(
                    () => !settings.UsePlacementBounds,
                    () => UI.Column(
                        UI.Field("Start (0..1)", () => settings.Start,
                            value => Set(element, _ => settings.Start = Clamp01(value))),
                        UI.Field("End (0..1)", () => settings.End,
                            value => Set(element, _ => settings.End = Clamp01(value))))));
        }

        private Element CreateTextureInspector(PosterElement element)
        {
            var settings = element.Texture;
            return UI.Column(
                UI.Dropdown("Source", () => (int)settings.Source,
                    value => Set(element, _ => settings.Source = (PosterTextureSource)value), TextureSourceNames),
                UI.Dropdown("Fit", () => (int)settings.Fit,
                    value => Set(element, _ => settings.Fit = (PosterFit)value), FitNames));
        }

        private void AddElement(PosterElementType type)
        {
            var document = ActiveDocument;
            if (document == null) return;
            document.Elements ??= new List<PosterElement>();

            var element = CreateDefaultElement(type);
            document.Elements.Insert(0, element);
            _selectedElement = element;
            DocumentChanged();
        }

        private PosterElement CreateDefaultElement(PosterElementType type)
        {
            var element = new PosterElement
            {
                Name = type == PosterElementType.Texture ? "Model" : type.ToString(),
                Enabled = true,
                Type = type,
                Placement = new PosterPlacement
                {
                    Mode = PosterPlacementMode.Free,
                    Anchor = PosterAnchor.TopLeft,
                    X = 0.1f,
                    Y = 0.1f,
                    Width = 0.35f,
                    Height = 0.2f
                },
                Color = type == PosterElementType.Text ? PosterColorRef.White : PosterColorRef.PaletteMain,
                Text = new PosterTextSettings
                {
                    Source = PosterTextSource.Literal,
                    Literal = "TEXT",
                    FontId = _posterCanvas.FontCatalog?.DefaultFontId,
                    FontSize = 0.06f,
                    HorizontalAlign = PosterHorizontalAlign.Left,
                    VerticalAlign = PosterVerticalAlign.Top
                },
                Texture = new PosterTextureSettings
                {
                    Source = PosterTextureSource.Model,
                    Fit = PosterFit.Cover
                },
                Line = new PosterLineSettings
                {
                    UsePlacementBounds = true,
                    Thickness = 4f
                }
            };

            if (type == PosterElementType.Line) element.Placement.Height = 0.04f;
            return element;
        }

        private void DuplicateSelected()
        {
            var document = ActiveDocument;
            if (document?.Elements == null || _selectedElement == null) return;

            var index = document.Elements.IndexOf(_selectedElement);
            if (index < 0) return;

            var duplicate = Copy(_selectedElement);
            duplicate.Name = $"{DisplayName(_selectedElement)} Copy";
            RegenerateAnimationIds(duplicate);
            document.Elements.Insert(index + 1, duplicate);
            _selectedElement = duplicate;
            DocumentChanged();
        }

        private void DeleteSelected()
        {
            var document = ActiveDocument;
            if (document?.Elements == null || _selectedElement == null) return;

            var index = document.Elements.IndexOf(_selectedElement);
            if (index < 0) return;

            document.Elements.RemoveAt(index);
            _selectedElement = document.Elements.Count == 0
                ? null
                : document.Elements[Mathf.Min(index, document.Elements.Count - 1)];
            DocumentChanged();
        }

        private void SelectElement(PosterElement element)
        {
            _selectedElement = element;
        }

        private void Set(PosterElement element, Action<PosterElement> write)
        {
            if (element == null) return;
            write(element);
            EnsureSettings(element);
            DocumentChanged();
        }

        private void DocumentChanged()
        {
            _observedOrderSignature = CalculateOrderSignature(ActiveDocument);
            _posterCanvas?.MarkDirty();
        }

        private int GetInspectorStatus()
        {
            unchecked
            {
                return (_posterCanvas?.ActiveDocumentIndex ?? -1) * 397
                       ^ (_selectedElement?.GetHashCode() ?? 0)
                       ^ ((int)(_selectedElement?.Type ?? 0) << 16);
            }
        }

        private int GetLibraryStatus()
        {
            unchecked
            {
                var hash = _posterCanvas?.ActiveDocumentIndex ?? -1;
                var documents = _posterCanvas?.Library.Documents;
                if (documents == null) return hash;
                hash = hash * 31 + documents.Count;
                foreach (var document in documents)
                    hash = hash * 31 + (document?.Name?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private void SynchronizeSelection()
        {
            var document = ActiveDocument;
            _observedDocument = document;
            document?.Elements?.RemoveAll(element => element == null);
            if (document?.Elements == null || document.Elements.Count == 0)
            {
                _selectedElement = null;
            }
            else if (_selectedElement == null || !document.Elements.Contains(_selectedElement))
            {
                _selectedElement = document.Elements[0];
            }

            _observedOrderSignature = CalculateOrderSignature(document);
        }

        private void LoadSavedLibrary()
        {
            var record = _prefs.Get();
            if (record != null
                && record.HasSavedData
                && record.SchemaVersion == SchemaVersion
                && record.Library != null)
            {
                _posterCanvas.ReplaceLibrary(Copy(record.Library));
                Debug.Log("[Poster] Library v3 loaded from Prefs.", this);
                return;
            }

            var legacyV2Record = _legacyV2Prefs.Get();
            if (legacyV2Record != null
                && legacyV2Record.HasSavedData
                && legacyV2Record.SchemaVersion == 2
                && legacyV2Record.Library != null)
            {
                TryLoadLegacyLibrary(legacyV2Record.Library.Documents, 2);
                return;
            }

            var legacyV1Record = _legacyV1Prefs.Get();
            if (legacyV1Record == null
                || !legacyV1Record.HasSavedData
                || legacyV1Record.SchemaVersion != 1
                || legacyV1Record.Library == null) return;

            TryLoadLegacyLibrary(new[]
            {
                legacyV1Record.Library.Editorial,
                legacyV1Record.Library.Split,
                legacyV1Record.Library.Modular,
                legacyV1Record.Library.Custom
            }, 1);
        }

        private void TryLoadLegacyLibrary(IEnumerable<LegacyPosterDocument> documents, int version)
        {
            try
            {
                var library = ConvertLegacyLibrary(documents);
                _posterCanvas.ReplaceLibrary(library);
                Debug.Log($"[Poster] Library v{version} migrated in memory. Save to persist v3.", this);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Poster] Library v{version} migration failed; Scene defaults and legacy Prefs were preserved.\n{exception}",
                    this);
            }
        }

        private PosterLibrary ConvertLegacyLibrary(IEnumerable<LegacyPosterDocument> legacyDocuments)
        {
            var library = new PosterLibrary();
            if (legacyDocuments != null)
            {
                foreach (var legacyDocument in legacyDocuments)
                {
                    if (legacyDocument != null)
                        library.Documents.Add(ConvertLegacyDocument(legacyDocument));
                }
            }

            return library.Documents.Count == 0
                ? PosterDocumentFactory.CreateDefaultLibrary()
                : library;
        }

        private PosterDocument ConvertLegacyDocument(LegacyPosterDocument legacy)
        {
            var document = new PosterDocument
            {
                Name = legacy.Name,
                Grid = new PosterGridDefinition(),
                Elements = new List<PosterElement>()
            };
            if (legacy.Elements == null) return document;

            foreach (var legacyElement in legacy.Elements)
            {
                if (legacyElement != null)
                    document.Elements.Add(ConvertLegacyElement(legacyElement));
            }

            return document;
        }

        private PosterElement ConvertLegacyElement(LegacyPosterElement legacy)
        {
            var text = legacy.Text ?? new PosterTextSettings();
            var anchor = legacy.Type == PosterElementType.Text
                ? ResolveTextAnchor(text.HorizontalAlign, text.VerticalAlign)
                : PosterAnchor.TopLeft;
            var placement = ConvertLegacyPlacement(legacy.Placement, anchor);
            var line = legacy.Line ?? new PosterLineSettings();
            if (!line.UsePlacementBounds)
            {
                line.Start = new Vector2(
                    line.Start.x / PosterLegacyPlacementConverter.ReferenceWidth,
                    line.Start.y / PosterLegacyPlacementConverter.ReferenceHeight);
                line.End = new Vector2(
                    line.End.x / PosterLegacyPlacementConverter.ReferenceWidth,
                    line.End.y / PosterLegacyPlacementConverter.ReferenceHeight);
            }

            return new PosterElement
            {
                Name = legacy.Name,
                Enabled = legacy.Enabled,
                Type = legacy.Type,
                Placement = placement,
                Rotation = legacy.Rotation,
                Color = legacy.Color,
                CustomColor = legacy.CustomColor,
                Text = new PosterTextSettings
                {
                    Source = text.Source,
                    Literal = text.Literal,
                    Format = text.Format,
                    FontId = string.IsNullOrWhiteSpace(text.FontId)
                        ? _posterCanvas.FontCatalog?.DefaultFontId
                        : text.FontId,
                    FontSize = text.FontSize,
                    HorizontalAlign = text.HorizontalAlign,
                    VerticalAlign = text.VerticalAlign
                },
                Texture = Copy(legacy.Texture) ?? new PosterTextureSettings(),
                Line = Copy(line) ?? new PosterLineSettings()
            };
        }

        private static PosterPlacement ConvertLegacyPlacement(
            LegacyPosterPlacement legacy,
            PosterAnchor anchor)
        {
            legacy ??= new LegacyPosterPlacement();
            var placement = legacy.Mode == PosterPlacementMode.Grid
                ? PosterLegacyPlacementConverter.FromGrid(
                    legacy.Column,
                    legacy.ColumnSpan,
                    legacy.Baseline,
                    legacy.BaselineSpan,
                    legacy.OffsetX,
                    legacy.OffsetY)
                : PosterLegacyPlacementConverter.FromFree(
                    legacy.X,
                    legacy.Y,
                    legacy.Width,
                    legacy.Height,
                    legacy.OffsetX,
                    legacy.OffsetY);
            return anchor == PosterAnchor.TopLeft
                ? placement
                : PosterLegacyPlacementConverter.WithAnchor(placement, anchor);
        }

        private static PosterAnchor ResolveTextAnchor(
            PosterHorizontalAlign horizontalAlign,
            PosterVerticalAlign verticalAlign)
        {
            return (horizontalAlign, verticalAlign) switch
            {
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Top) => PosterAnchor.Top,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Top) => PosterAnchor.TopRight,
                (PosterHorizontalAlign.Left, PosterVerticalAlign.Center) => PosterAnchor.Left,
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Center) => PosterAnchor.Center,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Center) => PosterAnchor.Right,
                (PosterHorizontalAlign.Left, PosterVerticalAlign.Bottom) => PosterAnchor.BottomLeft,
                (PosterHorizontalAlign.Center, PosterVerticalAlign.Bottom) => PosterAnchor.Bottom,
                (PosterHorizontalAlign.Right, PosterVerticalAlign.Bottom) => PosterAnchor.BottomRight,
                _ => PosterAnchor.TopLeft
            };
        }

        private static int CalculateOrderSignature(PosterDocument document)
        {
            unchecked
            {
                var hash = 17;
                var elements = document?.Elements;
                if (elements == null) return hash;
                hash = hash * 31 + elements.Count;
                foreach (var element in elements)
                    hash = hash * 31 + (element?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private static PosterElement FirstElement(PosterDocument document)
        {
            return document?.Elements != null && document.Elements.Count > 0
                ? document.Elements[0]
                : null;
        }

        private static string DisplayName(PosterElement element)
        {
            return string.IsNullOrWhiteSpace(element?.Name) ? "Unnamed" : element.Name;
        }

        private static int RotationIndex(PosterRotation rotation)
        {
            return Mathf.Clamp((int)rotation / 90, 0, 3);
        }

        private static Vector2 Clamp01(Vector2 value)
        {
            return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
        }

        private static void EnsureSettings(PosterElement element)
        {
            element.Placement ??= new PosterPlacement();
            element.Text ??= new PosterTextSettings();
            PosterCanvas.NormalizeTextAnimations(element.Text);
            element.Texture ??= new PosterTextureSettings();
            element.Line ??= new PosterLineSettings();
        }

        private static T Copy<T>(T value) where T : class
        {
            return value == null ? null : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        [Serializable]
        private sealed class PosterLibraryRecord
        {
            public int SchemaVersion = 3;
            public bool HasSavedData;
            public PosterLibrary Library;
        }

        [Serializable]
        private sealed class LegacyV2PosterLibraryRecord
        {
            public int SchemaVersion = 2;
            public bool HasSavedData;
            public LegacyV2PosterLibrary Library;
        }

        [Serializable]
        private sealed class LegacyV2PosterLibrary
        {
            public List<LegacyPosterDocument> Documents = new();
        }

        [Serializable]
        private sealed class LegacyV1PosterLibraryRecord
        {
            public int SchemaVersion = 1;
            public bool HasSavedData;
            public LegacyV1PosterLibrary Library;
        }

        [Serializable]
        private sealed class LegacyV1PosterLibrary
        {
            public LegacyPosterDocument Editorial;
            public LegacyPosterDocument Split;
            public LegacyPosterDocument Modular;
            public LegacyPosterDocument Custom;
        }

        [Serializable]
        private sealed class LegacyPosterDocument
        {
            public string Name;
            public List<LegacyPosterElement> Elements = new();
        }

        [Serializable]
        private sealed class LegacyPosterElement
        {
            public string Name;
            public bool Enabled = true;
            public PosterElementType Type;
            public LegacyPosterPlacement Placement = new();
            public PosterRotation Rotation;
            public PosterColorRef Color;
            public Color CustomColor = UnityEngine.Color.white;
            public PosterTextSettings Text = new();
            public PosterTextureSettings Texture = new();
            public PosterLineSettings Line = new();
        }

        [Serializable]
        private sealed class LegacyPosterPlacement
        {
            public PosterPlacementMode Mode;
            public int Column;
            public int ColumnSpan = 1;
            public int Baseline;
            public int BaselineSpan = 1;
            public float X;
            public float Y;
            public float Width;
            public float Height;
            public float OffsetX;
            public float OffsetY;
        }
    }
}
