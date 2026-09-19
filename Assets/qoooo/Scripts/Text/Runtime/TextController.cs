using System;
using System.Collections.Generic;
using System.Text;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Text.Layouts;
using qoooo.Text.Model;
using TMPro;
using UnityEngine;

namespace qoooo.Text.Runtime
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextController : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private TMP_Text _textMesh;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private string _sourceText = "HELLO";
        [SerializeField] private List<TextPattern> _patterns = new();
        [SerializeField] private int _patternIndex;

        [SerializeField]
        private RadioParameter _layout = new("text.layout", "Text Layout", new[] { "Three Circles" });

        [SerializeField]
        private FloatParameter _animationSpeed = new("text.animation-speed", "Text Animation Speed", 0f, 3f, 1f);

        [SerializeField] private StateParameter _transformMode = new("text.transform-mode", "Text Transform Mode", 3);
        [SerializeField] private MomentaryParameter _rebuild = new("text.rebuild", "Rebuild Text");

        [SerializeField]
        private SequenceParameter _blink = new("text.blink", "Text Beat Blink", new[] { true, true, true, false });

        private readonly List<GlyphSample> _entrySamples = new();
        private readonly StringBuilder _renderedText = new();

        private readonly List<GlyphSample> _samples = new();
        private TMP_MeshInfo[] _baseMeshInfo;
        private bool _meshDirty = true;

        public string SourceText => _sourceText;
        public int PatternCount => _patterns.Count;

        private void Awake()
        {
            if (_textMesh == null) _textMesh = GetComponent<TMP_Text>();
            SynchronizeLayoutOptions();
            _meshDirty = true;
        }

        private void Update()
        {
            if (_layout.Value != _patternIndex)
            {
                _patternIndex = Mathf.Clamp(_layout.Value, 0, Mathf.Max(0, _patterns.Count - 1));
                _meshDirty = true;
            }
            if (_rebuild.WasTriggeredThisFrame) _meshDirty = true;
            switch (_transformMode.Value)
            {
                case 1:
                    transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Sin(Time.time) * 0.25f,
                        transform.localPosition.z); break;
                case 2: transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time) * 8f); break;
                default:
                    transform.localPosition = new Vector3(transform.localPosition.x, 0f, transform.localPosition.z);
                    transform.localRotation = Quaternion.identity;
                    break;
            }

            Render(Time.time * _animationSpeed.Value);
        }

        private void OnEnable()
        {
            _meshDirty = true;
        }

        private void OnValidate()
        {
            SynchronizeLayoutOptions();
            _meshDirty = true;
        }

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _animationSpeed;
                yield return _layout;
                yield return _transformMode;
                yield return _rebuild;
                yield return _blink;
            }
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
            _layout.TrySetValue(nextIndex);
            if (_patternIndex == nextIndex) return;

            _patternIndex = nextIndex;
            _meshDirty = true;
        }

        private void SynchronizeLayoutOptions()
        {
            _layout ??= new RadioParameter("text.layout", "Text Layout", new[] { "Three Circles" });
            var names = new List<string>();
            foreach (var pattern in _patterns ?? new List<TextPattern>())
                names.Add(string.IsNullOrWhiteSpace(pattern?.Name) ? "Unnamed Layout" : pattern.Name);
            _layout.ReplaceOptions(names, 0);
        }

        private void Render(float time)
        {
            if (_textMesh == null) return;
            if (string.IsNullOrEmpty(_sourceText) || _patterns.Count == 0 || !_blink.IsActive)
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
            FitSamplesToOutput(pattern);
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

                Array.Copy(source, destination, Mathf.Min(source.Length, destination.Length));
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
                        if (motion != null)
                            motion.Apply(ref sample, time);

                    sample.Position = entry.Offset
                                      + Quaternion.Euler(entry.Rotation)
                                      * Vector3.Scale(sample.Position, entry.Scale);
                    sample.Rotation = Quaternion.Euler(entry.Rotation) * sample.Rotation;
                    sample.Scale = Vector3.Scale(sample.Scale, entry.Scale);
                    _samples.Add(sample);
                }
            }
        }

        private void FitSamplesToOutput(TextPattern pattern)
        {
            if (_outputCamera == null || !_outputCamera.orthographic) return;

            var texture = _outputCamera.targetTexture;
            var aspect = texture != null && texture.height > 0
                ? (float)texture.width / texture.height
                : _outputCamera.aspect;
            var localX = transform.TransformVector(Vector3.right).magnitude;
            var localY = transform.TransformVector(Vector3.up).magnitude;
            if (aspect <= 0f || localX <= 0f || localY <= 0f) return;

            var viewportHeight = _outputCamera.orthographicSize * 2f;
            var coverage = pattern.ScreenCoverage;
            var reference = pattern.ReferenceSize;
            // Typewriter only samples the currently visible prefix. Reserve room for the whole word
            // so its scale remains stable as more characters appear.
            foreach (var entry in pattern.Entries)
            {
                if (entry.Sampler is not TextTypewriterPattern typewriter) continue;
                var glyphWidth = _textMesh.GetPreferredValues("W").x * pattern.GlyphScale;
                var fullWidth = (Mathf.Max(0, _sourceText.Length - 1) * Mathf.Abs(typewriter.Spacing)
                                 + glyphWidth) * Mathf.Abs(entry.Scale.x) + Mathf.Abs(entry.Offset.x) * 2f;
                reference.x = Mathf.Max(reference.x, fullWidth);
            }
            var fitScale = Mathf.Min(
                viewportHeight * aspect * coverage.x / (reference.x * localX),
                viewportHeight * coverage.y / (reference.y * localY));

            for (var index = 0; index < _samples.Count; index++)
            {
                var sample = _samples[index];
                sample.Position = new Vector3(sample.Position.x * fitScale,
                    sample.Position.y * fitScale, sample.Position.z);
                sample.Scale *= fitScale * pattern.GlyphScale;
                _samples[index] = sample;
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
                    vertices[vertexIndex + vertex] = matrix.MultiplyPoint3x4(
                        vertices[vertexIndex + vertex] - center);
            }

            _textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
