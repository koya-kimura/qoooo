using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace qoooo.Rendering.Sources
{
    [Serializable]
    public sealed class PosterFontDefinition
    {
        public string Id;
        public string Label;
        public TMP_FontAsset Font;
    }

    [CreateAssetMenu(fileName = "Poster Font Catalog", menuName = "Qoooo/Poster Font Catalog")]
    public sealed class PosterFontCatalog : ScriptableObject
    {
        [SerializeField] private string _defaultFontId;
        [SerializeField] private List<PosterFontDefinition> _fonts = new();

        public IReadOnlyList<PosterFontDefinition> Fonts => _fonts ??= new List<PosterFontDefinition>();

        public string DefaultFontId => _defaultFontId;

        public TMP_FontAsset Resolve(string fontId, TMP_FontAsset fallback)
        {
            _fonts ??= new List<PosterFontDefinition>();
            var selected = Find(fontId);
            if (selected != null) return selected;

            var defaultFont = Find(_defaultFontId);
            if (defaultFont != null) return defaultFont;

            foreach (var definition in _fonts)
            {
                if (definition?.Font != null) return definition.Font;
            }

            return fallback;
        }

        public int IndexOf(string fontId)
        {
            _fonts ??= new List<PosterFontDefinition>();
            for (var index = 0; index < _fonts.Count; index++)
            {
                if (_fonts[index]?.Id == fontId && _fonts[index].Font != null) return index;
            }

            for (var index = 0; index < _fonts.Count; index++)
            {
                if (_fonts[index]?.Id == _defaultFontId && _fonts[index].Font != null) return index;
            }

            return 0;
        }

        public string IdAt(int index)
        {
            _fonts ??= new List<PosterFontDefinition>();
            if (index < 0 || index >= _fonts.Count) return _defaultFontId;
            return _fonts[index]?.Id ?? _defaultFontId;
        }

        public bool Contains(TMP_FontAsset font)
        {
            if (font == null) return false;
            _fonts ??= new List<PosterFontDefinition>();
            return _fonts.Exists(definition => definition?.Font == font);
        }

        public bool ContainsId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            _fonts ??= new List<PosterFontDefinition>();
            return _fonts.Exists(definition => definition?.Id == id);
        }

        public bool TryAdd(string id, string label, TMP_FontAsset font)
        {
            if (font == null || string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();
            if (Contains(font) || ContainsId(id)) return false;

            _fonts.Add(new PosterFontDefinition
            {
                Id = id,
                Label = string.IsNullOrWhiteSpace(label) ? font.name : label.Trim(),
                Font = font
            });
            if (string.IsNullOrWhiteSpace(_defaultFontId)) _defaultFontId = id;
            return true;
        }

        private TMP_FontAsset Find(string fontId)
        {
            _fonts ??= new List<PosterFontDefinition>();
            if (string.IsNullOrWhiteSpace(fontId)) return null;
            foreach (var definition in _fonts)
            {
                if (definition?.Id == fontId && definition.Font != null) return definition.Font;
            }

            return null;
        }
    }
}