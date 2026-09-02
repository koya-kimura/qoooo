using System.Collections;
using NUnit.Framework;
using Qoooo.VJ.Application;
using Qoooo.VJ.Composition;
using UnityEngine;
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
            gameObject.AddComponent<VjApplication>();
            var renderer = gameObject.GetComponent<FinalCompositeRenderer>();
            renderer.Settings.outputWidth = 64;
            renderer.Settings.outputHeight = 32;
            gameObject.SetActive(true);

            yield return null;

            var first = renderer.FinalTexture;
            Assert.That(first, Is.Not.Null);
            Assert.That(first.IsCreated(), Is.True);
            Assert.That(first.width, Is.EqualTo(64));
            Assert.That(first.height, Is.EqualTo(32));

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
