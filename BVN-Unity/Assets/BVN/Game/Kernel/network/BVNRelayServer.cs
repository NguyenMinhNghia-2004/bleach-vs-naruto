using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BVN.Game
{
    public class BVNRelayServer : IDisposable
    {
        private class ConnectedClient
        {
            public TcpClient Tcp { get; set; }
            public NetworkStream Stream { get; set; }
            public string RoomCode { get; set; }
            public bool IsHost { get; set; }
        }

        private class Room
        {
            public string Code { get; set; }
            public ConnectedClient Host { get; set; }
            public ConnectedClient Guest { get; set; }
        }

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _isRunning;
        private readonly List<ConnectedClient> _clients = new List<ConnectedClient>();
        private readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>();
        private readonly object _lock = new object();
        private readonly Random _random = new Random();

        public int Port { get; private set; }
        public bool IsRunning => _isRunning;

        public void Start(int port = 7777)
        {
            Stop();

            Port = port;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start(100);
            _isRunning = true;

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true
            };
            _acceptThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;

            if (_listener != null)
            {
                try { _listener.Stop(); } catch (Exception) { }
                _listener = null;
            }

            lock (_lock)
            {
                foreach (var c in _clients)
                {
                    try { c.Stream?.Close(); } catch (Exception) { }
                    try { c.Tcp?.Close(); } catch (Exception) { }
                }
                _clients.Clear();
                _rooms.Clear();
            }

            if (_acceptThread != null && _acceptThread.IsAlive)
            {
                try { _acceptThread.Join(200); } catch (Exception) { }
                _acceptThread = null;
            }
        }

        private void AcceptLoop()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var tcp = _listener.AcceptTcpClient();
                    tcp.NoDelay = true;
                    var client = new ConnectedClient
                    {
                        Tcp = tcp,
                        Stream = tcp.GetStream()
                    };

                    lock (_lock)
                    {
                        _clients.Add(client);
                    }

                    var clientThread = new Thread(() => ClientLoop(client))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private void ClientLoop(ConnectedClient client)
        {
            byte[] lenBuffer = new byte[4];

            while (_isRunning && client.Tcp != null && client.Tcp.Connected)
            {
                try
                {
                    if (!ReadExact(client.Stream, lenBuffer, 0, 4)) break;
                    int len = BitConverter.ToInt32(lenBuffer, 0);
                    if (len <= 0 || len > 1048576) break;

                    byte[] data = new byte[len];
                    if (!ReadExact(client.Stream, data, 0, len)) break;

                    ProcessPacket(client, data);
                }
                catch (Exception)
                {
                    break;
                }
            }

            DisconnectClient(client);
        }

        private void ProcessPacket(ConnectedClient client, byte[] data)
        {
            var packet = OnlinePacket.Deserialize(data);
            if (packet == null) return;

            switch (packet.Type)
            {
                case OnlinePacketType.CreateRoom:
                    HandleCreateRoom(client);
                    break;

                case OnlinePacketType.JoinRoom:
                    HandleJoinRoom(client, packet.RoomCode);
                    break;

                case OnlinePacketType.LeaveRoom:
                    DisconnectClientFromRoom(client);
                    break;

                default:
                    RelayToPeer(client, data);
                    break;
            }
        }

        private void HandleCreateRoom(ConnectedClient client)
        {
            lock (_lock)
            {
                DisconnectClientFromRoom(client);

                string code;
                do
                {
                    code = GenerateRoomCode();
                } while (_rooms.ContainsKey(code));

                var room = new Room
                {
                    Code = code,
                    Host = client
                };

                client.RoomCode = code;
                client.IsHost = true;
                _rooms[code] = room;

                var reply = new OnlinePacket
                {
                    Type = OnlinePacketType.RoomCreated,
                    RoomCode = code
                };
                Send(client, reply.Serialize());
            }
        }

        private void HandleJoinRoom(ConnectedClient client, string code)
        {
            lock (_lock)
            {
                DisconnectClientFromRoom(client);

                if (string.IsNullOrEmpty(code) || !_rooms.TryGetValue(code, out var room))
                {
                    var error = new OnlinePacket
                    {
                        Type = OnlinePacketType.RoomError,
                        StringData = "Không tìm thấy phòng"
                    };
                    Send(client, error.Serialize());
                    return;
                }

                if (room.Guest != null)
                {
                    var fullError = new OnlinePacket
                    {
                        Type = OnlinePacketType.RoomError,
                        StringData = "Phòng đã đầy"
                    };
                    Send(client, fullError.Serialize());
                    return;
                }

                room.Guest = client;
                client.RoomCode = code;
                client.IsHost = false;

                var guestReply = new OnlinePacket
                {
                    Type = OnlinePacketType.RoomJoined,
                    RoomCode = code
                };
                Send(client, guestReply.Serialize());

                var hostNotify = new OnlinePacket
                {
                    Type = OnlinePacketType.JoinRoom,
                    RoomCode = code
                };
                Send(room.Host, hostNotify.Serialize());
            }
        }

        private void RelayToPeer(ConnectedClient sender, byte[] data)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(sender.RoomCode) || !_rooms.TryGetValue(sender.RoomCode, out var room))
                {
                    return;
                }

                var target = sender.IsHost ? room.Guest : room.Host;
                if (target != null)
                {
                    Send(target, data);
                }
            }
        }

        private void DisconnectClientFromRoom(ConnectedClient client)
        {
            if (string.IsNullOrEmpty(client.RoomCode)) return;

            if (_rooms.TryGetValue(client.RoomCode, out var room))
            {
                var notify = new OnlinePacket
                {
                    Type = OnlinePacketType.LeaveRoom,
                    RoomCode = client.RoomCode
                };

                if (client.IsHost)
                {
                    if (room.Guest != null)
                    {
                        Send(room.Guest, notify.Serialize());
                        room.Guest.RoomCode = null;
                    }
                    _rooms.Remove(client.RoomCode);
                }
                else
                {
                    room.Guest = null;
                    if (room.Host != null)
                    {
                        Send(room.Host, notify.Serialize());
                    }
                }
            }

            client.RoomCode = null;
        }

        private void DisconnectClient(ConnectedClient client)
        {
            lock (_lock)
            {
                DisconnectClientFromRoom(client);
                _clients.Remove(client);

                try { client.Stream?.Close(); } catch (Exception) { }
                try { client.Tcp?.Close(); } catch (Exception) { }
            }
        }

        private void Send(ConnectedClient client, byte[] data)
        {
            if (client == null || client.Stream == null || !client.Tcp.Connected) return;

            try
            {
                byte[] len = BitConverter.GetBytes(data.Length);
                lock (client.Stream)
                {
                    client.Stream.Write(len, 0, 4);
                    client.Stream.Write(data, 0, data.Length);
                    client.Stream.Flush();
                }
            }
            catch (Exception)
            {
            }
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sb = new StringBuilder(6);
            for (int i = 0; i < 6; i++)
            {
                sb.Append(chars[_random.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read <= 0) return false;
                total += read;
            }
            return true;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
