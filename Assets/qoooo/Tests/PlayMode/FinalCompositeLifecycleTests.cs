using System.Collections;
using NUnit.Framework;
using Qoooo.VJ.Application;
using Qoooo.VJ.Composition;
using Qoooo.VJ.UI;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

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
            gameObject.AddComponent<VjPreferencesController>();
            gameObject.AddComponent<VjRenderLoop>();
            var renderer = gameObject.GetComponent<FinalCompositeRenderer>();
            gameObject.AddComponent<VjPreviewPresenter>().Source = renderer;
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
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            Assert.That(controlPanel.IsOutputWindowOpen, Is.True);

            var previewRoot = gameObject.transform.Find("VJ Preview");
            Assert.That(previewRoot, Is.Not.Null);
            var previewDocument = previewRoot.GetComponent<UIDocument>();
            Assert.That(
                document.panelSettings.sortingOrder,
                Is.GreaterThan(previewDocument.panelSettings.sortingOrder));

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
