using System.Collections.Generic;
using System.Linq;
using qoooo.Parameters;
using qoooo.Timing;
using qoooo.View;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace qoooo.Midi
{
    /// <summary>Trip26 の MIDI composition root。MIDI未接続でもParameterの初期化は必ず行う。</summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class MidiSystemController : MonoBehaviour, IUiTarget
    {
        private IBpmSource _bpmSource;
        [SerializeField] private MidiParameterRegistry _parameterRegistry;
        [SerializeField] private MidiParameterRuntime _parameterRuntime;
        [SerializeField] private MinisMidiInput _midiInput;
        [SerializeField] private ApcMiniMk2ParameterController _apcController;
        [SerializeField] private MidiProfileStore _profileStore;
        [SerializeField] private RtMidiOutput _midiOutput;
        [SerializeField] private ApcMiniMk2LedRenderer _ledRenderer;
        private MidiBindingUi _bindingUi;
        private bool _initialized;
        private bool _outputShutdown;
        private IEnumerable<IMidiParameterContainer> _providers = Enumerable.Empty<IMidiParameterContainer>();

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            _outputShutdown = false;
#if UNITY_EDITOR
            EditorApplication.focusChanged += OnEditorFocusChanged;
#endif
            Initialize();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.focusChanged -= OnEditorFocusChanged;
#endif
            ShutdownOutput();
        }

        private void OnDestroy()
        {
            ShutdownOutput();
        }

        public Element CreateElement()
        {
            return UI.Column(
                UI.Label("APC mini mk2"),
                UI.Label(() => _midiInput != null && _midiInput.IsConnected ? "Input: connected" : "Input: waiting"),
                UI.Label(() =>
                    _midiOutput != null && _midiOutput.IsConnected ? "Output: connected" : "Output: waiting"),
                UI.Button("Reconnect MIDI", () =>
                {
                    _midiInput?.RequestReconnect();
                    _midiOutput?.RequestReconnect();
                }),
                UI.Label(() => $"Parameters: {_parameterRegistry?.Parameters.Count ?? 0}"),
                UI.Label(() => $"Page: {_apcController?.CurrentPage ?? 0}"),
                UI.Button("Rebuild Mapping", () => _apcController?.Rebuild()),
                UI.Label(() => _profileStore != null && _profileStore.IsDirty ? "Profile: unsaved" : "Profile: saved"),
                UI.Button("Revert MIDI", () => _profileStore?.Revert()),
                UI.Button("Reset Mapping", () => _profileStore?.ResetMapping()),
                UI.Button("Reset Values", () => _profileStore?.ResetValues())
                , UI.Fold("MIDI Mapping", _bindingUi?.CreateElement() ?? UI.Label("MIDI is not initialized.")).Open()
            );
        }

        [Inject]
        public void Construct(IBpmSource bpmSource, IEnumerable<IMidiParameterContainer> providers)
        {
            _bpmSource = bpmSource;
            _providers = providers ?? Enumerable.Empty<IMidiParameterContainer>();
            // Domain reload直後はOnEnableがInjectより先に走る場合がある。
            // 空Providerで初期化済みなら、注入完了時に一度だけ正しい集合で作り直す。
            if (_initialized && _parameterRegistry != null && _parameterRegistry.Parameters.Count == 0 &&
                _providers.Any())
            {
                _initialized = false;
                Initialize();
            }
        }
#if UNITY_EDITOR
        private void OnEditorFocusChanged(bool focused)
        {
            if (!focused || !Application.isPlaying || !isActiveAndEnabled) return;
            _midiInput?.RequestReconnect();
            _midiOutput?.RequestReconnect();
        }
#endif
        private void Initialize()
        {
            if (_initialized) return;
            if (_bpmSource == null || _parameterRegistry == null || _parameterRuntime == null || _apcController == null ||
                _midiInput == null || _profileStore == null || _midiOutput == null || _ledRenderer == null)
            {
                Debug.LogError(
                    "[MIDI] MidiSystemController is missing a required component reference; MIDI is disabled.", this);
                return;
            }

            _parameterRegistry.Initialize(_providers);
            _parameterRuntime.Configure(_parameterRegistry, _bpmSource);
            _apcController.Configure(_midiInput, _parameterRegistry, _bpmSource);
            _apcController.Rebuild();
            _profileStore.Configure(_apcController, _parameterRegistry);
            _profileStore.Initialize();
            _bindingUi = new MidiBindingUi(_apcController, _parameterRegistry);
            _initialized = true;
            Debug.Log($"[MIDI] System initialized with {_parameterRegistry.Parameters.Count} typed parameters.", this);
        }

        private void ShutdownOutput()
        {
            if (_outputShutdown) return;
            _outputShutdown = true;
            // APC protocol: 0x96 (channel 6) + velocity 0 clears every controlled LED.
            _ledRenderer?.Clear();
            _midiOutput?.FlushAndStop();
        }
    }
}
