using System;
using System.Collections.Concurrent;
using System.Threading;
using RtMidi;
using UnityEngine;

namespace qoooo.Midi.Transport
{
    /// <summary>RtMidiへのnative送信を専用threadへ隔離する。Unity APIはUpdateだけで使用する。</summary>
    public sealed class RtMidiOutput : MonoBehaviour, IMidiOutput
    {
        [SerializeField] private string _productName = "APC mini mk2";
        // APC mini mk2: channel 6 (0x96) means solid LED at 100% brightness.
        [SerializeField] [Range(0, 15)] private int _channel = 6;
        [SerializeField] private bool _logConnection = true;
        private readonly ConcurrentQueue<string> _diagnostics = new();
        private readonly ConcurrentQueue<byte[]> _queue = new();
        private readonly AutoResetEvent _signal = new(false);
        private volatile bool _connected;
        private int _connectionGeneration;
        private int _reconnectRequested;
        private int _queuedCount;
        private volatile bool _running;
        private Thread _thread;

        private void Update()
        {
            while (_diagnostics.TryDequeue(out var message)) Debug.Log(message, this);
            if (isActiveAndEnabled && !_running && (_thread == null || !_thread.IsAlive)) StartWorker();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled) RequestReconnect();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused && isActiveAndEnabled) RequestReconnect();
        }

        private void OnEnable()
        {
            StartWorker();
        }

        private void OnDisable()
        {
            StopWorker(true);
        }

        private void OnDestroy()
        {
            StopWorker(true);
        }

        public bool IsConnected => _connected;
        public int ConnectionGeneration => Volatile.Read(ref _connectionGeneration);

        public void RequestReconnect()
        {
            if (!_running) return;
            Interlocked.Exchange(ref _reconnectRequested, 1);
            _connected = false;
            _signal.Set();
        }

        public void SendNoteOn(int note, byte velocity)
        {
            Enqueue((byte)(0x90 | _channel), (byte)note, velocity);
        }

        public void SendNoteOff(int note)
        {
            Enqueue((byte)(0x80 | _channel), (byte)note, 0);
        }

        public void SendLedOff(int note)
        {
            // Same 0x96 behaviour channel with velocity 0: explicit LED reset.
            Enqueue((byte)(0x90 | _channel), (byte)note, 0);
        }

        public void SendSingleLed(int note, bool on)
        {
            // Track / Scene Launch / Shift are single-colour LEDs on MIDI channel 0.
            Enqueue(0x90, (byte)note, on ? (byte)1 : (byte)0);
        }

        public void FlushAndStop()
        {
            StopWorker(true);
        }

        public void StartWorker()
        {
            if (_running || (_thread != null && _thread.IsAlive)) return;
            _running = true;
            _thread = new Thread(Worker) { IsBackground = true, Name = "qoooo RtMidi Output" };
            _thread.Start();
        }

        public void StopWorker(bool drain)
        {
            if (!_running) return;
            _running = false;
            if (!drain)
                while (_queue.TryDequeue(out _))
                    Interlocked.Decrement(ref _queuedCount);
            _signal.Set();
            if (_thread != null && !_thread.Join(100))
                _diagnostics.Enqueue("[MIDI] Output worker did not stop within 100ms; it will finish in background.");
            if (_thread == null || !_thread.IsAlive) _thread = null;
            _connected = false;
        }

        private void Enqueue(byte status, byte data1, byte data2)
        {
            if (!_running) return;
            if (Interlocked.Increment(ref _queuedCount) > 512)
            {
                Interlocked.Decrement(ref _queuedCount);
                _diagnostics.Enqueue("[MIDI] Output queue full; LED message dropped.");
                return;
            }

            _queue.Enqueue(new[] { status, data1, data2 });
            _signal.Set();
        }

        private void Worker()
        {
            MidiOut output = null;
            var nextConnect = DateTime.UtcNow;
            var nextPortCheck = DateTime.UtcNow;
            var connectedPort = -1;
            var connectedName = string.Empty;
            var waitingLogged = false;
            try
            {
                while (_running)
                {
                    if (Interlocked.Exchange(ref _reconnectRequested, 0) != 0)
                    {
                        _connected = false;
                        output?.Dispose(); output = null;
                        connectedPort = -1;
                        ClearPending();
                        nextConnect = DateTime.UtcNow;
                    }
                    if (output != null && DateTime.UtcNow >= nextPortCheck)
                    {
                        nextPortCheck = DateTime.UtcNow.AddSeconds(1);
                        try
                        {
                            if (connectedPort >= output.PortCount || output.GetPortName(connectedPort) != connectedName)
                            {
                                _diagnostics.Enqueue("[MIDI] Output port disappeared; reconnecting.");
                                _connected = false;
                                output.Dispose(); output = null;
                                ClearPending();
                                nextConnect = DateTime.UtcNow;
                            }
                        }
                        catch (Exception exception)
                        {
                            _diagnostics.Enqueue($"[MIDI] Output port check failed: {exception.Message}");
                            _connected = false;
                            output?.Dispose(); output = null;
                            ClearPending();
                            nextConnect = DateTime.UtcNow;
                        }
                    }
                    if (output == null && DateTime.UtcNow >= nextConnect)
                    {
                        try
                        {
                            output = MidiOut.Create();
                            var port = FindPort(output);
                            if (port >= 0)
                            {
                                output.OpenPort(port, "qoooo APC mini mk2");
                                connectedPort = port;
                                connectedName = output.GetPortName(port);
                                ClearPending();
                                Interlocked.Increment(ref _connectionGeneration);
                                _connected = true;
                                waitingLogged = false;
                                nextPortCheck = DateTime.UtcNow.AddSeconds(1);
                                if (_logConnection)
                                    _diagnostics.Enqueue($"[MIDI] Output connected: '{connectedName}'.");
                            }
                            else
                            {
                                if (_logConnection && !waitingLogged)
                                    _diagnostics.Enqueue(
                                        $"[MIDI] Waiting for output '{_productName}'. Available ports: {DescribePorts(output)}");
                                waitingLogged = true;
                                output.Dispose();
                                output = null;
                                _connected = false;
                            }
                        }
                        catch (Exception exception)
                        {
                            output?.Dispose();
                            output = null;
                            _connected = false;
                            ClearPending();
                            _diagnostics.Enqueue($"[MIDI] Output connect failed: {exception.Message}");
                        }

                        nextConnect = DateTime.UtcNow.AddMilliseconds(500);
                    }

                    if (output != null && _queue.TryDequeue(out var message))
                    {
                        Interlocked.Decrement(ref _queuedCount);
                        try
                        {
                            if (output.SendMessage(message) != 0)
                                throw new InvalidOperationException(output.Error);
                        }
                        catch (Exception exception)
                        {
                            _diagnostics.Enqueue($"[MIDI] Output send failed: {exception.Message}");
                            output.Dispose();
                            output = null;
                            _connected = false;
                            ClearPending();
                        }

                        continue;
                    }

                    _signal.WaitOne(50);
                }

                while (_queue.TryDequeue(out var message))
                {
                    Interlocked.Decrement(ref _queuedCount);
                    if (output != null && output.SendMessage(message) != 0)
                        throw new InvalidOperationException(output.Error);
                }
            }
            catch (Exception exception)
            {
                _diagnostics.Enqueue($"[MIDI] Output worker stopped: {exception.Message}");
            }
            finally
            {
                output?.Dispose();
                _connected = false;
            }
        }

        private void ClearPending()
        {
            while (_queue.TryDequeue(out _)) Interlocked.Decrement(ref _queuedCount);
        }

        private int FindPort(MidiOut output)
        {
            for (var index = 0; index < output.PortCount; index++)
            {
                var name = output.GetPortName(index);
                if (string.IsNullOrWhiteSpace(_productName) ||
                    name.IndexOf(_productName, StringComparison.OrdinalIgnoreCase) >= 0) return index;
            }

            return -1;
        }

        private static string DescribePorts(MidiOut output)
        {
            if (output.PortCount == 0) return "none";
            var names = new string[output.PortCount];
            for (var i = 0; i < names.Length; i++) names[i] = output.GetPortName(i);
            return string.Join(", ", names);
        }
    }
}
