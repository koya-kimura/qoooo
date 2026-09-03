using NUnit.Framework;
using Qoooo.VJ.Application;
using Qoooo.VJ.Composition;
using Qoooo.VJ.Output;
using Qoooo.VJ.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnitySimpleContainer;

namespace Qoooo.VJ.Tests
{
    public sealed class VjRuntimeSceneTests
    {
        [Test]
        public void Trip26_SeparatesRuntimeResponsibilitiesIntoChildObjects()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/qoooo/Scenes/Trip26.unity",
                OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            var runtime = GameObject.Find("VJ Runtime");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.GetComponents<MonoBehaviour>(), Is.Empty);

            AssertComponent<FinalCompositeRenderer>(runtime, "Composition");
            AssertComponent<CompositionController>(runtime, "Composition");
            AssertComponent<VjRenderLoop>(runtime, "Composition");
            AssertComponent<VjLayerStackUiTarget>(runtime, "Composition");
            AssertComponent<TextureOutputController>(runtime, "Output");
            AssertComponent<VjOutputUiTarget>(runtime, "Output");
            AssertComponent<VjPreviewPresenter>(runtime, "UI");
            AssertComponent<UIBuilder>(runtime, "UI");
            AssertComponent<VjOutputSettingsController>(runtime, "Preferences");
            AssertComponent<VjPreferencesController>(runtime, "Preferences");
            Assert.That(Object.FindFirstObjectByType<SceneContainer>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ProjectContainer>(), Is.Not.Null);
        }

        private static void AssertComponent<T>(GameObject root, string childName)
            where T : Component
        {
            var child = root.transform.Find(childName);
            Assert.That(child, Is.Not.Null, $"Missing VJ Runtime/{childName}");
            Assert.That(child.GetComponent<T>(), Is.Not.Null);
        }
    }
}
