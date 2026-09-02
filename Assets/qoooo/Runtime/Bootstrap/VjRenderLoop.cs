using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class VjRenderLoop : MonoBehaviour
    {
        [SerializeField] private FinalCompositeRenderer compositor;
        [SerializeField] private TextureOutputController output;

        public FinalCompositeRenderer Compositor
        {
            get => compositor;
            set => compositor = value;
        }

        public TextureOutputController Output
        {
            get => output;
            set => output = value;
        }

        private void LateUpdate()
        {
            if (compositor == null || output == null) return;
            compositor.RenderNow();
            output.Publish(compositor.FinalTexture);
        }
    }
}
