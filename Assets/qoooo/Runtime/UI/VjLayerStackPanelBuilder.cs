using System.Collections.Generic;
using Qoooo.VJ.Composition;
using Qoooo.VJ.Model;
using RosettaUI;
using UnityEngine;
using RUI = RosettaUI.UI;

namespace Qoooo.VJ.UI
{
    internal sealed class VjLayerStackPanelBuilder
    {
        private readonly CompositionController _controller;

        public WindowElement Window { get; }

        public VjLayerStackPanelBuilder(CompositionController controller)
        {
            _controller = controller;
            Window = RUI.Window(
                    "Layer Stack",
                    RUI.DynamicElementOnStatusChanged(
                        () => _controller.Revision,
                        _ => BuildContents()))
                .SetPosition(new Vector2(24f, 112f));
        }

        private Element BuildContents()
        {
            var elements = new List<Element>
            {
                RUI.Row(
                    RUI.Button("+ Solid", () => _controller.AddLayer(LayerType.Solid)),
                    RUI.Button("+ Camera", () => _controller.AddLayer(LayerType.Camera)))
            };

            for (var index = _controller.Data.layers.Count - 1; index >= 0; index--)
            {
                var layer = _controller.Data.layers[index];
                var capturedIndex = index;
                elements.Add(RUI.Row(
                    RUI.Button(layer.visible ? "Eye" : "Hidden",
                        () => _controller.SetVisible(layer.id, !layer.visible)),
                    RUI.Button(layer.solo ? "Solo*" : "Solo",
                        () => _controller.SetSolo(layer.id, !layer.solo)),
                    RUI.Button(layer.locked ? "Unlock" : "Lock",
                        () => _controller.SetLocked(layer.id, !layer.locked)),
                    RUI.Button(
                        _controller.SelectedLayerId == layer.id ? $"> {layer.name}" : layer.name,
                        () => _controller.SelectLayer(layer.id)),
                    RUI.Button("Up", () => _controller.MoveLayer(layer.id, capturedIndex + 1)),
                    RUI.Button("Down", () => _controller.MoveLayer(layer.id, capturedIndex - 1))));
            }

            var selected = _controller.SelectedLayer;
            if (selected != null)
            {
                elements.Add(RUI.Row(
                    RUI.Button("Duplicate", () => _controller.DuplicateLayer(selected.id)),
                    RUI.Button("Delete", () => _controller.DeleteLayer(selected.id))));
                elements.Add(BuildInspector(selected));
            }

            return RUI.Column(elements);
        }

        private Element BuildInspector(LayerData layer)
        {
            var fields = new List<Element>
            {
                RUI.Field("Name", () => layer.name,
                    value => _controller.RenameLayer(layer.id, value))
            };

            if (layer.type == LayerType.Solid)
            {
                fields.Add(RUI.Field("Color", () => layer.solidColor,
                    value => _controller.SetSolidColor(layer.id, value)));
            }
            else if (layer.type == LayerType.Camera)
            {
                fields.Add(RUI.Field("Source ID", () => layer.sourceId,
                    value => _controller.SetSourceId(layer.id, value)));
            }

            return RUI.Fold("Inspector", RUI.Column(fields)).Open();
        }
    }
}
