using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace qoooo.Rendering.Cameras
{
    [RequireComponent(typeof(Camera))]
    public class CameraPoseController : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField]
        private RadioParameter _cameraPose = new(
            "model.camera-pose",
            "Model Camera Pose",
            new[] { "Front", "Close", "Low Angle" });

        [SerializeField] private CinemachineCamera _front;
        [SerializeField] private CinemachineCamera _close;
        [SerializeField] private CinemachineCamera _lowAngle;

        private int _activePose = -1;

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _cameraPose; }
        }

        private void Start()
        {
            ApplyPose(_cameraPose.Value);
        }

        private void Update()
        {
            if (_activePose != _cameraPose.Value)
                ApplyPose(_cameraPose.Value);
        }

        private void ApplyPose(int poseIndex)
        {
            var selectedIndex = Mathf.Clamp(poseIndex, 0, 2);
            SetPriority(_front, selectedIndex == 0);
            SetPriority(_close, selectedIndex == 1);
            SetPriority(_lowAngle, selectedIndex == 2);
            _activePose = selectedIndex;
        }

        private static void SetPriority(CinemachineCamera camera, bool isSelected)
        {
            if (camera != null)
                camera.Priority = isSelected ? 10 : 0;
        }
    }
}
