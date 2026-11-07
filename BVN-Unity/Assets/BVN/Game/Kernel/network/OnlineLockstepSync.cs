using System;
using System.Collections.Generic;
using BVN.Script;

namespace BVN.Game
{
    /// <summary>
    /// Deterministic input lockstep for online 1v1.
    /// Every game frame N is simulated only when BOTH players' inputs for N are known. Local input sampled at frame N
    /// is scheduled for frame N + InputDelay and sent to the opponent, so both machines feed identical input to
    /// identical simulations: P1 = host input, P2 = guest input (local input goes through the same path).
    /// </summary>
    public class OnlineLockstepSync
    {
        private static OnlineLockstepSync _instance;
        public static OnlineLockstepSync Instance => _instance ??= new OnlineLockstepSync();

        /// <summary>Frames of input delay; hides network latency up to InputDelay / FPS seconds without stalling.</summary>
        public int InputDelay { get; set; } = 4;

        private readonly Dictionary<int, ushort> _remoteInputs = new Dictionary<int, ushort>();
        private readonly Dictionary<int, ushort> _localInputs = new Dictionary<int, ushort>();
        private GameSocketInput _localSampler;
        private GameSocketInput _p1Input;
        private GameSocketInput _p2Input;
        private int _nextLocalFrame;

        public bool IsRunning { get; private set; }
        /// <summary>Next frame to simulate.</summary>
        public int CurrentFrame { get; private set; }
        public int RemoteFrame { get; private set; }
        /// <summary>True when the last tick could not advance because the opponent's input had not arrived.</summary>
        public bool IsStalled { get; private set; }
        public int StalledTicks { get; private set; }

        public event Action<int> OnFrameAdvanced;

        public void Start(int initialFrame = 0)
        {
            _remoteInputs.Clear();
            _localInputs.Clear();
            CurrentFrame = initialFrame;
            RemoteFrame = initialFrame - 1;
            IsStalled = false;
            StalledTicks = 0;
            IsRunning = true;

            // identical random sequence on both machines from the first simulated frame
            BVNRandom.Seed(OnlineRoomSession.Instance.MatchRandomSeed);

            _localSampler = new GameSocketInput { enabled = true };
            _localSampler.setInputers(new List<object> { InputManager.I.key_p1, InputManager.I.joy_p1 });

            _p1Input = new GameSocketInput { enabled = true };
            _p2Input = new GameSocketInput { enabled = true };
            GameInterfaceManager.RemoteSocketInput = OnlineRoomSession.Instance.IsHost ? _p2Input : _p1Input;
            // bind both fighter slots to the lockstep inputs now (the input lists were cached at boot)
            GameInputer.setInput(GameInputType.P1, new List<IGameInput> { _p1Input });
            GameInputer.setInput(GameInputType.P2, new List<IGameInput> { _p2Input });

            // the first InputDelay frames have no input on either side
            for (int f = initialFrame; f < initialFrame + InputDelay; f++)
            {
                _localInputs[f] = 0;
                _remoteInputs[f] = 0;
            }
            _nextLocalFrame = initialFrame + InputDelay;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            IsStalled = false;
            _remoteInputs.Clear();
            _localInputs.Clear();
            GameInterfaceManager.RemoteSocketInput = null;
            GameInputer.initInput(); // restore keyboard / joystick inputs for local play
        }

        public void OnOpponentInputReceived(int frame, ushort inputBits)
        {
            _remoteInputs[frame] = inputBits;
            if (frame > RemoteFrame) RemoteFrame = frame;
        }

        public bool CanStepFrame(int frame) => !IsRunning || (_localInputs.ContainsKey(frame) && _remoteInputs.ContainsKey(frame));

        /// <summary>
        /// Called once per game tick before the simulation. Samples/sends local input and, when both inputs for
        /// CurrentFrame are available, applies them and returns true (simulate this tick). Returns false to stall.
        /// </summary>
        public bool TryStep()
        {
            if (!IsRunning) return true;

            // schedule this tick's local input (at most one new frame per tick so the delay stays constant)
            if (_nextLocalFrame <= CurrentFrame + InputDelay)
            {
                ushort bits = SampleLocalInput();
                _localInputs[_nextLocalFrame] = bits;
                OnlineRoomSession.Instance.SendFrameInput(_nextLocalFrame, bits);
                _nextLocalFrame++;
            }

            if (!CanStepFrame(CurrentFrame))
            {
                IsStalled = true;
                StalledTicks++;
                return false;
            }

            ushort local = _localInputs[CurrentFrame];
            ushort remote = _remoteInputs[CurrentFrame];
            bool host = OnlineRoomSession.Instance.IsHost;
            _p1Input.setSocketData(host ? local : remote);
            _p2Input.setSocketData(host ? remote : local);

            _localInputs.Remove(CurrentFrame - 60);
            _remoteInputs.Remove(CurrentFrame - 60);

            IsStalled = false;
            CurrentFrame++;
            GameInputer.stepFrame();
            OnFrameAdvanced?.Invoke(CurrentFrame);
            return true;
        }

        /// <summary>Keys held this tick (fresh each call).</summary>
        public ushort SampleLocalInput()
        {
            _localSampler.resetInput(); // clears then samples the current key state
            return (ushort)_localSampler.getSocketData();
        }
    }
}
