using NUnit.Framework;
using UnityEngine;

namespace Qoooo.VJ.Tests
{
    public sealed class OwnedRenderTextureTests
    {
        [Test]
        public void GetOrCreate_ReusesTexture_WhenDescriptorIsUnchanged()
        {
            using var owner = new OwnedRenderTexture("Test RT");
            var descriptor = new RenderTextureDescriptor(64, 32, RenderTextureFormat.ARGB32, 0);

            var first = owner.GetOrCreate(descriptor);
            var second = owner.GetOrCreate(descriptor);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.IsCreated(), Is.True);
        }

        [Test]
        public void GetOrCreate_RecreatesTexture_WhenSizeChanges()
        {
            using var owner = new OwnedRenderTexture("Test RT");
            var first = owner.GetOrCreate(
                new RenderTextureDescriptor(64, 32, RenderTextureFormat.ARGB32, 0));
            var second = owner.GetOrCreate(
                new RenderTextureDescriptor(128, 32, RenderTextureFormat.ARGB32, 0));

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.width, Is.EqualTo(128));
        }

        [Test]
        public void Dispose_ReleasesOwnedTexture()
        {
            var owner = new OwnedRenderTexture("Test RT");
            var texture = owner.GetOrCreate(
                new RenderTextureDescriptor(16, 16, RenderTextureFormat.ARGB32, 0));

            owner.Dispose();

            Assert.That(owner.Texture, Is.Null);
            Assert.That(texture == null, Is.True);
        }
    }
}
