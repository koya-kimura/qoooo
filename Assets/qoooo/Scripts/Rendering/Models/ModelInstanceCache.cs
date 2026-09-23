using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    // Arrangementが決めたslot要求に対し、cloneの生成・再利用・破棄だけを所有する。
    public sealed class ModelInstanceCache
    {
        private readonly Transform _parent;
        private readonly Func<GameObject, Vector3> _sourceScale;
        private readonly Func<GameObject, Quaternion> _sourceRotation;
        private readonly List<CacheSlot> _slots = new();

        public ModelInstanceCache(
            Transform parent,
            Func<GameObject, Vector3> sourceScale,
            Func<GameObject, Quaternion> sourceRotation)
        {
            _parent = parent;
            _sourceScale = sourceScale;
            _sourceRotation = sourceRotation;
        }

        public GameObject GetOrCreate(int slotIndex, GameObject source)
        {
            // Cacheは渡されたsourceのinstance lifecycleだけを扱い、sourceの選択は行わない。
            while (_slots.Count <= slotIndex) _slots.Add(new CacheSlot());

            var slot = _slots[slotIndex];
            if (slot.Source == source && slot.Instance != null)
                return slot.Instance;

            Replace(slot, source);
            return slot.Instance;
        }

        public void Prewarm(GameObject source, int count)
        {
            if (source == null) return;

            for (var index = 0; index < count; index++)
                GetOrCreate(index, source);

            SetActiveCount(0);
        }

        public void SetActiveCount(int count)
        {
            // 通常時のclone表示状態はここだけで決める。teardown時の非表示はDestroyInstanceが担う。
            for (var index = 0; index < _slots.Count; index++)
            {
                var instance = _slots[index].Instance;
                if (instance != null) instance.SetActive(index < count);
            }
        }

        public void Dispose()
        {
            foreach (var slot in _slots)
                DestroyInstance(slot);

            _slots.Clear();
        }

        private void Replace(CacheSlot slot, GameObject source)
        {
            DestroyInstance(slot);
            slot.Source = source;
            slot.Instance = source == null ? null : UnityEngine.Object.Instantiate(source, _parent);
            if (slot.Instance == null) return;

            slot.Instance.name = $"{source.name} Arrangement Slot";
            slot.Instance.transform.localScale = _sourceScale(source);
            slot.Instance.transform.localRotation = _sourceRotation(source);
        }

        private static void DestroyInstance(CacheSlot slot)
        {
            if (slot.Instance == null) return;

            // Destroyはframe終端まで遅延するため、teardown時だけ即時に隠す。
            slot.Instance.SetActive(false);
            UnityEngine.Object.Destroy(slot.Instance);
            slot.Instance = null;
        }

        private sealed class CacheSlot
        {
            public GameObject Source;
            public GameObject Instance;
        }
    }
}