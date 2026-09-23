using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace qoooo.Rendering.Cameras
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-9100)]
    // Owns the camera-pose Parameter and applies selected poses independently of model animation.
    public class CameraPoseController : MonoBehaviour, IMidiParameterContainer
    {
        private static readonly string[] PoseNames = { "Front", "Close", "Low Angle", "Extreme Close" };

        [SerializeField] private RadioParameter _cameraPose = new(
            "model.camera-pose",
            "Model Camera Pose",
            PoseNames);

        [SerializeField] private CinemachineCamera _front;
        [SerializeField] private CinemachineCamera _close;
        [SerializeField] private CinemachineCamera _lowAngle;
        [SerializeField] private CinemachineCamera _extremeClose;

        private int _activePose = -1;

        private void Awake()
        {
            // Sceneにserializedされた旧3-option設定を、stable indexを保ったまま拡張する。
            _cameraPose.ReplaceOptions(PoseNames);
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

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _cameraPose; }
        }

        // カメラは配置を設定だが、カメラの動きは？配置だけの話はわかった
        private void ApplyPose(int poseIndex)
        {
            var selectedIndex = Mathf.Clamp(poseIndex, 0, PoseNames.Length - 1);
            SetPriority(_front, selectedIndex == 0);
            // 新しいScene referenceが未設定でも、選択時に全virtual cameraがstandbyになるのを避ける。
            SetPriority(_close, selectedIndex == 1 || (selectedIndex == 3 && _extremeClose == null));
            SetPriority(_lowAngle, selectedIndex == 2);
            SetPriority(_extremeClose, selectedIndex == 3);
            _activePose = selectedIndex;
        }

        private static void SetPriority(CinemachineCamera camera, bool isSelected)
        {
            if (camera != null)
                camera.Priority = isSelected ? 10 : 0;
        }
    }
}
