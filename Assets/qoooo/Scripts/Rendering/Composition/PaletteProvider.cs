using System;
using System.Collections.Generic;
using System.Linq;
using PrefsGUI;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    public interface IPaletteProvider
    {
        Color Main { get; }
        Color Sub { get; }
        Color ColorA { get; }
        Color ColorB { get; }
        Color ColorC { get; }
        Color ColorD { get; }
    }

    [Serializable]
    public sealed class PaletteDefinition
    {
        public string Id;
        public string Name;
        public Color ColorA;
        public Color ColorB;
        public Color ColorC;
        public Color ColorD;
        public bool BuiltIn;
    }

    [Serializable]
    public sealed class PaletteCatalogRecord
    {
        public int SchemaVersion = 1;
        public bool HasSavedData;
        public string ActivePaletteId;
        public List<PaletteDefinition> UserPalettes = new();
    }

    // Owns the shared palette catalog and typed selection Parameter. User palettes persist through Qoooo Save.
    public class PaletteProvider : MonoBehaviour, IPaletteProvider, IMidiParameterContainer,
        IUiTarget, IPrefsSaveParticipant
    {
        private const string PrefsKey = "qoooo.visual.palettes.v1";
        private const string DefaultPaletteId = "cobalt";

        [SerializeField]
        private RadioParameter _palette = new(
            "visual.palette",
            "Visual Palette",
            new[] { "Cobalt", "Acid", "Hot" });

        private readonly PrefsAny<PaletteCatalogRecord> _prefs = new(PrefsKey);
        private readonly List<PaletteDefinition> _palettes = new();
        private PaletteCatalogRecord _recordBeforePrepare;
        private string _savedActivePaletteId;
        private int _editorIndex;
        private int _revision;
        private bool _editorOpen;

        public Color Main => ActivePalette.ColorA;
        public Color Sub => ActivePalette.ColorB;
        public Color ColorA => ActivePalette.ColorA;
        public Color ColorB => ActivePalette.ColorB;
        public Color ColorC => ActivePalette.ColorC;
        public Color ColorD => ActivePalette.ColorD;

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _palette; }
        }

        private PaletteDefinition ActivePalette
        {
            get
            {
                EnsureCatalog();
                return _palettes[Mathf.Clamp(_palette.Value, 0, _palettes.Count - 1)];
            }
        }

        private PaletteDefinition EditedPalette => _palettes.Count == 0
            ? null
            : _palettes[Mathf.Clamp(_editorIndex, 0, _palettes.Count - 1)];

        private void Awake()
        {
            _palette ??= new RadioParameter(
                "visual.palette",
                "Visual Palette",
                new[] { "Cobalt", "Acid", "Hot" });
            LoadCatalog();
        }

        private void Start()
        {
            SelectById(_savedActivePaletteId);
        }

        public Element CreateElement()
        {
            return CreateElement(null);
        }

        public Element CreateElement(Action save)
        {
            return UI.Column(
                UI.DynamicElementOnStatusChanged(
                    () => _revision,
                    _ => UI.Dropdown("Visual Palette", () => _palette.Value,
                        value => _palette.TrySetValue(value), _palette.Options.ToArray())),
                UI.Button("Edit Palettes", OpenEditor),
                UI.DynamicElementOnStatusChanged(EditorStatus, _ => CreateEditor(save)));
        }

        public void PrepareSave()
        {
            _recordBeforePrepare = Copy(_prefs.Get());
            _prefs.Set(CaptureRecord());
        }

        public void CommitSave()
        {
            _recordBeforePrepare = null;
            Debug.Log("[Palette] Catalog staged for Prefs.Save.", this);
        }

        public void AbortSave()
        {
            if (_recordBeforePrepare == null) return;
            _prefs.Set(_recordBeforePrepare);
            _recordBeforePrepare = null;
        }

        private Element CreateEditor(Action save)
        {
            if (!_editorOpen)
                return UI.Fold("Palette Editor", UI.HelpBox("Open the editor to manage palettes."));

            EnsureCatalog();
            var palette = EditedPalette;
            var controls = new List<Element>
            {
                UI.Dropdown("Palette", () => _editorIndex, SelectEditorPalette,
                    _palettes.Select(value => value.Name).ToArray()),
                UI.Row(
                    UI.Button("New", CreatePalette),
                    UI.Button("Duplicate", DuplicatePalette),
                    UI.Button("Delete", DeletePalette))
            };

            if (palette.BuiltIn)
            {
                controls.Add(UI.HelpBox("Built-in palettes are read-only. Duplicate one to edit it."));
            }
            else
            {
                controls.Add(UI.Field("Name", () => palette.Name, RenamePalette));
                controls.Add(UI.Field("Color A", () => palette.ColorA, value => SetColor(palette, 0, value)));
                controls.Add(UI.Field("Color B", () => palette.ColorB, value => SetColor(palette, 1, value)));
                controls.Add(UI.Field("Color C", () => palette.ColorC, value => SetColor(palette, 2, value)));
                controls.Add(UI.Field("Color D", () => palette.ColorD, value => SetColor(palette, 3, value)));
            }

            controls.Add(UI.Row(
                UI.Button("Save", save ?? (() => { })),
                UI.Button("Close", CloseEditor)));
            return UI.Fold("Palette Editor", UI.Column(controls)).Open();
        }

        private void OpenEditor()
        {
            _editorIndex = Mathf.Clamp(_palette.Value, 0, Mathf.Max(0, _palettes.Count - 1));
            _editorOpen = true;
            _revision++;
        }

        private void CloseEditor()
        {
            _editorOpen = false;
            _revision++;
        }

        private string EditorStatus()
        {
            return $"{_editorOpen}:{_editorIndex}:{_revision}";
        }

        private void SelectEditorPalette(int index)
        {
            _editorIndex = Mathf.Clamp(index, 0, _palettes.Count - 1);
            _revision++;
        }

        private void CreatePalette()
        {
            var source = ActivePalette;
            var palette = new PaletteDefinition
            {
                Id = NewUserId(),
                Name = UniqueName("Custom", null),
                ColorA = source.ColorA,
                ColorB = source.ColorB,
                ColorC = source.ColorC,
                ColorD = source.ColorD
            };
            _palettes.Add(palette);
            SynchronizeOptions(palette.Id);
            _editorIndex = _palettes.IndexOf(palette);
            _revision++;
        }

        private void DuplicatePalette()
        {
            var source = EditedPalette;
            if (source == null) return;
            var palette = new PaletteDefinition
            {
                Id = NewUserId(),
                Name = UniqueName($"{source.Name} Copy", null),
                ColorA = source.ColorA,
                ColorB = source.ColorB,
                ColorC = source.ColorC,
                ColorD = source.ColorD
            };
            _palettes.Add(palette);
            SynchronizeOptions(palette.Id);
            _editorIndex = _palettes.IndexOf(palette);
            _revision++;
        }

        private void DeletePalette()
        {
            var palette = EditedPalette;
            if (palette == null || palette.BuiltIn) return;
            var activeId = ActivePalette.Id;
            var deletingActive = activeId == palette.Id;
            _palettes.Remove(palette);
            SynchronizeOptions(deletingActive ? DefaultPaletteId : activeId);
            _editorIndex = Mathf.Clamp(_editorIndex, 0, _palettes.Count - 1);
            _revision++;
        }

        private void RenamePalette(string name)
        {
            var palette = EditedPalette;
            if (palette == null || palette.BuiltIn) return;
            var activeId = ActivePalette.Id;
            palette.Name = UniqueName(name, palette);
            SynchronizeOptions(activeId);
            _revision++;
        }

        private void SetColor(PaletteDefinition palette, int index, Color value)
        {
            if (palette == null || palette.BuiltIn) return;
            value = SanitizeColor(value, Color.white);
            switch (index)
            {
                case 0: palette.ColorA = value; break;
                case 1: palette.ColorB = value; break;
                case 2: palette.ColorC = value; break;
                case 3: palette.ColorD = value; break;
            }
            _revision++;
        }

        private void LoadCatalog()
        {
            _palettes.Clear();
            _palettes.AddRange(CreateBuiltIns());
            var record = _prefs.Get();
            if (record != null && record.HasSavedData && record.SchemaVersion == 1)
            {
                AddValidUserPalettes(record.UserPalettes);
                _savedActivePaletteId = record.ActivePaletteId;
            }
            SynchronizeOptions(DefaultPaletteId);
        }

        private void EnsureCatalog()
        {
            if (_palettes.Count > 0) return;
            LoadCatalog();
        }

        private void AddValidUserPalettes(IEnumerable<PaletteDefinition> values)
        {
            var ids = new HashSet<string>(_palettes.Select(value => value.Id), StringComparer.Ordinal);
            foreach (var source in values ?? Array.Empty<PaletteDefinition>())
            {
                if (source == null || string.IsNullOrWhiteSpace(source.Id)) continue;
                var id = source.Id.Trim();
                if (!ids.Add(id)) continue;
                _palettes.Add(new PaletteDefinition
                {
                    Id = id,
                    Name = UniqueName(source.Name, null),
                    ColorA = SanitizeColor(source.ColorA, Color.black),
                    ColorB = SanitizeColor(source.ColorB, Color.white),
                    ColorC = SanitizeColor(source.ColorC, Color.gray),
                    ColorD = SanitizeColor(source.ColorD, Color.white)
                });
            }
        }

        private void SynchronizeOptions(string selectedId)
        {
            _palette.ReplaceOptions(_palettes.Select(value => value.Name));
            SelectById(selectedId);
        }

        private void SelectById(string id)
        {
            var index = _palettes.FindIndex(value => value.Id == id);
            _palette.TrySetValue(index >= 0 ? index : 0);
        }

        private PaletteCatalogRecord CaptureRecord()
        {
            return new PaletteCatalogRecord
            {
                HasSavedData = true,
                ActivePaletteId = ActivePalette.Id,
                UserPalettes = _palettes.Where(value => !value.BuiltIn).Select(Copy).ToList()
            };
        }

        private string UniqueName(string value, PaletteDefinition self)
        {
            var baseName = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
            var candidate = baseName;
            for (var suffix = 2; _palettes.Any(palette => palette != self && palette.Name == candidate); suffix++)
                candidate = $"{baseName} {suffix}";
            return candidate;
        }

        private string NewUserId()
        {
            string id;
            do id = $"custom-{Guid.NewGuid():N}"; while (_palettes.Any(value => value.Id == id));
            return id;
        }

        private static Color SanitizeColor(Color value, Color fallback)
        {
            return new Color(
                float.IsFinite(value.r) ? Mathf.Clamp01(value.r) : fallback.r,
                float.IsFinite(value.g) ? Mathf.Clamp01(value.g) : fallback.g,
                float.IsFinite(value.b) ? Mathf.Clamp01(value.b) : fallback.b,
                float.IsFinite(value.a) ? Mathf.Clamp01(value.a) : fallback.a);
        }

        private static IEnumerable<PaletteDefinition> CreateBuiltIns()
        {
            yield return new PaletteDefinition
            {
                Id = DefaultPaletteId, Name = "Cobalt", BuiltIn = true,
                ColorA = new Color(0.05f, 0.25f, 0.95f, 1f), ColorB = Color.white,
                ColorC = new Color(0.02f, 0.04f, 0.12f, 1f), ColorD = new Color(0.2f, 0.8f, 1f, 1f)
            };
            yield return new PaletteDefinition
            {
                Id = "acid", Name = "Acid", BuiltIn = true,
                ColorA = new Color(0.95f, 0.9f, 0.05f, 1f), ColorB = new Color(0.2f, 1f, 0.1f, 1f),
                ColorC = new Color(0.04f, 0.04f, 0.02f, 1f), ColorD = new Color(0.2f, 0.55f, 1f, 1f)
            };
            yield return new PaletteDefinition
            {
                Id = "hot", Name = "Hot", BuiltIn = true,
                ColorA = new Color(1f, 0.08f, 0.1f, 1f), ColorB = new Color(1f, 0.2f, 0.55f, 1f),
                ColorC = new Color(0.1f, 0.01f, 0.03f, 1f), ColorD = new Color(1f, 0.72f, 0.08f, 1f)
            };
        }

        private static PaletteDefinition Copy(PaletteDefinition value)
        {
            return value == null ? null : JsonUtility.FromJson<PaletteDefinition>(JsonUtility.ToJson(value));
        }

        private static PaletteCatalogRecord Copy(PaletteCatalogRecord value)
        {
            return value == null ? null : JsonUtility.FromJson<PaletteCatalogRecord>(JsonUtility.ToJson(value));
        }
    }
}