using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Pattern
{
    [Serializable]
    public class TextPattern
    {
        [SerializeField] private string _name = "Pattern";
        [SerializeField] private List<TextSamplerEntry> _entries = new();

        public string Name => _name;
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

        public bool Enabled => _enabled;
        public ITextPattern Sampler => _sampler as ITextPattern;
        public Vector3 Offset => _offset;
        public Vector3 Rotation => _rotation;
        public Vector3 Scale => _scale;
        public IReadOnlyList<TextGlyphMotion> Motions => _motions;
    }
}
