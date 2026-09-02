using NUnit.Framework;
using Qoooo.VJ.Composition;
using UnityEngine;

namespace Qoooo.VJ.Tests
{
    public sealed class FinalCompositeRendererTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null) Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void RenderNow_ClearsFinalTextureToSolidColor()
        {
            _gameObject = new GameObject("Composite Test");
            _gameObject.SetActive(false);
            var renderer = _gameObject.AddComponent<FinalCompositeRenderer>();
            renderer.Settings.outputWidth = 16;
            renderer.Settings.outputHeight = 16;
            renderer.SolidColor = new Color(0.25f, 0.5f, 0.75f, 1f);
            _gameObject.SetActive(true);

            renderer.RenderNow();
            var actual = ReadCenterPixel(renderer.FinalTexture);
            var expected = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? renderer.SolidColor.gamma
                : renderer.SolidColor;

            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.02f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.02f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.02f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.02f));
        }

        [Test]
        public void RenderNow_RecreatesFinalTextureAfterResolutionChange()
        {
            _gameObject = new GameObject("Composite Test");
            var renderer = _gameObject.AddComponent<FinalCompositeRenderer>();
            var first = renderer.FinalTexture;
            renderer.Settings.outputWidth = 320;
            renderer.Settings.outputHeight = 180;

            renderer.RenderNow();

            Assert.That(renderer.FinalTexture, Is.Not.SameAs(first));
            Assert.That(renderer.FinalTexture.width, Is.EqualTo(320));
            Assert.That(renderer.FinalTexture.height, Is.EqualTo(180));
        }

        private static Color ReadCenterPixel(RenderTexture source)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                RenderTexture.active = source;
                texture.ReadPixels(new Rect(source.width / 2f, source.height / 2f, 1, 1), 0, 0);
                texture.Apply();
                return texture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
            }
        }
    }
}
