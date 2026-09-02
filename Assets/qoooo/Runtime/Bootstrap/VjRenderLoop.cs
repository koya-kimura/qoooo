using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using UnityEngine;

namespace Qoooo.VJ.Application
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FinalCompositeRenderer))]
    [RequireComponent(typeof(TextureOutputController))]
    public sealed class VjRenderLoop : MonoBehaviour
    {
        [SerializeField] private FinalCompositeRenderer compositor;
        [SerializeField] private TextureOutputController output;

        private void Awake()
        {
            if (compositor == null) compositor = GetComponent<FinalCompositeRenderer>();
            if (output == null) output = GetComponent<TextureOutputController>();
        }

        private void LateUpdate()
        {
            compositor.RenderNow();
            output.Publish(compositor.FinalTexture);
        }
    }
}
