using System.Collections;
using System.Collections.Generic;
using qoooo.Foundation.Timing;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using qoooo.Rendering.Composition;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Sources
{
    public enum LiveCameraMotionMode
    {
        Still,
        Drift,
        PushIn,
        PullOut,
        Orbit,
        SlowDrift,
        FastDrift,
        SlowOrbit,
        FastOrbit,
        SlowPush,
        FastPush,
        SlowPull,
        FastPull,
        BeatPush,
        BeatPull,
        BeatOrbit,
        OffbeatPan,
        HalfTimeDrift,
        DoubleTimePan,
        BeatZoom,
        BarSweep,
        BarOrbit,
        LooseHandheld,
        NervousHandheld,
        PanSweep,
        FigureEight,
        DollyLeft,
        DollyRight,
        CloseBreathe,
        ArcLeft,
        ArcRight,
        FloatClose
    }

    public enum LiveCameraFraming
    {
        FullWide,
        Wide,
        Medium,
        Close,
        Tight,
        ExtremeClose,
        Headroom,
        LowAngle,
        LeftMedium,
        CenterMedium,
        RightMedium,
        LeftClose,
        CenterClose,
        RightClose,
        DutchLeft,
        DutchRight
    }

    public enum LiveCameraLook
    {
        Clean,
        CenterTriptych,
        MirrorSplit,
        SoftFilm,
        Punch,
        Bleach,
        Mono,
        Chromatic
    }

    [DefaultExecutionOrder(-100)]
    // Owns the external camera lifecycle and a cover-cropped texture for the compositor.
    public sealed class LiveCameraSource : MonoBehaviour,
        ITextureContainer, IParameterContainer, IMidiParameterSource, IUiTarget
    {
        private static readonly string[] MotionModeNames =
        {
            "Still", "Drift", "Push In", "Pull Out", "Orbit",
            "Slow Drift", "Fast Drift", "Slow Orbit", "Fast Orbit",
            "Slow Push", "Fast Push", "Slow Pull", "Fast Pull",
            "Beat Push", "Beat Pull", "Beat Orbit", "Offbeat Pan",
            "Half-Time Drift", "Double-Time Pan", "Beat Zoom",
            "Bar Sweep", "Bar Orbit", "Loose Handheld", "Nervous Handheld",
            "Pan Sweep", "Figure Eight", "Dolly Left", "Dolly Right",
            "Close Breathe", "Arc Left", "Arc Right", "Float Close"
        };
        private static readonly string[] FramingNames =
        {
            "Full Wide", "Wide", "Medium", "Close", "Tight", "Extreme Close", "Headroom", "Low Angle",
            "Left Medium", "Center Medium", "Right Medium", "Left Close", "Center Close", "Right Close",
            "Dutch Left", "Dutch Right"
        };
        private static readonly string[] LookNames =
        {
            "Clean", "Center 1/3 Triptych", "Mirror Split", "Soft Film",
            "Punch", "Bleach", "Mono", "Chromatic"
        };
        private static readonly FramingPreset[] FramingPresets =
        {
            new(1f, Vector2.zero, 0f),
            new(1.08f, Vector2.zero, 0f),
            new(1.2f, Vector2.zero, 0f),
            new(1.38f, Vector2.zero, 0f),
            new(1.62f, Vector2.zero, 0f),
            new(1.9f, Vector2.zero, 0f),
            new(1.25f, new Vector2(0f, 0.45f), 0f),
            new(1.3f, new Vector2(0f, -0.35f), 0f),
            new(1.22f, new Vector2(-0.62f, 0f), 0f),
            new(1.22f, Vector2.zero, 0f),
            new(1.22f, new Vector2(0.62f, 0f), 0f),
            new(1.48f, new Vector2(-0.55f, 0f), 0f),
            new(1.48f, Vector2.zero, 0f),
            new(1.48f, new Vector2(0.55f, 0f), 0f),
            new(1.28f, new Vector2(-0.18f, 0f), -4f),
            new(1.28f, new Vector2(0.18f, 0f), 4f)
        };
        private static readonly LookPreset[] LookPresets =
        {
            new(0, 1f, 1f, 0f, 0f, 0f, Vector3.one),
            new(1, 1.05f, 1.05f, 0f, 0.08f, 0f, Vector3.one),
            new(2, 1.08f, 1.1f, 0f, 0.12f, 0f, Vector3.one),
            new(0, 0.94f, 0.82f, 0.08f, 0.3f, 0.8f, new Vector3(1.06f, 1f, 0.92f)),
            new(0, 1.28f, 1.18f, -0.02f, 0.3f, 0f, Vector3.one),
            new(0, 0.82f, 0.55f, 0.16f, 0.2f, 0.15f, new Vector3(1.08f, 1.04f, 0.94f)),
            new(0, 1.12f, 0f, 0f, 0.38f, 0.05f, Vector3.one),
            new(0, 1.15f, 1.12f, 0f, 0.2f, 0f, Vector3.one, 2.5f)
        };
        private const float RetryInterval = 2f;
        private const float TransitionBeats = 0.4f;
        private const int RequestedWidth = 640;
        private const int RequestedHeight = 360;
        private const int RequestedFps = 30;

        [SerializeField] private Material _material;
        [SerializeField] private BoolParameter _enabled = new("camera.live.enabled", "Live Camera Enabled");
        [SerializeField] private BoolParameter _motionEnabled = new(
            "camera.live.motion-enabled", "Live Camera Motion Enabled", true);
        [SerializeField] private RadioParameter _motionMode = new(
            "camera.live.motion-mode", "Live Camera Motion", MotionModeNames, (int)LiveCameraMotionMode.Drift);
        [SerializeField] private RadioParameter _framing = new(
            "camera.live.framing", "Live Camera Framing", FramingNames, (int)LiveCameraFraming.Medium);
        [SerializeField] private RadioParameter _look = new(
            "camera.live.look", "Live Camera Look", LookNames);
        [SerializeField] private FloatParameter _zoom = new(
            "camera.live.zoom", "Live Camera Zoom", 1f, 2f, 1.08f);
        [SerializeField] private FloatParameter _panX = new(
            "camera.live.pan-x", "Live Camera Pan X", -1f, 1f);
        [SerializeField] private FloatParameter _panY = new(
            "camera.live.pan-y", "Live Camera Pan Y", -1f, 1f);
        [SerializeField] private FloatParameter _motionAmount = new(
            "camera.live.motion-amount", "Live Camera Motion Amount", 0f, 1f, 0.35f);
        [SerializeField] private FloatParameter _motionSpeed = new(
            "camera.live.motion-speed", "Live Camera Motion Speed", 0f, 1f, 0.15f);
        [SerializeField] private FloatParameter _opacity = new(
            "camera.live.opacity", "Live Camera Opacity", 0f, 1f, 1f);
        [SerializeField] private BoolParameter _flipX = new(
            "camera.live.flip-x", "Live Camera Flip X");

        private IResolutionContainer _resolutionContainer;
        private IBpmSource _bpmSource;
        private WebCamTexture _cameraTexture;
        private RenderTexture _output;
        private Coroutine _startRoutine;
        private string[] _deviceNames = { "Default" };
        private string _preferredDeviceName;
        private string _status = "Off";
        private float _nextRetryTime;
        private bool _outputIsClear;
        private bool _permissionWarningLogged;
        private bool _permissionDenied;
        private int _deviceRevision;
        private int _renderedMotionMode = -1;
        private int _renderedFraming = -1;
        private int _renderedLook = -1;
        private Vector3 _smoothedTransform;
        private float _smoothedRoll;
        private float _transitionStartedAt = float.NegativeInfinity;
        private bool _hasSmoothedTransform;

        public Texture Texture => _output;

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _enabled;
                yield return _motionEnabled;
                yield return _motionMode;
                yield return _framing;
                yield return _look;
                yield return _zoom;
                yield return _panX;
                yield return _panY;
                yield return _motionAmount;
                yield return _motionSpeed;
                yield return _opacity;
                yield return _flipX;
            }
        }

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer, IBpmSource bpmSource)
        {
            _resolutionContainer = resolutionContainer;
            _bpmSource = bpmSource;
        }

        private void Awake()
        {
            _motionMode.ReplaceOptions(MotionModeNames, (int)LiveCameraMotionMode.Drift);
            _framing.ReplaceOptions(FramingNames, (int)LiveCameraFraming.Medium);
            _look.ReplaceOptions(LookNames);
        }

        private void OnEnable()
        {
            EnsureOutput();
            ClearOutput();
            _hasSmoothedTransform = false;
        }

        private void Update()
        {
            EnsureOutput();
            if (!_enabled.Value)
            {
                ClearOutput();
                _status = _cameraTexture != null && _cameraTexture.isPlaying ? "Standby" : "Off";
                return;
            }

            if (_cameraTexture == null || !_cameraTexture.isPlaying)
            {
                ClearOutput();
                if (!_permissionDenied && _startRoutine == null && Time.unscaledTime >= _nextRetryTime)
                {
                    _nextRetryTime = Time.unscaledTime + RetryInterval;
                    _startRoutine = StartCoroutine(StartCamera());
                }
                return;
            }

            if (_cameraTexture.width <= 16 || _cameraTexture.height <= 16)
            {
                _status = "Waiting for video";
                ClearOutput();
                return;
            }

            EnsureOutput();
            if (!_cameraTexture.didUpdateThisFrame && !_outputIsClear) return;
            RenderCamera();
        }

        private void OnDisable()
        {
            StopCamera();
            ReleaseOutput();
        }

        private void OnDestroy()
        {
            StopCamera();
            ReleaseOutput();
        }

        public Element CreateElement() => CreateParameterElement();

        public Element CreateParameterElement()
        {
            return UI.Column(
                UI.Field("Enabled", () => _enabled.Value, value => _enabled.TrySetValue(value)),
                UI.Label(() => $"Status: {_status}"),
                UI.DynamicElementOnStatusChanged(() => _deviceRevision, _ => CreateDeviceElement()),
                UI.Fold("Motion",
                    UI.Field("Enabled", () => _motionEnabled.Value, value => _motionEnabled.TrySetValue(value)),
                    UI.Dropdown("Mode", () => _motionMode.Value, value => _motionMode.TrySetValue(value), MotionModeNames),
                    Slider("Zoom", _zoom),
                    Slider("Pan X", _panX),
                    Slider("Pan Y", _panY),
                    Slider("Amount", _motionAmount),
                    Slider("Speed", _motionSpeed)
                ).Open(),
                UI.Fold("Framing & Look",
                    UI.Dropdown("Framing", () => _framing.Value, value => _framing.TrySetValue(value), FramingNames),
                    UI.Dropdown("Look", () => _look.Value, value => _look.TrySetValue(value), LookNames)
                ).Open(),
                Slider("Opacity", _opacity),
                UI.Field("Flip X", () => _flipX.Value, value => _flipX.TrySetValue(value)));
        }

        private Element CreateDeviceElement()
        {
            return UI.Row(
                UI.Dropdown("Device", SelectedDeviceIndex, SelectDevice, _deviceNames),
                UI.Button("Refresh", RefreshDevices));
        }

        private static Element Slider(string label, FloatParameter parameter)
        {
            return UI.Slider(label, () => parameter.Value, value => parameter.TrySetValue(value),
                parameter.Min, parameter.Max);
        }

        private IEnumerator StartCamera()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                _status = "Permission unavailable";
                _permissionDenied = true;
                if (!_permissionWarningLogged)
                {
                    _permissionWarningLogged = true;
                    Debug.LogWarning("[Live Camera] Camera permission is unavailable.", this);
                }
                _startRoutine = null;
                yield break;
            }

            RefreshDevices();
            var deviceName = ResolveDeviceName();
            if (string.IsNullOrEmpty(deviceName))
            {
                _status = "No camera found";
                _startRoutine = null;
                yield break;
            }

            _cameraTexture = new WebCamTexture(deviceName, RequestedWidth, RequestedHeight, RequestedFps);
            try
            {
                _cameraTexture.Play();
                _status = $"Starting {_cameraTexture.deviceName}";
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[Live Camera] Could not start '{deviceName}': {exception.Message}", this);
                Destroy(_cameraTexture);
                _cameraTexture = null;
                _status = "Camera unavailable";
            }
            _startRoutine = null;
        }

        private void StopCamera()
        {
            if (_startRoutine != null)
            {
                StopCoroutine(_startRoutine);
                _startRoutine = null;
            }
            if (_cameraTexture == null) return;

            if (_cameraTexture.isPlaying) _cameraTexture.Stop();
            Destroy(_cameraTexture);
            _cameraTexture = null;
        }

        private void RefreshDevices()
        {
            var devices = WebCamTexture.devices;
            _deviceNames = new string[devices.Length + 1];
            _deviceNames[0] = "Default";
            for (var index = 0; index < devices.Length; index++)
                _deviceNames[index + 1] = devices[index].name;
            _deviceRevision++;
        }

        private int SelectedDeviceIndex()
        {
            if (string.IsNullOrEmpty(_preferredDeviceName)) return 0;
            for (var index = 1; index < _deviceNames.Length; index++)
                if (_deviceNames[index] == _preferredDeviceName) return index;
            return 0;
        }

        private void SelectDevice(int index)
        {
            index = Mathf.Clamp(index, 0, _deviceNames.Length - 1);
            _preferredDeviceName = index == 0 ? null : _deviceNames[index];
            StopCamera();
            _nextRetryTime = 0f;
        }

        private string ResolveDeviceName()
        {
            if (!string.IsNullOrEmpty(_preferredDeviceName))
                for (var index = 1; index < _deviceNames.Length; index++)
                    if (_deviceNames[index] == _preferredDeviceName) return _preferredDeviceName;
            return _deviceNames.Length > 1 ? _deviceNames[1] : null;
        }

        private void RenderCamera()
        {
            if (_material == null || _output == null) return;

            var rotation = NormalizeRotation(_cameraTexture.videoRotationAngle);
            var sourceWidth = rotation is 90 or 270 ? _cameraTexture.height : _cameraTexture.width;
            var sourceHeight = rotation is 90 or 270 ? _cameraTexture.width : _cameraTexture.height;
            var sourceAspect = (float)sourceWidth / Mathf.Max(1, sourceHeight);
            var outputAspect = (float)_output.width / Mathf.Max(1, _output.height);
            var uvScale = sourceAspect > outputAspect
                ? new Vector2(outputAspect / sourceAspect, 1f)
                : new Vector2(1f, sourceAspect / outputAspect);

            UpdateTransitionState();
            var framing = FramingPresets[Mathf.Clamp(_framing.Value, 0, FramingPresets.Length - 1)];
            var motion = MotionOffset(Time.unscaledTime);
            var targetTransform = new Vector3(
                _panX.Value + framing.Pan.x + motion.x,
                _panY.Value + framing.Pan.y + motion.y,
                Mathf.Max(1f, _zoom.Value * framing.Zoom + motion.z));
            var targetRoll = framing.Roll + MotionRoll(Time.unscaledTime);
            SmoothCameraTransform(targetTransform, targetRoll);

            var zoom = _smoothedTransform.z;
            uvScale /= zoom;
            var normalizedPan = Vector2.ClampMagnitude((Vector2)_smoothedTransform, 1f);
            var look = LookPresets[Mathf.Clamp(_look.Value, 0, LookPresets.Length - 1)];
            var fade = Mathf.Lerp(0.88f, 1f, TransitionProgress());

            _material.SetVector("_UvScale", uvScale);
            _material.SetVector("_Pan", normalizedPan);
            _material.SetFloat("_Opacity", _opacity.Value * fade);
            _material.SetInt("_Rotation", rotation);
            _material.SetInt("_FlipX", _flipX.Value ? 1 : 0);
            _material.SetInt("_FlipY", _cameraTexture.videoVerticallyMirrored ? 1 : 0);
            _material.SetFloat("_Roll", _smoothedRoll * Mathf.Deg2Rad);
            _material.SetInt("_LayoutMode", look.LayoutMode);
            _material.SetFloat("_Contrast", look.Contrast);
            _material.SetFloat("_Saturation", look.Saturation);
            _material.SetFloat("_Exposure", look.Exposure);
            _material.SetFloat("_Vignette", look.Vignette);
            _material.SetFloat("_Softness", look.Softness);
            _material.SetFloat("_Chromatic", look.Chromatic);
            _material.SetVector("_ColorBalance", look.ColorBalance);
            Graphics.Blit(_cameraTexture, _output, _material);
            _outputIsClear = false;
            _status = _cameraTexture.deviceName;
        }

        private Vector3 MotionOffset(float time)
        {
            if (!_motionEnabled.Value || _motionMode.Value == (int)LiveCameraMotionMode.Still)
                return Vector3.zero;

            var beat = (float)(_bpmSource?.Beat ?? time * 2d);
            var speed = Mathf.Pow(4f, _motionSpeed.Value - 0.15f);
            var fourBeatPhase = beat * speed * Mathf.PI * 0.5f;
            var eightBeatPhase = fourBeatPhase * 0.5f;
            var twoBeatPhase = fourBeatPhase * 2f;
            var beatPhase = fourBeatPhase * 4f;
            var amount = _motionAmount.Value;
            return (LiveCameraMotionMode)_motionMode.Value switch
            {
                LiveCameraMotionMode.Drift => Drift(fourBeatPhase, amount),
                LiveCameraMotionMode.PushIn => ZoomPulse(fourBeatPhase, amount, false),
                LiveCameraMotionMode.PullOut => ZoomPulse(fourBeatPhase, amount, true),
                LiveCameraMotionMode.Orbit => Orbit(fourBeatPhase, amount),
                LiveCameraMotionMode.SlowDrift => Drift(eightBeatPhase, amount),
                LiveCameraMotionMode.FastDrift => Drift(twoBeatPhase, amount),
                LiveCameraMotionMode.SlowOrbit => Orbit(eightBeatPhase, amount),
                LiveCameraMotionMode.FastOrbit => Orbit(twoBeatPhase, amount),
                LiveCameraMotionMode.SlowPush => ZoomPulse(eightBeatPhase, amount, false),
                LiveCameraMotionMode.FastPush => ZoomPulse(twoBeatPhase, amount, false),
                LiveCameraMotionMode.SlowPull => ZoomPulse(eightBeatPhase, amount, true),
                LiveCameraMotionMode.FastPull => ZoomPulse(twoBeatPhase, amount, true),
                LiveCameraMotionMode.BeatPush => ZoomPulse(beatPhase, amount, false),
                LiveCameraMotionMode.BeatPull => ZoomPulse(beatPhase, amount, true),
                LiveCameraMotionMode.BeatOrbit => Orbit(beatPhase, amount),
                LiveCameraMotionMode.OffbeatPan => new Vector3(
                    Mathf.Sin(beatPhase + Mathf.PI) * amount * 0.65f,
                    Mathf.Sin(beatPhase + Mathf.PI * 0.5f) * amount * 0.2f,
                    amount * 0.05f),
                LiveCameraMotionMode.HalfTimeDrift => Drift(eightBeatPhase, amount),
                LiveCameraMotionMode.DoubleTimePan => new Vector3(
                    Mathf.Sin(twoBeatPhase) * amount * 0.75f,
                    Mathf.Sin(twoBeatPhase * 2f + 0.7f) * amount * 0.15f,
                    amount * 0.04f),
                LiveCameraMotionMode.BeatZoom => new Vector3(0f, 0f,
                    BeatPulse(beatPhase) * amount * 0.3f),
                LiveCameraMotionMode.BarSweep => new Vector3(
                    Mathf.Sin(fourBeatPhase) * amount * 0.8f,
                    Mathf.Sin(fourBeatPhase * 2f + 0.8f) * amount * 0.18f,
                    amount * 0.06f),
                LiveCameraMotionMode.BarOrbit => Orbit(fourBeatPhase, amount),
                LiveCameraMotionMode.LooseHandheld => Handheld(fourBeatPhase, amount),
                LiveCameraMotionMode.NervousHandheld => Handheld(twoBeatPhase, amount),
                LiveCameraMotionMode.PanSweep => new Vector3(
                    Mathf.Sin(fourBeatPhase) * amount * 0.85f, 0f, amount * 0.04f),
                LiveCameraMotionMode.FigureEight => new Vector3(
                    Mathf.Sin(fourBeatPhase) * amount * 0.65f,
                    Mathf.Sin(fourBeatPhase * 2f) * amount * 0.3f,
                    amount * 0.06f),
                LiveCameraMotionMode.DollyLeft => new Vector3(
                    (Mathf.Cos(fourBeatPhase) - 1f) * amount * 0.38f, 0f,
                    (0.5f - 0.5f * Mathf.Cos(fourBeatPhase)) * amount * 0.16f),
                LiveCameraMotionMode.DollyRight => new Vector3(
                    (1f - Mathf.Cos(fourBeatPhase)) * amount * 0.38f, 0f,
                    (0.5f - 0.5f * Mathf.Cos(fourBeatPhase)) * amount * 0.16f),
                LiveCameraMotionMode.CloseBreathe => new Vector3(
                    0f, Mathf.Sin(fourBeatPhase) * amount * 0.08f,
                    amount * (0.24f + 0.08f * Mathf.Sin(fourBeatPhase))),
                LiveCameraMotionMode.ArcLeft => Arc(fourBeatPhase, amount, -1f),
                LiveCameraMotionMode.ArcRight => Arc(fourBeatPhase, amount, 1f),
                LiveCameraMotionMode.FloatClose => new Vector3(
                    Mathf.Sin(fourBeatPhase) * amount * 0.18f,
                    Mathf.Cos(fourBeatPhase * 2f) * amount * 0.12f,
                    amount * (0.2f + 0.05f * Mathf.Sin(fourBeatPhase))),
                _ => Vector3.zero
            };
        }

        private float MotionRoll(float time)
        {
            if (!_motionEnabled.Value) return 0f;
            var beat = (float)(_bpmSource?.Beat ?? time * 2d);
            var speed = Mathf.Pow(4f, _motionSpeed.Value - 0.15f);
            var phase = beat * speed * Mathf.PI * 0.5f;
            var amount = _motionAmount.Value;
            return (LiveCameraMotionMode)_motionMode.Value switch
            {
                LiveCameraMotionMode.Orbit or LiveCameraMotionMode.BarOrbit => Mathf.Sin(phase) * amount * 2.5f,
                LiveCameraMotionMode.ArcLeft => -Mathf.Sin(phase) * amount * 3f,
                LiveCameraMotionMode.ArcRight => Mathf.Sin(phase) * amount * 3f,
                LiveCameraMotionMode.LooseHandheld => Mathf.Sin(phase * 3f + 0.4f) * amount * 1.5f,
                LiveCameraMotionMode.NervousHandheld => Mathf.Sin(phase * 8f + 0.4f) * amount * 3f,
                _ => 0f
            };
        }

        private static Vector3 Drift(float phase, float amount)
        {
            return new Vector3(
                Mathf.Sin(phase) * amount * 0.55f,
                Mathf.Sin(phase * 2f + 1.2f) * amount * 0.35f,
                amount * 0.06f);
        }

        private static Vector3 Orbit(float phase, float amount)
        {
            return new Vector3(
                Mathf.Sin(phase) * amount * 0.5f,
                Mathf.Cos(phase) * amount * 0.5f,
                amount * 0.08f);
        }

        private static Vector3 ZoomPulse(float phase, float amount, bool reverse)
        {
            var value = 0.5f - 0.5f * Mathf.Cos(phase);
            if (reverse) value = 1f - value;
            return new Vector3(0f, 0f, value * amount * 0.25f);
        }

        private static float BeatPulse(float phase)
        {
            return Mathf.Pow(Mathf.Max(0f, Mathf.Cos(phase)), 6f);
        }

        private static Vector3 Handheld(float phase, float amount)
        {
            return new Vector3(
                (Mathf.Sin(phase * 2f) + Mathf.Sin(phase * 5f) * 0.45f) * amount * 0.3f,
                (Mathf.Sin(phase * 3f + 1.1f) + Mathf.Sin(phase * 7f) * 0.35f) * amount * 0.22f,
                (0.5f + 0.5f * Mathf.Sin(phase * 4f)) * amount * 0.08f);
        }

        private static Vector3 Arc(float phase, float amount, float direction)
        {
            var side = (1f - Mathf.Cos(phase)) * amount * 0.32f * direction;
            return new Vector3(side, Mathf.Sin(phase) * amount * 0.22f,
                (0.5f - 0.5f * Mathf.Cos(phase)) * amount * 0.14f);
        }

        private void UpdateTransitionState()
        {
            if (_renderedMotionMode == _motionMode.Value
                && _renderedFraming == _framing.Value
                && _renderedLook == _look.Value) return;

            _renderedMotionMode = _motionMode.Value;
            _renderedFraming = _framing.Value;
            _renderedLook = _look.Value;
            _transitionStartedAt = Time.unscaledTime;
        }

        private void SmoothCameraTransform(Vector3 target, float targetRoll)
        {
            if (!_hasSmoothedTransform)
            {
                _smoothedTransform = target;
                _smoothedRoll = targetRoll;
                _hasSmoothedTransform = true;
                return;
            }

            var duration = TransitionDurationSeconds();
            var blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / Mathf.Max(0.02f, duration));
            _smoothedTransform = Vector3.Lerp(_smoothedTransform, target, blend);
            _smoothedRoll = Mathf.Lerp(_smoothedRoll, targetRoll, blend);
        }

        private float TransitionProgress()
        {
            return Mathf.SmoothStep(0f, 1f,
                (Time.unscaledTime - _transitionStartedAt) / TransitionDurationSeconds());
        }

        private float TransitionDurationSeconds()
        {
            var bpm = Mathf.Max(1f, (float)(_bpmSource?.Bpm ?? 120d));
            return 60f / bpm * TransitionBeats;
        }

        private void EnsureOutput()
        {
            var resolution = CameraProcessingResolution();
            var width = resolution.x;
            var height = resolution.y;
            if (_output != null && _output.width == width && _output.height == height) return;

            ReleaseOutput();
            _output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _output.Create();
            _outputIsClear = false;
        }

        private Vector2Int CameraProcessingResolution()
        {
            if (_cameraTexture == null || _cameraTexture.width <= 16 || _cameraTexture.height <= 16)
            {
                var fallback = _resolutionContainer?.Resolution ?? new Vector2Int(1920, 1080);
                var fallbackScale = Mathf.Min(1f, 1280f / fallback.x, 720f / fallback.y);
                return new Vector2Int(
                    Mathf.Max(1, Mathf.RoundToInt(fallback.x * fallbackScale)),
                    Mathf.Max(1, Mathf.RoundToInt(fallback.y * fallbackScale)));
            }

            var rotation = NormalizeRotation(_cameraTexture.videoRotationAngle);
            var sourceWidth = rotation is 90 or 270 ? _cameraTexture.height : _cameraTexture.width;
            var sourceHeight = rotation is 90 or 270 ? _cameraTexture.width : _cameraTexture.height;
            var scale = Mathf.Min(1f, 1280f / sourceWidth, 720f / sourceHeight);
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale)),
                Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale)));
        }

        private void ClearOutput()
        {
            if (_output == null || _outputIsClear) return;
            var previous = RenderTexture.active;
            RenderTexture.active = _output;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
            _outputIsClear = true;
        }

        private void ReleaseOutput()
        {
            if (_output == null) return;
            _output.Release();
            Destroy(_output);
            _output = null;
        }

        private static int NormalizeRotation(int angle)
        {
            angle = ((angle % 360) + 360) % 360;
            return Mathf.RoundToInt(angle / 90f) * 90 % 360;
        }

        private readonly struct FramingPreset
        {
            public readonly float Zoom;
            public readonly Vector2 Pan;
            public readonly float Roll;

            public FramingPreset(float zoom, Vector2 pan, float roll)
            {
                Zoom = zoom;
                Pan = pan;
                Roll = roll;
            }
        }

        private readonly struct LookPreset
        {
            public readonly int LayoutMode;
            public readonly float Contrast;
            public readonly float Saturation;
            public readonly float Exposure;
            public readonly float Vignette;
            public readonly float Softness;
            public readonly Vector3 ColorBalance;
            public readonly float Chromatic;

            public LookPreset(int layoutMode, float contrast, float saturation, float exposure,
                float vignette, float softness, Vector3 colorBalance, float chromatic = 0f)
            {
                LayoutMode = layoutMode;
                Contrast = contrast;
                Saturation = saturation;
                Exposure = exposure;
                Vignette = vignette;
                Softness = softness;
                ColorBalance = colorBalance;
                Chromatic = chromatic;
            }
        }
    }
}