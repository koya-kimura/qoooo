using System;
using UnityEngine;

namespace Qoooo.VJ.Model
{
    public enum LayerType
    {
        Camera,
        Solid
    }

    [Serializable]
    public sealed class TransformData
    {
        public Vector2 position;
        public float rotation;
        public float scale = 1f;
        public float opacity = 1f;

        public TransformData Copy() => new()
        {
            position = position,
            rotation = rotation,
            scale = scale,
            opacity = opacity
        };
    }

    [Serializable]
    public sealed class LayerData
    {
        public string id;
        public string name;
        public LayerType type;
        public bool visible = true;
        public bool solo;
        public bool locked;
        public TransformData transform = new();
        public string sourceId;
        public Color solidColor = Color.black;

        public LayerData Copy(string newId) => new()
        {
            id = newId,
            name = name,
            type = type,
            visible = visible,
            solo = solo,
            locked = locked,
            transform = transform?.Copy() ?? new TransformData(),
            sourceId = sourceId,
            solidColor = solidColor
        };
    }
}
