using NUnit.Framework;
using Qoooo.VJ.Composition;
using Qoooo.VJ.Model;
using UnityEngine;

namespace Qoooo.VJ.Tests
{
    public sealed class CompositionControllerTests
    {
        private GameObject _owner;
        private CompositionController _controller;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Composition Controller Test");
            _controller = _owner.AddComponent<CompositionController>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_owner);

        [Test]
        public void AddLayer_AssignsUniqueIdsAndSelectsLatestLayer()
        {
            var first = _controller.AddLayer(LayerType.Solid);
            var second = _controller.AddLayer(LayerType.Camera);

            Assert.That(first.id, Is.Not.Empty);
            Assert.That(second.id, Is.Not.EqualTo(first.id));
            Assert.That(_controller.SelectedLayer, Is.SameAs(second));
            Assert.That(first.name, Is.EqualTo("Solid 01"));
        }

        [Test]
        public void DuplicateLayer_DeepCopiesMutableDataAndAssignsNewId()
        {
            var original = _controller.AddLayer(LayerType.Camera, "Camera Hero");
            original.sourceId = "camera-01";
            original.transform.position = new Vector2(0.25f, -0.5f);

            var duplicate = _controller.DuplicateLayer(original.id);

            Assert.That(duplicate.id, Is.Not.EqualTo(original.id));
            Assert.That(duplicate.sourceId, Is.EqualTo(original.sourceId));
            Assert.That(duplicate.transform, Is.Not.SameAs(original.transform));
            duplicate.transform.position = Vector2.zero;
            Assert.That(original.transform.position, Is.EqualTo(new Vector2(0.25f, -0.5f)));
        }

        [Test]
        public void MoveLayer_ClampsDestinationAndPreservesLayerIdentity()
        {
            var first = _controller.AddLayer(LayerType.Solid, "First");
            _controller.AddLayer(LayerType.Solid, "Second");
            _controller.AddLayer(LayerType.Solid, "Third");

            Assert.That(_controller.MoveLayer(first.id, 99), Is.True);
            Assert.That(_controller.Data.layers[2], Is.SameAs(first));
        }

        [Test]
        public void DeleteSelectedLayer_SelectsNearestRemainingLayer()
        {
            var first = _controller.AddLayer(LayerType.Solid, "First");
            var second = _controller.AddLayer(LayerType.Solid, "Second");

            Assert.That(_controller.DeleteLayer(second.id), Is.True);
            Assert.That(_controller.SelectedLayer, Is.SameAs(first));
        }

        [Test]
        public void SoloRule_RendersOnlyVisibleSoloLayers()
        {
            var normal = _controller.AddLayer(LayerType.Solid, "Normal");
            var solo = _controller.AddLayer(LayerType.Solid, "Solo");
            _controller.SetSolo(solo.id, true);

            Assert.That(_controller.IsRendered(normal.id), Is.False);
            Assert.That(_controller.IsRendered(solo.id), Is.True);
            _controller.SetVisible(solo.id, false);
            Assert.That(_controller.IsRendered(normal.id), Is.True);
            Assert.That(_controller.IsRendered(solo.id), Is.False);
        }

        [Test]
        public void LockedLayer_RejectsMutatingCommandsUntilUnlocked()
        {
            var layer = _controller.AddLayer(LayerType.Solid, "Locked");
            _controller.SetLocked(layer.id, true);

            Assert.That(_controller.RenameLayer(layer.id, "Changed"), Is.False);
            Assert.That(_controller.SetVisible(layer.id, false), Is.False);
            Assert.That(_controller.DuplicateLayer(layer.id), Is.Null);
            Assert.That(_controller.DeleteLayer(layer.id), Is.False);
            Assert.That(layer.name, Is.EqualTo("Locked"));
            Assert.That(layer.visible, Is.True);
        }

        [Test]
        public void FieldCommands_UpdateThroughControllerAndAdvanceRevision()
        {
            var solid = _controller.AddLayer(LayerType.Solid);
            var revision = _controller.Revision;

            Assert.That(_controller.SetSolidColor(solid.id, Color.magenta), Is.True);
            Assert.That(_controller.RenameLayer(solid.id, "Backdrop"), Is.True);
            Assert.That(solid.solidColor, Is.EqualTo(Color.magenta));
            Assert.That(solid.name, Is.EqualTo("Backdrop"));
            Assert.That(_controller.Revision, Is.EqualTo(revision + 2));
        }
    }
}
