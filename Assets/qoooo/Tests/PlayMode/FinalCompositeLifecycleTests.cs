using System.Collections;
using NUnit.Framework;
using Qoooo.VJ.Application;
using Qoooo.VJ.Composition;
using Qoooo.VJ.UI;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Qoooo.VJ.Tests
{
    public sealed class FinalCompositeLifecycleTests
    {
        [UnityTest]
        public IEnumerator Renderer_CreatesResizesAndReleasesFinalTexture()
        {
            var gameObject = new GameObject("Composite Lifecycle Test");
            gameObject.SetActive(false);
            var controlPanel = gameObject.AddComponent<VjControlPanel>();
#if UNITY_EDITOR
            var rosettaPanelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Packages/ga.fuquna.rosettaui/UIToolkit/Runtime/Settings/RosettaUI_DefaultPanelSettings.asset");
            controlPanel.PanelSettings = rosettaPanelSettings;
#endif
            var renderer = gameObject.AddComponent<FinalCompositeRenderer>();
            var output = gameObject.AddComponent<Qoooo.VJ.Output.TextureOutputController>();
            var outputSettings = gameObject.AddComponent<VjOutputSettingsController>();
            var composition = gameObject.AddComponent<CompositionController>();
            var preferences = gameObject.AddComponent<VjPreferencesController>();
            var renderLoop = gameObject.AddComponent<VjRenderLoop>();
            outputSettings.Compositor = renderer;
            outputSettings.Output = output;
            preferences.OutputSettings = outputSettings;
            renderLoop.Compositor = renderer;
            renderLoop.Output = output;
            controlPanel.Initialize(outputSettings, preferences);
            controlPanel.Composition = composition;
            var preview = gameObject.AddComponent<VjPreviewPresenter>();
            preview.Source = renderer;
#if UNITY_EDITOR
            preview.PanelSettings = rosettaPanelSettings;
#endif
            renderer.Settings.outputWidth = 64;
            renderer.Settings.outputHeight = 32;
            gameObject.SetActive(true);

            yield return null;

            var first = renderer.FinalTexture;
            Assert.That(first, Is.Not.Null);
            Assert.That(first.IsCreated(), Is.True);
            Assert.That(first.width, Is.EqualTo(64));
            Assert.That(first.height, Is.EqualTo(32));

            var uiRoot = gameObject.transform.Find("VJ RosettaUI");
            Assert.That(uiRoot, Is.Not.Null);
            Assert.That(uiRoot.gameObject.activeSelf, Is.True);
            var document = uiRoot.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.panelSettings, Is.Not.Null);
            Assert.That(document.panelSettings.name, Is.EqualTo("RosettaUI_DefaultPanelSettings"));
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            Assert.That(controlPanel.IsOutputWindowOpen, Is.False);
            Assert.That(controlPanel.IsLayerStackWindowOpen, Is.False);
            Assert.That(controlPanel.IsLauncherWindowOpen, Is.False);
            Assert.That(controlPanel.IsVisible, Is.True);
            controlPanel.ToggleVisible();
            Assert.That(controlPanel.IsVisible, Is.False);
            Assert.That(document.rootVisualElement.style.display.value, Is.EqualTo(DisplayStyle.None));
            controlPanel.ToggleVisible();
            Assert.That(controlPanel.IsVisible, Is.True);

            var previewRoot = gameObject.transform.Find("VJ Preview");
            Assert.That(previewRoot, Is.Not.Null);
            var previewDocument = previewRoot.GetComponent<UIDocument>();
            Assert.That(
                document.sortingOrder,
                Is.GreaterThan(previewDocument.sortingOrder));

            renderer.Settings.outputWidth = 128;
            yield return null;

            var second = renderer.FinalTexture;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.width, Is.EqualTo(128));

            gameObject.SetActive(false);
            Assert.That(renderer.FinalTexture, Is.Null);
            Assert.That(second.IsCreated(), Is.False);

            Object.Destroy(gameObject);
        }
    }
}
