using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BVN.Server
{
    public interface IRelayClient
    {
        string RemoteEndPoint { get; }
        string RoomCode { get; set; }
        bool IsHost { get; set; }
        bool IsConnected { get; }
        void Send(byte[] data);
        void Disconnect();
    }

    public class TcpRelayClient : IRelayClient
    {
        private readonly TcpClient _tcp;
        private readonly NetworkStream _stream;
        private readonly Action<IRelayClient, byte[]> _onMessage;
        private readonly Action<IRelayClient> _onDisconnect;
        private volatile bool _isRunning;

        public string RemoteEndPoint { get; }
        public string RoomCode { get; set; }
        public bool IsHost { get; set; }
        public bool IsConnected => _isRunning && _tcp != null && _tcp.Connected;

        public TcpRelayClient(TcpClient tcp, Action<IRelayClient, byte[]> onMessage, Action<IRelayClient> onDisconnect)
        {
            _tcp = tcp;
            _tcp.NoDelay = true;
            _stream = tcp.GetStream();
            RemoteEndPoint = tcp.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            _onMessage = onMessage;
            _onDisconnect = onDisconnect;
            _isRunning = true;
        }

        public void Start()
        {
            var thread = new Thread(ReadLoop)
            {
                IsBackground = true
            };
            thread.Start();
        }

        public void Send(byte[] data)
        {
            if (!_isRunning || _stream == null || !_tcp.Connected || data == null || data.Length == 0)
            {
                return;
            }

            try
            {
                byte[] len = BitConverter.GetBytes(data.Length);
                lock (_stream)
                {
                    _stream.Write(len, 0, 4);
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            try { _stream?.Close(); } catch (Exception) { }
            try { _tcp?.Close(); } catch (Exception) { }

            _onDisconnect?.Invoke(this);
        }

        private void ReadLoop()
        {
            byte[] lenBuffer = new byte[4];

            while (_isRunning && _tcp.Connected)
            {
                try
                {
                    if (!ReadExact(_stream, lenBuffer, 0, 4))
                    {
                        break;
                    }

                    int len = BitConverter.ToInt32(lenBuffer, 0);
                    if (len <= 0 || len > 1048576)
                    {
                        break;
                    }

                    byte[] buffer = new byte[len];
                    if (!ReadExact(_stream, buffer, 0, len))
                    {
                        break;
                    }

                    _onMessage?.Invoke(this, buffer);
                }
                catch (Exception)
                {
                    break;
                }
            }

            Disconnect();
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read <= 0)
                {
                    return false;
                }
                total += read;
            }
            return true;
        }
    }

    public class WebSocketRelayClient : IRelayClient
    {
        private readonly WebSocket _ws;
        private readonly Action<IRelayClient, byte[]> _onMessage;
        private readonly Action<IRelayClient> _onDisconnect;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private volatile bool _isRunning;

        public string RemoteEndPoint { get; }
        public string RoomCode { get; set; }
        public bool IsHost { get; set; }
        public bool IsConnected => _isRunning && _ws != null && _ws.State == WebSocketState.Open;

        public WebSocketRelayClient(WebSocket ws, string remoteEndPoint, Action<IRelayClient, byte[]> onMessage, Action<IRelayClient> onDisconnect)
        {
            _ws = ws;
            RemoteEndPoint = remoteEndPoint;
            _onMessage = onMessage;
            _onDisconnect = onDisconnect;
            _isRunning = true;
        }

        public void Start()
        {
            _ = ReadLoopAsync();
        }

        public async void Send(byte[] data)
        {
            if (!_isRunning || _ws.State != WebSocketState.Open || data == null || data.Length == 0)
            {
                return;
            }

            try
            {
                await _sendLock.WaitAsync();
                try
                {
                    await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            try
            {
                if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
                {
                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(200);
                }
            }
            catch (Exception) { }

            try { _ws.Dispose(); } catch (Exception) { }

            _onDisconnect?.Invoke(this);
        }

        private async Task ReadLoopAsync()
        {
            var buffer = new byte[65536];
            using var ms = new MemoryStream();

            while (_isRunning && _ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        byte[] fullMessage = ms.ToArray();
                        ms.SetLength(0);
                        _onMessage?.Invoke(this, fullMessage);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            Disconnect();
        }
    }

    public class BVNRelayServer : IDisposable
    {
        private class Room
        {
            public string Code { get; set; }
            public IRelayClient Host { get; set; }
            public IRelayClient Guest { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private TcpListener _tcpListener;
        private Thread _tcpAcceptThread;
        private HttpListener _httpListener;
        private Thread _wsAcceptThread;
        private volatile bool _isRunning;
        private readonly List<IRelayClient> _clients = new List<IRelayClient>();
        private readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>();
        private readonly object _lock = new object();
        private readonly Random _random = new Random();

        public int Port { get; private set; }
        public int WsPort { get; private set; }
        public bool IsRunning => _isRunning;

        public void Start(int port = 7777)
        {
            Stop();

            Port = port;
            WsPort = port + 1;
            _isRunning = true;

            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start(100);

            _tcpAcceptThread = new Thread(TcpAcceptLoop)
            {
                IsBackground = true
            };
            _tcpAcceptThread.Start();

            Console.WriteLine($"[BVN Server] TCP dang lang nghe tai cong {Port}");

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://*:{WsPort}/");
                _httpListener.Start();
            }
            catch (Exception)
            {
                try
                {
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add($"http://localhost:{WsPort}/");
                    _httpListener.Prefixes.Add($"http://127.0.0.1:{WsPort}/");
                    _httpListener.Start();
                }
                catch (Exception)
                {
                    _httpListener = null;
                }
            }

            if (_httpListener != null && _httpListener.IsListening)
            {
                _wsAcceptThread = new Thread(WsAcceptLoop)
                {
                    IsBackground = true
                };
                _wsAcceptThread.Start();
                Console.WriteLine($"[BVN Server] WebSocket dang lang nghe tai cong {WsPort}");
            }
        }

        public void Stop()
        {
            _isRunning = false;

            if (_tcpListener != null)
            {
                try { _tcpListener.Stop(); } catch (Exception) { }
                _tcpListener = null;
            }

            if (_httpListener != null)
            {
                try { _httpListener.Stop(); } catch (Exception) { }
                try { _httpListener.Close(); } catch (Exception) { }
                _httpListener = null;
            }

            lock (_lock)
            {
                foreach (var c in _clients.ToArray())
                {
                    c.Disconnect();
                }
                _clients.Clear();
                _rooms.Clear();
            }

            if (_tcpAcceptThread != null && _tcpAcceptThread.IsAlive)
            {
                try { _tcpAcceptThread.Join(200); } catch (Exception) { }
                _tcpAcceptThread = null;
            }

            if (_wsAcceptThread != null && _wsAcceptThread.IsAlive)
            {
                try { _wsAcceptThread.Join(200); } catch (Exception) { }
                _wsAcceptThread = null;
            }
        }

        private void TcpAcceptLoop()
        {
            while (_isRunning && _tcpListener != null)
            {
                try
                {
                    var tcp = _tcpListener.AcceptTcpClient();
                    var client = new TcpRelayClient(tcp, ProcessPacket, DisconnectClient);

                    lock (_lock)
                    {
                        _clients.Add(client);
                    }

                    Console.WriteLine($"[BVN Server] Client TCP ket noi: {client.RemoteEndPoint} (Tong CCU: {_clients.Count})");
                    client.Start();
                }
                catch (Exception)
                {
                    if (!_isRunning) break;
                }
            }
        }

        private async void WsAcceptLoop()
        {
            while (_isRunning && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        var client = new WebSocketRelayClient(
                            wsContext.WebSocket,
                            context.Request.RemoteEndPoint?.ToString() ?? "Unknown",
                            ProcessPacket,
                            DisconnectClient
                        );

                        lock (_lock)
                        {
                            _clients.Add(client);
                        }

                        Console.WriteLine($"[BVN Server] Client WebSocket ket noi: {client.RemoteEndPoint} (Tong CCU: {_clients.Count})");
                        client.Start();
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                        byte[] msg = Encoding.UTF8.GetBytes("Bleach vs Naruto Relay Server is Online!");
                        context.Response.OutputStream.Write(msg, 0, msg.Length);
                        context.Response.Close();
                    }
                }
                catch (Exception)
                {
                    if (!_isRunning) break;
                }
            }
        }

        private void ProcessPacket(IRelayClient client, byte[] data)
        {
            var packet = OnlinePacket.Deserialize(data);
            if (packet == null) return;

            switch (packet.Type)
            {
                case OnlinePacketType.Hello:
                    break;

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

        private void HandleCreateRoom(IRelayClient client)
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
                    Host = client,
                    CreatedAt = DateTime.UtcNow
                };

                client.RoomCode = code;
                client.IsHost = true;
                _rooms[code] = room;

                var reply = new OnlinePacket
                {
                    Type = OnlinePacketType.RoomCreated,
                    RoomCode = code
                };
                client.Send(reply.Serialize());

                Console.WriteLine($"[BVN Server] Phong [{code}] duoc tao boi Host {client.RemoteEndPoint} (Tong phong: {_rooms.Count})");
            }
        }

        private void HandleJoinRoom(IRelayClient client, string code)
        {
            lock (_lock)
            {
                DisconnectClientFromRoom(client);

                if (string.IsNullOrEmpty(code) || !_rooms.TryGetValue(code, out var room))
                {
                    var error = new OnlinePacket
                    {
                        Type = OnlinePacketType.RoomError,
                        StringData = "Khong tim thay phong"
                    };
                    client.Send(error.Serialize());
                    return;
                }

                if (room.Guest != null)
                {
                    var fullError = new OnlinePacket
                    {
                        Type = OnlinePacketType.RoomError,
                        StringData = "Phong da day"
                    };
                    client.Send(fullError.Serialize());
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
                client.Send(guestReply.Serialize());

                var hostNotify = new OnlinePacket
                {
                    Type = OnlinePacketType.JoinRoom,
                    RoomCode = code
                };
                room.Host.Send(hostNotify.Serialize());

                Console.WriteLine($"[BVN Server] Khach {client.RemoteEndPoint} da vao phong [{code}]. Tran dau san sang!");
            }
        }

        private void RelayToPeer(IRelayClient sender, byte[] data)
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
                    target.Send(data);
                }
            }
        }

        private void DisconnectClientFromRoom(IRelayClient client)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(client.RoomCode)) return;

                if (_rooms.TryGetValue(client.RoomCode, out var room))
                {
                    if (room.Host == client)
                    {
                        Console.WriteLine($"[BVN Server] Host roi phong [{client.RoomCode}]. Phong da bi dong.");
                        if (room.Guest != null)
                        {
                            var leaveNotice = new OnlinePacket
                            {
                                Type = OnlinePacketType.LeaveRoom,
                                RoomCode = client.RoomCode
                            };
                            room.Guest.Send(leaveNotice.Serialize());
                            room.Guest.RoomCode = null;
                        }
                        _rooms.Remove(client.RoomCode);
                    }
                    else if (room.Guest == client)
                    {
                        Console.WriteLine($"[BVN Server] Khach roi phong [{client.RoomCode}].");
                        room.Guest = null;
                        if (room.Host != null)
                        {
                            var leaveNotice = new OnlinePacket
                            {
                                Type = OnlinePacketType.LeaveRoom,
                                RoomCode = client.RoomCode
                            };
                            room.Host.Send(leaveNotice.Serialize());
                        }
                    }
                }

                client.RoomCode = null;
            }
        }

        private void DisconnectClient(IRelayClient client)
        {
            lock (_lock)
            {
                DisconnectClientFromRoom(client);
                _clients.Remove(client);

                Console.WriteLine($"[BVN Server] Client ngat ket noi: {client.RemoteEndPoint} (Tong CCU: {_clients.Count})");
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

        public void Dispose()
        {
            Stop();
        }
    }
}
