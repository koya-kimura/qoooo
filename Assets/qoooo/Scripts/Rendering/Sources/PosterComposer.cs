using System;
using System.Collections;
using System.Collections.Generic;
using PrefsGUI;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
    [DefaultExecutionOrder(-9100)]
    // PosterLibrary.Documentsを編集するauthoring UIと、明示的なQoooo Save participant。
    // Prepareでauthored documentをstageし、Commitで受け入れ、Abortで失敗したSaveを処理する。autosaveはしない。
    public sealed class PosterComposer : MonoBehaviour, IUiTarget, IPrefsSaveParticipant
    {
        private const string PrefsKey = "qoooo.poster.library.v2";
        private const string LegacyPrefsKey = "qoooo.poster.library.v1";
        private const int SchemaVersion = 2;
        private const float LeftPaneWidth = 300f;
        private static readonly Color SelectedColor = new(0.08f, 0.45f, 0.55f, 0.4f);
        private static readonly string[] TypeNames = { "Text", "Rect", "Line", "Texture" };
        private static readonly string[] PlacementNames = { "Grid", "Free" };
        private static readonly string[] RotationNames = { "0", "90", "180", "270" };
        private static readonly string[] ColorNames = { "Palette Main", "Palette Sub", "White", "Black", "Custom" };
        private static readonly string[] TextSourceNames = { "Literal", "Main Text", "Current Date", "Current Time" };
        private static readonly string[] HorizontalAlignNames = { "Left", "Center", "Right" };
        private static readonly string[] VerticalAlignNames = { "Top", "Center", "Bottom" };
        private static readonly string[] TextureSourceNames = { "Model", "Background" };
        private static readonly string[] FitNames = { "Stretch", "Cover" };

        [SerializeField] private PosterCanvas _posterCanvas;

        private readonly PrefsAny<PosterLibraryRecord> _prefs = new(PrefsKey);
        private readonly PrefsAny<LegacyPosterLibraryRecord> _legacyPrefs = new(LegacyPrefsKey);
        private PosterLibraryRecord _recordBeforePrepare;
        private PosterDocument _observedDocument;
        private PosterElement _selectedElement;
        private int _observedOrderSignature;

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

            _recordBeforePrepare = Copy(_prefs.Get());
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

            return UI.Row(
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
                UI.Button("Delete", DeleteDocument),
                UI.Space(),
                UI.HelpBox("Use Qoooo > Save to persist poster changes."));
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
                UI.DynamicElementOnStatusChanged(
                    () => placement.Mode,
                    mode => mode == PosterPlacementMode.Grid
                        ? CreateGridPlacementInspector(element, placement)
                        : CreateFreePlacementInspector(element, placement)));
        }

        private Element CreateGridPlacementInspector(PosterElement element, PosterPlacement placement)
        {
            return UI.Column(
                UI.Field("Column", () => placement.Column,
                    value => Set(element, _ => placement.Column = Mathf.Clamp(value, 0, 11))),
                UI.Field("Column Span", () => placement.ColumnSpan,
                    value => Set(element, _ => placement.ColumnSpan = Mathf.Clamp(value, 1, 12 - placement.Column))),
                UI.Field("Baseline", () => placement.Baseline,
                    value => Set(element, _ => placement.Baseline = Mathf.Max(0, value))),
                UI.Field("Baseline Span", () => placement.BaselineSpan,
                    value => Set(element, _ => placement.BaselineSpan = Mathf.Max(1, value))),
                UI.Field("Offset X", () => placement.OffsetX,
                    value => Set(element, _ => placement.OffsetX = value)),
                UI.Field("Offset Y", () => placement.OffsetY,
                    value => Set(element, _ => placement.OffsetY = value)));
        }

        private Element CreateFreePlacementInspector(PosterElement element, PosterPlacement placement)
        {
            return UI.Column(
                UI.Field("X", () => placement.X, value => Set(element, _ => placement.X = value)),
                UI.Field("Y", () => placement.Y, value => Set(element, _ => placement.Y = value)),
                UI.Field("Width", () => placement.Width,
                    value => Set(element, _ => placement.Width = Mathf.Max(0f, value))),
                UI.Field("Height", () => placement.Height,
                    value => Set(element, _ => placement.Height = Mathf.Max(0f, value))),
                UI.Field("Offset X", () => placement.OffsetX,
                    value => Set(element, _ => placement.OffsetX = value)),
                UI.Field("Offset Y", () => placement.OffsetY,
                    value => Set(element, _ => placement.OffsetY = value)));
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
                UI.Field("Font Size", () => settings.FontSize,
                    value => Set(element, _ => settings.FontSize = Mathf.Max(0.001f, value))),
                UI.Dropdown("Horizontal Align", () => (int)settings.HorizontalAlign,
                    value => Set(element, _ => settings.HorizontalAlign = (PosterHorizontalAlign)value), HorizontalAlignNames),
                UI.Dropdown("Vertical Align", () => (int)settings.VerticalAlign,
                    value => Set(element, _ => settings.VerticalAlign = (PosterVerticalAlign)value), VerticalAlignNames));
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
                        UI.Field("Start", () => settings.Start,
                            value => Set(element, _ => settings.Start = value)),
                        UI.Field("End", () => settings.End,
                            value => Set(element, _ => settings.End = value)))));
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
            var width = Mathf.Max(320f, _posterCanvas.Width * 0.35f);
            var height = Mathf.Max(120f, _posterCanvas.Height * 0.2f);
            var element = new PosterElement
            {
                Name = type == PosterElementType.Texture ? "Model" : type.ToString(),
                Enabled = true,
                Type = type,
                Placement = new PosterPlacement
                {
                    Mode = PosterPlacementMode.Free,
                    X = _posterCanvas.Width * 0.1f,
                    Y = _posterCanvas.Height * 0.1f,
                    Width = width,
                    Height = height
                },
                Color = type == PosterElementType.Text ? PosterColorRef.White : PosterColorRef.PaletteMain,
                Text = new PosterTextSettings
                {
                    Source = PosterTextSource.Literal,
                    Literal = "TEXT",
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

            if (type == PosterElementType.Line) element.Placement.Height = 40f;
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
                Debug.Log("[Poster] Library v2 loaded from Prefs.", this);
                return;
            }

            var legacyRecord = _legacyPrefs.Get();
            if (legacyRecord == null
                || !legacyRecord.HasSavedData
                || legacyRecord.SchemaVersion != 1
                || legacyRecord.Library == null) return;

            _posterCanvas.ReplaceLibrary(ConvertLegacyLibrary(legacyRecord.Library));
            Debug.Log("[Poster] Library v1 migrated from Prefs.", this);
        }

        private static PosterLibrary ConvertLegacyLibrary(LegacyPosterLibrary legacy)
        {
            var library = new PosterLibrary();
            AddLegacyDocument(library.Documents, legacy.Editorial);
            AddLegacyDocument(library.Documents, legacy.Split);
            AddLegacyDocument(library.Documents, legacy.Modular);
            AddLegacyDocument(library.Documents, legacy.Custom);
            return library.Documents.Count == 0 ? PosterDocumentFactory.CreateDefaultLibrary() : library;
        }

        private static void AddLegacyDocument(List<PosterDocument> documents, PosterDocument document)
        {
            if (document != null) documents.Add(Copy(document));
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

        private static void EnsureSettings(PosterElement element)
        {
            element.Placement ??= new PosterPlacement();
            element.Text ??= new PosterTextSettings();
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
            public int SchemaVersion = 2;
            public bool HasSavedData;
            public PosterLibrary Library;
        }

        [Serializable]
        private sealed class LegacyPosterLibraryRecord
        {
            public int SchemaVersion = 1;
            public bool HasSavedData;
            public LegacyPosterLibrary Library;
        }

        [Serializable]
        private sealed class LegacyPosterLibrary
        {
            public PosterDocument Editorial;
            public PosterDocument Split;
            public PosterDocument Modular;
            public PosterDocument Custom;
        }
    }
}
