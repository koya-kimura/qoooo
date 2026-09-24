using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Rendering.Models;
using Unity.Cinemachine;
using UnityEngine;

namespace qoooo.Rendering.Cameras
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-9100)]
    // Owns the camera-pose Parameter and applies selected poses independently of model animation.
    public class CameraPoseController : MonoBehaviour, IMidiParameterContainer
    {
        private static readonly string[] PoseNames = { "Front", "Orbit", "Low Sweep", "Flyby" };

        [SerializeField] private RadioParameter _cameraPose = new(
            "model.camera-pose",
            "Model Camera Pose",
            PoseNames);

        [SerializeField] private CinemachineCamera _front;
        [SerializeField] private CinemachineCamera _close;
        [SerializeField] private CinemachineCamera _lowAngle;
        [SerializeField] private CinemachineCamera _extremeClose;

        private ModelArrangementController _arrangement;
        private int _activePose = -1;

        private void Awake()
        {
            // Sceneにserializedされた旧3-option設定を、stable indexを保ったまま拡張する。
            _cameraPose.ReplaceOptions(PoseNames);
            _arrangement = FindFirstObjectByType<ModelArrangementController>();
        }

        private void Start()
        {
            ApplyPose(_cameraPose.Value);
        }

        private void Update()
        {
            if (_activePose != _cameraPose.Value)
                ApplyPose(_cameraPose.Value);

            AnimatePose(_activePose);
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
            SetPriority(_close, selectedIndex == 1 || selectedIndex == 3 && _extremeClose == null);
            SetPriority(_lowAngle, selectedIndex == 2);
            SetPriority(_extremeClose, selectedIndex == 3);
            _activePose = selectedIndex;
        }

        private void AnimatePose(int poseIndex)
        {
            if (poseIndex == 0) return;

            var target = GetModelCenter();
            var time = Time.time;
            switch (poseIndex)
            {
                case 1:
                    var orbitPhase = time * 0.28f;
                    SetShot(_close, target, new Vector3(
                        Mathf.Sin(orbitPhase) * 0.85f,
                        0.22f + Mathf.Sin(orbitPhase * 0.7f) * 0.14f,
                        -Mathf.Cos(orbitPhase) * 0.85f), 46f);
                    break;
                case 2:
                    var sweepPhase = time * 0.38f;
                    SetShot(_lowAngle, target, new Vector3(
                        Mathf.Sin(sweepPhase) * 0.72f,
                        -0.34f + Mathf.Sin(sweepPhase * 0.6f) * 0.06f,
                        -0.62f + Mathf.Cos(sweepPhase) * 0.08f), 48f);
                    break;
                case 3:
                    var flybyPhase = time * 0.52f;
                    var flybyCamera = _extremeClose != null ? _extremeClose : _close;
                    var side = Mathf.Sin(flybyPhase);
                    SetShot(flybyCamera, target, new Vector3(
                        side * 0.95f,
                        Mathf.Sin(flybyPhase * 0.5f) * 0.2f,
                        -Mathf.Lerp(0.22f, 0.82f, Mathf.Abs(side))), 64f,
                        Mathf.Sin(flybyPhase * 0.7f) * 10f);
                    break;
            }
        }

        private Vector3 GetModelCenter()
        {
            if (_arrangement == null || _arrangement.ActiveSlots.Count == 0)
                return transform.position + transform.forward;

            var center = Vector3.zero;
            var count = 0;
            foreach (var slot in _arrangement.ActiveSlots)
            {
                if (slot == null || !slot.activeInHierarchy) continue;
                center += slot.transform.position;
                count++;
            }

            return count > 0 ? center / count : transform.position + transform.forward;
        }

        private static void SetShot(
            CinemachineCamera camera,
            Vector3 target,
            Vector3 offset,
            float fieldOfView,
            float roll = 0f)
        {
            if (camera == null) return;

            var cameraTransform = camera.transform;
            cameraTransform.position = target + offset;
            cameraTransform.rotation = Quaternion.LookRotation(target - cameraTransform.position, Vector3.up)
                * Quaternion.AngleAxis(roll, Vector3.forward);
            camera.Lens.FieldOfView = fieldOfView;
        }

        private static void SetPriority(CinemachineCamera camera, bool isSelected)
        {
            if (camera != null)
                camera.Priority = isSelected ? 10 : 0;
        }
    }
}
