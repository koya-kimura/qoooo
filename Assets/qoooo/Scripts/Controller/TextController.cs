using System.Collections.Generic;
using System.Text;
using qoooo.Pattern;
using TMPro;
using UnityEngine;

namespace qoooo.Controller
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textMesh;
        [SerializeField] private string _sourceText = "HELLO";
        [SerializeField] private List<TextPattern> _patterns = new();
        [SerializeField] private int _patternIndex;

        private readonly List<GlyphSample> _samples = new();
        private readonly List<GlyphSample> _entrySamples = new();
        private readonly StringBuilder _renderedText = new();
        private TMP_MeshInfo[] _baseMeshInfo;
        private bool _meshDirty = true;

        public string SourceText => _sourceText;
        public int PatternCount => _patterns.Count;

        private void Awake()
        {
            if (_textMesh == null) _textMesh = GetComponent<TMP_Text>();
            _meshDirty = true;
        }

        private void OnEnable()
        {
            _meshDirty = true;
        }

        private void OnValidate()
        {
            _meshDirty = true;
        }

        private void Update()
        {
            Render(Time.time);
        }

        public void SetText(string value)
        {
            value ??= string.Empty;
            if (_sourceText == value) return;

            _sourceText = value;
            _meshDirty = true;
        }

        public void SelectPattern(int index)
        {
            if (_patterns.Count == 0)
            {
                _patternIndex = 0;
                return;
            }

            var nextIndex = Mathf.Clamp(index, 0, _patterns.Count - 1);
            if (_patternIndex == nextIndex) return;

            _patternIndex = nextIndex;
            _meshDirty = true;
        }

        private void Render(float time)
        {
            if (_textMesh == null) return;
            if (string.IsNullOrEmpty(_sourceText) || _patterns.Count == 0)
            {
                if (_textMesh.text.Length > 0)
                {
                    _textMesh.text = string.Empty;
                    _baseMeshInfo = null;
                    _meshDirty = true;
                }
                return;
            }

            var pattern = _patterns[Mathf.Clamp(_patternIndex, 0, _patterns.Count - 1)];
            BuildSamples(pattern, time);
            BuildRenderedText();

            var renderedText = _renderedText.ToString();
            if (_textMesh.text != renderedText)
            {
                _textMesh.text = renderedText;
                _meshDirty = true;
            }

            PrepareBaseMesh();
            RestoreBaseVertices();
            ApplySamplesToMesh(_textMesh.textInfo);
        }

        private void PrepareBaseMesh()
        {
            if (_textMesh.havePropertiesChanged) _meshDirty = true;
            if (!_meshDirty && _baseMeshInfo != null) return;

            _textMesh.ForceMeshUpdate();
            _baseMeshInfo = _textMesh.textInfo.CopyMeshInfoVertexData();
            _meshDirty = false;
        }

        private void RestoreBaseVertices()
        {
            if (_baseMeshInfo == null) return;

            var meshInfo = _textMesh.textInfo.meshInfo;
            var materialCount = Mathf.Min(meshInfo.Length, _baseMeshInfo.Length);
            for (var index = 0; index < materialCount; index++)
            {
                var source = _baseMeshInfo[index].vertices;
                var destination = meshInfo[index].vertices;
                if (source == null || destination == null) continue;

                System.Array.Copy(source, destination, Mathf.Min(source.Length, destination.Length));
            }
        }

        private void BuildSamples(TextPattern pattern, float time)
        {
            _samples.Clear();

            foreach (var entry in pattern.Entries)
            {
                if (!entry.Enabled || entry.Sampler == null) continue;

                _entrySamples.Clear();
                entry.Sampler.Sample(_sourceText, _entrySamples);

                foreach (var sampled in _entrySamples)
                {
                    var sample = sampled;

                    foreach (var motion in entry.Motions)
                    {
                        if (motion != null) motion.Apply(ref sample, time);
                    }

                    sample.Position = entry.Offset
                        + Quaternion.Euler(entry.Rotation)
                        * Vector3.Scale(sample.Position, entry.Scale);
                    sample.Rotation = Quaternion.Euler(entry.Rotation) * sample.Rotation;
                    sample.Scale = Vector3.Scale(sample.Scale, entry.Scale);
                    _samples.Add(sample);
                }
            }
        }

        private void BuildRenderedText()
        {
            _renderedText.Clear();

            foreach (var sample in _samples)
            {
                if (sample.SourceCharacterIndex < 0
                    || sample.SourceCharacterIndex >= _sourceText.Length) continue;

                _renderedText.Append(_sourceText[sample.SourceCharacterIndex]);
            }
        }

        private void ApplySamplesToMesh(TMP_TextInfo textInfo)
        {
            var characterCount = Mathf.Min(textInfo.characterCount, _samples.Count);

            for (var index = 0; index < characterCount; index++)
            {
                var character = textInfo.characterInfo[index];
                if (!character.isVisible) continue;

                var sample = _samples[index];
                var vertices = textInfo.meshInfo[character.materialReferenceIndex].vertices;
                var vertexIndex = character.vertexIndex;
                var center = (character.bottomLeft + character.topRight) * 0.5f;
                var matrix = Matrix4x4.TRS(sample.Position, sample.Rotation, sample.Scale);

                for (var vertex = 0; vertex < 4; vertex++)
                {
                    vertices[vertexIndex + vertex] = matrix.MultiplyPoint3x4(
                        vertices[vertexIndex + vertex] - center);
                }
            }

            _textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
