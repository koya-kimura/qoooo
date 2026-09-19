using Klak.Syphon;
using PrefsGUI;
using qoooo.Rendering.Composition;
using UnityEngine;

namespace qoooo.Integration.Syphon
{
    public class SyphonController : MonoBehaviour
    {
        [SerializeField] private SyphonServer _syphonServer;
        [SerializeField] private TextureCompositor _compositor;

        private readonly PrefsString _syphonName = new("syphonName", "qoooo");

        private void Start()
        {
            ApplySyphonName();
        }

        private void Update()
        {
            if (_compositor == null) return;

            var texture = _compositor.Texture;
            if (_syphonServer.SourceTexture != texture) _syphonServer.SourceTexture = texture;
        }

        private void ApplySyphonName() 
        {
            var syphonName = _syphonName.Get().Trim();
            if (string.IsNullOrEmpty(syphonName)) return;

            _syphonServer.ServerName = syphonName;
        }
    }
}
