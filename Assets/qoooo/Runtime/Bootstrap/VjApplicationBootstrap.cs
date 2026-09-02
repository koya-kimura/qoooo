using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using Qoooo.VJ.UI;
using UnityEngine;

namespace Qoooo.VJ.Bootstrap
{
    public static class VjApplicationBootstrap
    {
        private const string RootName = "[VJ] Application";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<FinalCompositeRenderer>() != null) return;

            var root = new GameObject(RootName);
            Object.DontDestroyOnLoad(root);

            var compositor = root.AddComponent<FinalCompositeRenderer>();
            root.AddComponent<VjPreviewPresenter>().Source = compositor;
            root.AddComponent<TextureOutputController>().Source = compositor;
        }
    }
}
