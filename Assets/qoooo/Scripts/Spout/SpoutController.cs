using Klak.Spout;
using qoooo.Scripts.View;
using RosettaUI;
using UnityEngine;

namespace qoooo.Scripts.Spout
{
    public class SpoutController : MonoBehaviour, IUiTarget
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

        public Element CreateElement()
        {
            return UI.Column(
                UI.Button("Apply Address", Test)
                );
        }

        private void Test()
        {
            Debug.Log("test");
        }
    }
}
