using System;
using System.Runtime.InteropServices;

namespace BVN.Game
{
    public class WebSocketNetworkTransport : INetworkTransport
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BVN_WS_Connect(string url);

        [DllImport("__Internal")]
        private static extern int BVN_WS_GetState();

        [DllImport("__Internal")]
        private static extern void BVN_WS_Send(byte[] data, int len);

        [DllImport("__Internal")]
        private static extern int BVN_WS_GetNextPacketSize();

        [DllImport("__Internal")]
        private static extern int BVN_WS_PopPacket(byte[] dest, int maxLen);

        [DllImport("__Internal")]
        private static extern void BVN_WS_Close();
#endif

        private bool _wasConnected;
        private bool _connecting;
        private byte[] _packetBuffer = new byte[65536];

        public bool IsConnected
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return BVN_WS_GetState() == 1;
#else
                return false;
#endif
            }
        }

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnDataReceived;

        public void Connect(string host, int port)
        {
            Disconnect();

            string url;
            if (host.StartsWith("ws://") || host.StartsWith("wss://"))
            {
                url = host;
            }
            else
            {
                // tunnels / https hosts use wss on 443; an explicit other port means a plain local relay (ws://host:7778)
                url = port > 0 && port != 443 ? $"ws://{host}:{port}" : $"wss://{host}";
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            _connecting = true;
            BVN_WS_Connect(url);
#endif
        }

        public void Disconnect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _connecting = false;
            BVN_WS_Close();
#endif
            if (_wasConnected)
            {
                _wasConnected = false;
                OnDisconnected?.Invoke();
            }
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0 || !IsConnected)
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            BVN_WS_Send(data, data.Length);
#endif
        }

        public void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int state = BVN_WS_GetState();
            if (state == 1 && !_wasConnected)
            {
                _wasConnected = true;
                OnConnected?.Invoke();
            }
            else if (state == 2 && (_wasConnected || _connecting))
            {
                // also reports a connection that never opened (dead tunnel / wrong address)
                _wasConnected = false;
                _connecting = false;
                OnDisconnected?.Invoke();
            }
            if (state == 1) _connecting = false;

            int nextSize;
            while ((nextSize = BVN_WS_GetNextPacketSize()) > 0)
            {
                if (nextSize > _packetBuffer.Length)
                {
                    _packetBuffer = new byte[nextSize];
                }

                int read = BVN_WS_PopPacket(_packetBuffer, nextSize);
                if (read > 0)
                {
                    byte[] packet = new byte[read];
                    Array.Copy(_packetBuffer, packet, read);
                    OnDataReceived?.Invoke(packet);
                }
            }
#endif
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
