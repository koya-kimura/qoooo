using Klak.Spout;
using Klak.Syphon;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Qoooo.VJ.Output
{
    [DisallowMultipleComponent]
    public sealed class TextureOutputController : MonoBehaviour
    {
        [SerializeField] private VjOutputMode mode = VjOutputMode.Auto;
        [SerializeField] private string outputName = "qoooo VJ";
        [SerializeField] private SpoutResources spoutResources;
        [SerializeField] private SyphonResources syphonResources;

        private ITextureOutput _output;

        public VjOutputMode Mode
        {
            get => mode;
            set
            {
                if (mode == value) return;
                mode = value;
                RecreateOutput();
            }
        }

        public bool IsAvailable => _output?.IsAvailable == true;

        public string OutputName
        {
            get => outputName;
            set
            {
                var validated = string.IsNullOrWhiteSpace(value) ? "qoooo VJ" : value.Trim();
                if (outputName == validated) return;
                outputName = validated;
                RecreateOutput();
            }
        }

        private void OnEnable()
        {
            LoadEditorResources();
            RecreateOutput();
        }

        public void Publish(Texture texture)
        {
            _output?.SetTexture(texture);
        }

        private void OnDisable()
        {
            _output?.Dispose();
            _output = null;
        }

        private void RecreateOutput()
        {
            if (!isActiveAndEnabled) return;
            _output?.Dispose();
            _output = CreateOutput(ResolveMode());
        }

        private ITextureOutput CreateOutput(VjOutputMode resolvedMode)
        {
            switch (resolvedMode)
            {
                case VjOutputMode.Spout:
                    if (spoutResources != null)
                        return new SpoutTextureOutput(gameObject, outputName, spoutResources);
                    Debug.LogWarning("Spout output is disabled: SpoutResources is not assigned.", this);
                    break;
                case VjOutputMode.Syphon:
                    if (syphonResources != null)
                        return new SyphonTextureOutput(gameObject, outputName, syphonResources);
                    Debug.LogWarning("Syphon output is disabled: SyphonResources is not assigned.", this);
                    break;
            }

            return new NullTextureOutput();
        }

        private VjOutputMode ResolveMode()
        {
            if (mode != VjOutputMode.Auto) return mode;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return VjOutputMode.Spout;
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return VjOutputMode.Syphon;
#else
            return VjOutputMode.Disabled;
#endif
        }

        private void LoadEditorResources()
        {
#if UNITY_EDITOR
            if (spoutResources == null)
                spoutResources = AssetDatabase.LoadAssetAtPath<SpoutResources>(
                    "Packages/jp.keijiro.klak.spout/Editor/SpoutResources.asset");
            if (syphonResources == null)
                syphonResources = AssetDatabase.LoadAssetAtPath<SyphonResources>(
                    "Packages/jp.keijiro.klak.syphon/Internal/SyphonResources.asset");
#endif
        }

        private sealed class NullTextureOutput : ITextureOutput
        {
            public bool IsAvailable => false;
            public void SetTexture(Texture texture) { }
            public void Dispose() { }
        }

        private sealed class SpoutTextureOutput : ITextureOutput
        {
            private readonly SpoutSender _sender;
            private Texture _texture;

            public SpoutTextureOutput(GameObject owner, string name, SpoutResources resources)
            {
                _sender = owner.AddComponent<SpoutSender>();
                _sender.spoutName = name;
                _sender.captureMethod = Klak.Spout.CaptureMethod.Texture;
                _sender.keepAlpha = true;
                _sender.SetResources(resources);
            }

            public bool IsAvailable => _sender != null;
            public void SetTexture(Texture texture)
            {
                if (_sender == null || texture == _texture) return;
                _texture = texture;
                _sender.sourceTexture = texture;
            }

            public void Dispose()
            {
                if (_sender != null) Destroy(_sender);
            }
        }

        private sealed class SyphonTextureOutput : ITextureOutput
        {
            private readonly SyphonServer _server;
            private Texture _texture;

            public SyphonTextureOutput(GameObject owner, string name, SyphonResources resources)
            {
                _server = owner.AddComponent<SyphonServer>();
                _server.ServerName = name;
                _server.CaptureMethod = Klak.Syphon.CaptureMethod.Texture;
                _server.KeepAlpha = true;
                _server.Resources = resources;
            }

            public bool IsAvailable => _server != null;
            public void SetTexture(Texture texture)
            {
                if (_server == null || texture == _texture) return;
                _texture = texture;
                _server.SourceTexture = texture;
            }

            public void Dispose()
            {
                if (_server != null) Destroy(_server);
            }
        }
    }
}
