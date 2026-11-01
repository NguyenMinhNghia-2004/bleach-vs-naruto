using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace BVN.Game
{
    public class TcpNetworkTransport : INetworkTransport
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private volatile bool _isRunning;
        private readonly object _lockObj = new object();
        private readonly Queue<byte[]> _inbox = new Queue<byte[]>();
        private readonly Queue<Action> _eventQueue = new Queue<Action>();

        public bool IsConnected => _client != null && _client.Connected;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnDataReceived;

        public void Connect(string host, int port)
        {
            Disconnect();

            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.ReceiveBufferSize = 65536;
                _client.SendBufferSize = 65536;

                _client.BeginConnect(host, port, ar =>
                {
                    try
                    {
                        _client.EndConnect(ar);
                        _stream = _client.GetStream();
                        _isRunning = true;

                        lock (_lockObj)
                        {
                            _eventQueue.Enqueue(() => OnConnected?.Invoke());
                        }

                        _receiveThread = new Thread(ReceiveLoop)
                        {
                            IsBackground = true
                        };
                        _receiveThread.Start();
                    }
                    catch (Exception)
                    {
                        lock (_lockObj)
                        {
                            _eventQueue.Enqueue(() => OnDisconnected?.Invoke());
                        }
                    }
                }, null);
            }
            catch (Exception)
            {
                lock (_lockObj)
                {
                    _eventQueue.Enqueue(() => OnDisconnected?.Invoke());
                }
            }
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_stream != null)
            {
                try { _stream.Close(); } catch (Exception) { }
                _stream = null;
            }

            if (_client != null)
            {
                try { _client.Close(); } catch (Exception) { }
                _client = null;
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                try { _receiveThread.Join(200); } catch (Exception) { }
                _receiveThread = null;
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected || _stream == null || data == null || data.Length == 0)
            {
                return;
            }

            try
            {
                byte[] lenPrefix = BitConverter.GetBytes(data.Length);
                lock (_stream)
                {
                    _stream.Write(lenPrefix, 0, 4);
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
            }
            catch (Exception)
            {
                Disconnect();
                lock (_lockObj)
                {
                    _eventQueue.Enqueue(() => OnDisconnected?.Invoke());
                }
            }
        }

        private void ReceiveLoop()
        {
            byte[] lenBuffer = new byte[4];

            while (_isRunning && _client != null && _client.Connected && _stream != null)
            {
                try
                {
                    if (!ReadExact(_stream, lenBuffer, 0, 4))
                    {
                        break;
                    }

                    int packetLen = BitConverter.ToInt32(lenBuffer, 0);
                    if (packetLen <= 0 || packetLen > 1048576)
                    {
                        break;
                    }

                    byte[] packetBuffer = new byte[packetLen];
                    if (!ReadExact(_stream, packetBuffer, 0, packetLen))
                    {
                        break;
                    }

                    lock (_lockObj)
                    {
                        _inbox.Enqueue(packetBuffer);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            Disconnect();

            lock (_lockObj)
            {
                _eventQueue.Enqueue(() => OnDisconnected?.Invoke());
            }
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read <= 0)
                {
                    return false;
                }
                totalRead += read;
            }
            return true;
        }

        public void Update()
        {
            Queue<Action> eventsToRun = null;
            Queue<byte[]> packetsToProcess = null;

            lock (_lockObj)
            {
                if (_eventQueue.Count > 0)
                {
                    eventsToRun = new Queue<Action>(_eventQueue);
                    _eventQueue.Clear();
                }

                if (_inbox.Count > 0)
                {
                    packetsToProcess = new Queue<byte[]>(_inbox);
                    _inbox.Clear();
                }
            }

            if (eventsToRun != null)
            {
                while (eventsToRun.Count > 0)
                {
                    eventsToRun.Dequeue()?.Invoke();
                }
            }

            if (packetsToProcess != null)
            {
                while (packetsToProcess.Count > 0)
                {
                    byte[] packet = packetsToProcess.Dequeue();
                    OnDataReceived?.Invoke(packet);
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
