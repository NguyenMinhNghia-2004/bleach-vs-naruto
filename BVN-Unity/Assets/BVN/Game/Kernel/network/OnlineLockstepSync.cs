using System;
using System.Collections.Generic;

namespace BVN.Game
{
    public class OnlineLockstepSync
    {
        private static OnlineLockstepSync _instance;
        public static OnlineLockstepSync Instance => _instance ??= new OnlineLockstepSync();

        private readonly Dictionary<int, ushort> _remoteInputs = new Dictionary<int, ushort>();
        private readonly Dictionary<int, ushort> _localInputs = new Dictionary<int, ushort>();
        private GameSocketInput _localSampler;

        public bool IsRunning { get; private set; }
        public int CurrentFrame { get; private set; }
        public int RemoteFrame { get; private set; }
        public int MaxBufferFrames { get; set; } = 3;

        public event Action<int> OnFrameAdvanced;

        public void Start(int initialFrame = 0)
        {
            _remoteInputs.Clear();
            _localInputs.Clear();
            CurrentFrame = initialFrame;
            RemoteFrame = initialFrame;
            IsRunning = true;

            if (GameInterfaceManager.RemoteSocketInput == null)
            {
                GameInterfaceManager.RemoteSocketInput = new GameSocketInput { enabled = true };
            }

            if (_localSampler == null)
            {
                _localSampler = new GameSocketInput { enabled = true };
                _localSampler.setInputers(new List<object> { InputManager.I.key_p1, InputManager.I.joy_p1 });
            }
        }

        public void Stop()
        {
            IsRunning = false;
            _remoteInputs.Clear();
            _localInputs.Clear();
            GameInterfaceManager.RemoteSocketInput = null;
        }

        public void OnOpponentInputReceived(int frame, ushort inputBits)
        {
            _remoteInputs[frame] = inputBits;
            if (frame > RemoteFrame)
            {
                RemoteFrame = frame;
            }
        }

        public bool CanStepFrame(int frame)
        {
            if (!IsRunning) return true;
            return _remoteInputs.ContainsKey(frame);
        }

        public ushort GetRemoteInput(int frame)
        {
            if (_remoteInputs.TryGetValue(frame, out ushort bits))
            {
                return bits;
            }
            return 0;
        }

        public ushort SampleLocalInput()
        {
            if (_localSampler == null)
            {
                _localSampler = new GameSocketInput { enabled = true };
                _localSampler.setInputers(new List<object> { InputManager.I.key_p1, InputManager.I.joy_p1 });
            }
            _localSampler.renderInput();
            _localSampler.freeRender();
            return (ushort)_localSampler.getSocketData();
        }

        public void ApplyRemoteInput(int frame)
        {
            if (GameInterfaceManager.RemoteSocketInput == null)
            {
                GameInterfaceManager.RemoteSocketInput = new GameSocketInput { enabled = true };
            }
            ushort bits = GetRemoteInput(frame);
            GameInterfaceManager.RemoteSocketInput.setSocketData(bits);
            GameInterfaceManager.RemoteSocketInput.freeRender();
        }

        public void AdvanceFrame(int frame, ushort localBits)
        {
            _localInputs[frame] = localBits;
            OnlineRoomSession.Instance.SendFrameInput(frame, localBits);

            ApplyRemoteInput(frame);

            if (_remoteInputs.ContainsKey(frame))
            {
                _remoteInputs.Remove(frame - 30);
                _localInputs.Remove(frame - 30);
            }

            CurrentFrame = frame + 1;
            OnFrameAdvanced?.Invoke(CurrentFrame);
        }
    }
}
