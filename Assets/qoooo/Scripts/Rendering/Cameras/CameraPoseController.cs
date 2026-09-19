using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Cameras
{
    [Serializable]
    public class CameraPose
    {
        [SerializeField] private string _name = "Pose";
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private float _fieldOfView = 60f;

        public string Name => _name;
        public Vector3 Position => _position;
        public Vector3 Rotation => _rotation;
        public float FieldOfView => _fieldOfView;
    }

    [RequireComponent(typeof(Camera))]
    public class CameraPoseController : MonoBehaviour
    {
        [SerializeField] private List<CameraPose> _poses = new();
        [SerializeField] private int _selectedIndex;

        private Camera _camera;

        public int SelectedIndex => _selectedIndex;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            Apply();
        }

        public void Select(int index)
        {
            if (_poses.Count == 0) return;

            _selectedIndex = Mathf.Clamp(index, 0, _poses.Count - 1);
            Apply();
        }

        private void Apply()
        {
            if (_poses.Count == 0) return;
            if (_camera == null) _camera = GetComponent<Camera>();

            var pose = _poses[Mathf.Clamp(_selectedIndex, 0, _poses.Count - 1)];
            transform.localPosition = pose.Position;
            transform.localRotation = Quaternion.Euler(pose.Rotation);
            _camera.fieldOfView = pose.FieldOfView;
        }
    }
}
