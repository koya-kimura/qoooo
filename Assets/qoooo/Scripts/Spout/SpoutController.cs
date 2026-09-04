using Klak.Spout;
using UnityEngine;

namespace qoooo.Scripts.Spout
{
    public class SpoutController : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] SpoutSender _spoutSender;
        [SerializeField] Vector2Int _resolution;

        private RenderTexture _output;

        private void Start()
        {
            _output = new RenderTexture(_resolution.x, _resolution.y, 0);
            _camera.targetTexture = _output;
            _spoutSender.sourceTexture = _output;
        }
    }
}
