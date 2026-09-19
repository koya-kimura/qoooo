using System;
using System.Collections.Generic;
using qoooo.Text.Motions;
using UnityEngine;

namespace qoooo.Text.Model
{
    [Serializable]
    public class TextPattern
    {
        [SerializeField] private string _name = "Pattern";
        [SerializeField, Tooltip("Width and height of this layout in sampler-local units, including glyph margins.")]
        private Vector2 _referenceSize = new(12f, 7f);
        [SerializeField, Tooltip("Maximum fraction of the output screen occupied by this layout (0 to 1). Fit preserves the aspect ratio.")]
        private Vector2 _screenCoverage = new(0.95f, 0.9f);
        [SerializeField, Min(0f), Tooltip("Glyph size relative to the fitted layout; does not change glyph positions.")]
        private float _glyphScale = 1f;
        [SerializeField] private List<TextSamplerEntry> _entries = new();

        public TextPattern() { }
        public TextPattern(string name, params TextSamplerEntry[] entries)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "Pattern" : name;
            _entries = entries == null ? new List<TextSamplerEntry>() : new List<TextSamplerEntry>(entries);
        }
        public string Name => _name;
        public Vector2 ReferenceSize => new(Mathf.Max(0.01f, _referenceSize.x), Mathf.Max(0.01f, _referenceSize.y));
        public Vector2 ScreenCoverage => new(Mathf.Clamp01(_screenCoverage.x), Mathf.Clamp01(_screenCoverage.y));
        public float GlyphScale => Mathf.Max(0f, _glyphScale);
        public IReadOnlyList<TextSamplerEntry> Entries => _entries;
    }

    [Serializable]
    public class TextSamplerEntry
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private MonoBehaviour _sampler;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private Vector3 _scale = Vector3.one;
        [SerializeField] private List<TextGlyphMotion> _motions = new();

        public TextSamplerEntry() { }
        public TextSamplerEntry(MonoBehaviour sampler, Vector3 offset = default, params TextGlyphMotion[] motions)
        {
            _sampler = sampler;
            _offset = offset;
            _motions = motions == null ? new List<TextGlyphMotion>() : new List<TextGlyphMotion>(motions);
        }
        public bool Enabled => _enabled;
        public ITextPattern Sampler => _sampler as ITextPattern;
        public Vector3 Offset => _offset;
        public Vector3 Rotation => _rotation;
        public Vector3 Scale => _scale;
        public IReadOnlyList<TextGlyphMotion> Motions => _motions;
    }
}
