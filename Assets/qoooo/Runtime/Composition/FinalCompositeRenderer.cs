using System;
using Qoooo.VJ;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qoooo.VJ.Composition
{
    [DisallowMultipleComponent]
    public sealed class FinalCompositeRenderer : MonoBehaviour
    {
        [SerializeField] private VjRuntimeSettings settings = new();
        [SerializeField] private Color solidColor = new(0.06f, 0.06f, 0.08f, 1f);

        private OwnedRenderTexture _target;
        private CommandBuffer _commandBuffer;

        public event Action<RenderTexture> TextureChanged;

        public VjRuntimeSettings Settings => settings;
        public RenderTexture FinalTexture => _target?.Texture;

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

        private void LateUpdate() => RenderNow();

        private void OnDisable()
        {
            _commandBuffer?.Release();
            _commandBuffer = null;
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
            _commandBuffer.ClearRenderTarget(false, true, solidColor);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
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
        }
    }
}
