using qoooo.View;
using RosettaUI;
using UnityEngine;

namespace qoooo.Textures
{
    public class OutputSettings : MonoBehaviour, IResolutionContainer, IUiTarget
    {
        [SerializeField] private Vector2Int _resolution = new(1920, 1080);

        public Vector2Int Resolution => new(
            Mathf.Max(1, _resolution.x),
            Mathf.Max(1, _resolution.y));

        public Element CreateElement()
        {
            return UI.Column(
                UI.Field("Resolution", () => Resolution, SetResolution)
            );
        }

        private void SetResolution(Vector2Int resolution)
        {
            _resolution = new Vector2Int(
                Mathf.Max(1, resolution.x),
                Mathf.Max(1, resolution.y));
        }

        private void OnValidate()
        {
            SetResolution(_resolution);
        }
    }
}
