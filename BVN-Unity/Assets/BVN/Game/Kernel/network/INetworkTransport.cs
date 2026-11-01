using System;

namespace BVN.Game
{
    public interface INetworkTransport : IDisposable
    {
        bool IsConnected { get; }
        void Connect(string host, int port);
        void Disconnect();
        void Send(byte[] data);
        void Update();
        event Action OnConnected;
        event Action OnDisconnected;
        event Action<byte[]> OnDataReceived;
    }
}
