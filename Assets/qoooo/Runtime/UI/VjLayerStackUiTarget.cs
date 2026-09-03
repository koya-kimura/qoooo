using Qoooo.VJ.Composition;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;
using RUI = RosettaUI.UI;

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class VjLayerStackUiTarget : MonoBehaviour, IUiTarget
    {
        private Element _rootUI;

        public int Order => 200;
        public Element RootUI => _rootUI ??= RUI.WindowLauncher("Layers", Window);
        public WindowElement Window { get; private set; }

        [Inject]
        public void Construct(CompositionController composition)
        {
            Window ??= new VjLayerStackPanelBuilder(composition).Window;
        }

        public void Initialize(CompositionController composition)
            => Construct(composition);
    }
}
