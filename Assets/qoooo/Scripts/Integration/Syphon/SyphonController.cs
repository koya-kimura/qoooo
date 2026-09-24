using Klak.Syphon;
using PrefsGUI;
using qoooo.Rendering.Composition;
using UnityEngine;
using UnityEngine.Serialization;

namespace qoooo.Integration.Syphon
{
    // 最終出力textureをSyphonへ公開する。composition stateは変更も所有もしない。
    public class SyphonController : MonoBehaviour
    {
        [SerializeField] private SyphonServer _syphonServer;
        [SerializeField, FormerlySerializedAs("_compositor")] private MonoBehaviour _sourceComponent;

        private readonly PrefsString _syphonName = new("syphonName", "qoooo");

        private void Start()
        {
            ApplySyphonName();
        }

        private void Update()
        {
            if (_sourceComponent is not ITextureContainer source) return;

            var texture = source.Texture;
            if (_syphonServer.SourceTexture != texture) _syphonServer.SourceTexture = texture;
        }

        private void OnValidate()
        {
            if (_sourceComponent != null && _sourceComponent is not ITextureContainer)
                Debug.LogError("Source Component must implement ITextureContainer.", this);
        }

        private void ApplySyphonName() 
        {
            var syphonName = _syphonName.Get().Trim();
            if (string.IsNullOrEmpty(syphonName)) return;

            _syphonServer.ServerName = syphonName;
        }
    }
}
