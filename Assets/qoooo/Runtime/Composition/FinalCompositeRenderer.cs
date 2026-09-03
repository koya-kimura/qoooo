using System;
using Qoooo.VJ;
using Qoooo.VJ.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qoooo.VJ.Composition
{
    [DisallowMultipleComponent]
    public sealed class FinalCompositeRenderer : MonoBehaviour
    {
        [SerializeField] private VjRuntimeSettings settings = new();
        [SerializeField] private Color solidColor = new(0.06f, 0.06f, 0.08f, 1f);
        [SerializeField] private CompositionController composition;
        [SerializeField] private Shader solidLayerShader;

        private OwnedRenderTexture _target;
        private CommandBuffer _commandBuffer;
        private Material _solidLayerMaterial;
        private MaterialPropertyBlock _layerProperties;
        private static readonly int LayerColorId = Shader.PropertyToID("_LayerColor");

        public event Action<RenderTexture> TextureChanged;

        public VjRuntimeSettings Settings => settings;
        public RenderTexture FinalTexture => _target?.Texture;
        public CompositionController Composition
        {
            get => composition;
            set => composition = value;
        }

        public Shader SolidLayerShader
        {
            get => solidLayerShader;
            set => solidLayerShader = value;
        }

        public Color SolidColor
        {
            get => solidColor;
            set => solidColor = value;
        }

        private void OnEnable()
        {
            EnsureInitialized();
            EnsureTarget();
            RenderNow();
        }

        private void OnDisable()
        {
            _commandBuffer?.Release();
            _commandBuffer = null;
            if (_solidLayerMaterial != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(_solidLayerMaterial);
                else DestroyImmediate(_solidLayerMaterial);
            }
            _solidLayerMaterial = null;
            _layerProperties = null;
            _target?.Dispose();
            _target = null;
            TextureChanged?.Invoke(null);
        }

        public void RenderNow()
        {
            EnsureInitialized();
            var texture = EnsureTarget();
            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(texture);
            if (composition == null)
            {
                _commandBuffer.ClearRenderTarget(false, true, solidColor);
            }
            else
            {
                _commandBuffer.ClearRenderTarget(false, true, Color.clear);
                DrawSolidLayers();
            }
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        private void DrawSolidLayers()
        {
            EnsureSolidLayerMaterial();
            if (_solidLayerMaterial == null) return;

            foreach (var layer in composition.Data.layers)
            {
                if (layer.type != LayerType.Solid || !composition.IsRendered(layer.id)) continue;
                var color = layer.solidColor;
                color.a *= Mathf.Clamp01(layer.transform?.opacity ?? 1f);
                _layerProperties.Clear();
                _layerProperties.SetColor(LayerColorId, color);
                _commandBuffer.DrawProcedural(
                    Matrix4x4.identity,
                    _solidLayerMaterial,
                    0,
                    MeshTopology.Triangles,
                    3,
                    1,
                    _layerProperties);
            }
        }

        private RenderTexture EnsureTarget()
        {
            EnsureInitialized();
            var previous = _target.Texture;
            var current = _target.GetOrCreate(
                settings.CreateOutputDescriptor(),
                settings.filterMode,
                TextureWrapMode.Clamp);

            if (current != previous) TextureChanged?.Invoke(current);
            return current;
        }

        private void EnsureInitialized()
        {
            _target ??= new OwnedRenderTexture("VJ Final Output");
            _commandBuffer ??= new CommandBuffer { name = "VJ Solid Composite" };
            _layerProperties ??= new MaterialPropertyBlock();
        }

        private void EnsureSolidLayerMaterial()
        {
            if (_solidLayerMaterial != null) return;
            if (solidLayerShader == null)
                solidLayerShader = Shader.Find("Hidden/Qoooo/VJ/SolidLayerComposite");
            if (solidLayerShader != null)
                _solidLayerMaterial = new Material(solidLayerShader) { hideFlags = HideFlags.HideAndDontSave };
        }
    }
}
