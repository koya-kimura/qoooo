using System;
using Qoooo.VJ.Model;
using UnityEngine;

namespace Qoooo.VJ.Composition
{
    [DisallowMultipleComponent]
    public sealed class CompositionController : MonoBehaviour
    {
        [SerializeField] private CompositionData data = new();
        [SerializeField] private string selectedLayerId;

        public event Action Changed;

        public CompositionData Data => data;
        public string SelectedLayerId => selectedLayerId;
        public LayerData SelectedLayer => FindLayer(selectedLayerId);

        public LayerData AddLayer(LayerType type, string name = null)
        {
            var layer = new LayerData
            {
                id = CreateId(),
                name = string.IsNullOrWhiteSpace(name) ? CreateDefaultName(type) : name.Trim(),
                type = type
            };
            data.layers.Add(layer);
            selectedLayerId = layer.id;
            Changed?.Invoke();
            return layer;
        }

        public bool DeleteLayer(string id)
        {
            var index = FindIndex(id);
            if (index < 0 || data.layers[index].locked) return false;

            data.layers.RemoveAt(index);
            if (selectedLayerId == id)
            {
                var next = Mathf.Clamp(index, 0, data.layers.Count - 1);
                selectedLayerId = data.layers.Count == 0 ? null : data.layers[next].id;
            }
            Changed?.Invoke();
            return true;
        }

        public LayerData DuplicateLayer(string id)
        {
            var index = FindIndex(id);
            if (index < 0 || data.layers[index].locked) return null;

            var duplicate = data.layers[index].Copy(CreateId());
            duplicate.name = $"{duplicate.name} Copy";
            duplicate.locked = false;
            data.layers.Insert(index + 1, duplicate);
            selectedLayerId = duplicate.id;
            Changed?.Invoke();
            return duplicate;
        }

        public bool MoveLayer(string id, int destinationIndex)
        {
            var sourceIndex = FindIndex(id);
            if (sourceIndex < 0 || data.layers[sourceIndex].locked) return false;

            var boundedIndex = Mathf.Clamp(destinationIndex, 0, data.layers.Count - 1);
            if (sourceIndex == boundedIndex) return false;

            var layer = data.layers[sourceIndex];
            data.layers.RemoveAt(sourceIndex);
            data.layers.Insert(boundedIndex, layer);
            Changed?.Invoke();
            return true;
        }

        public bool SelectLayer(string id)
        {
            if (FindIndex(id) < 0 || selectedLayerId == id) return false;
            selectedLayerId = id;
            Changed?.Invoke();
            return true;
        }

        public bool SetVisible(string id, bool value) => MutateUnlocked(id, layer => layer.visible = value);
        public bool SetSolo(string id, bool value) => MutateUnlocked(id, layer => layer.solo = value);

        public bool SetLocked(string id, bool value)
        {
            var layer = FindLayer(id);
            if (layer == null || layer.locked == value) return false;
            layer.locked = value;
            Changed?.Invoke();
            return true;
        }

        public bool RenameLayer(string id, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return MutateUnlocked(id, layer => layer.name = value.Trim());
        }

        public bool IsRendered(string id)
        {
            var layer = FindLayer(id);
            if (layer == null || !layer.visible) return false;
            var hasSolo = data.layers.Exists(candidate => candidate.solo && candidate.visible);
            return !hasSolo || layer.solo;
        }

        public LayerData FindLayer(string id)
            => string.IsNullOrEmpty(id) ? null : data.layers.Find(layer => layer.id == id);

        private bool MutateUnlocked(string id, Action<LayerData> mutation)
        {
            var layer = FindLayer(id);
            if (layer == null || layer.locked) return false;
            mutation(layer);
            Changed?.Invoke();
            return true;
        }

        private int FindIndex(string id) => data.layers.FindIndex(layer => layer.id == id);
        private static string CreateId() => Guid.NewGuid().ToString("N");
        private string CreateDefaultName(LayerType type) => $"{type} {CountType(type) + 1:00}";

        private int CountType(LayerType type)
        {
            var count = 0;
            foreach (var layer in data.layers)
                if (layer.type == type) count++;
            return count;
        }
    }
}
