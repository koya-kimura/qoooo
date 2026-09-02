using Qoooo.VJ.Composition;
using UnityEngine;

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class VjPreviewPresenter : MonoBehaviour
    {
        [SerializeField] private FinalCompositeRenderer source;
        [SerializeField, Range(0f, 0.25f)] private float margin = 0.03f;

        public FinalCompositeRenderer Source
        {
            get => source;
            set => source = value;
        }

        private void OnGUI()
        {
            var texture = source != null ? source.FinalTexture : null;
            if (texture == null || Event.current.type != EventType.Repaint) return;

            var available = new Rect(
                Screen.width * margin,
                Screen.height * margin,
                Screen.width * (1f - margin * 2f),
                Screen.height * (1f - margin * 2f));
            var target = Fit(available, texture.width / (float)texture.height);

            GUI.DrawTexture(available, Texture2D.blackTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(target, texture, ScaleMode.StretchToFill, false);
        }

        private static Rect Fit(Rect available, float aspect)
        {
            var width = available.width;
            var height = width / aspect;
            if (height > available.height)
            {
                height = available.height;
                width = height * aspect;
            }

            return new Rect(
                available.x + (available.width - width) * 0.5f,
                available.y + (available.height - height) * 0.5f,
                width,
                height);
        }
    }
}
