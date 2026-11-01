using System;
using System.Collections.Generic;
using System.IO;

namespace BVN.Game
{
    public class OnlineRoomSession
    {
        private static OnlineRoomSession _instance;
        public static OnlineRoomSession Instance => _instance ??= new OnlineRoomSession();

        private readonly Queue<byte[]> _pendingSendQueue = new Queue<byte[]>();

        public INetworkTransport Transport { get; private set; }
        public string ServerHost { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 7777;

        public string CurrentRoomCode { get; private set; } = string.Empty;
        public bool IsHost { get; private set; }
        public bool IsInRoom { get; private set; }
        public bool IsOpponentInRoom { get; private set; }
        public bool IsLocalReady { get; private set; }
        public bool IsOpponentReady { get; private set; }

        public string LocalFighterId { get; set; } = "naruto";
        public string LocalAssisterId { get; set; } = "sakura";
        public string RemoteFighterId { get; set; } = "ichigo";
        public string RemoteAssisterId { get; set; } = "rukia";
        public string SelectedMapId { get; set; } = "woods";
        public int MatchRandomSeed { get; set; } = 12345;

        public event Action<string> OnRoomCreated;
        public event Action<string> OnRoomJoined;
        public event Action<string> OnRoomError;
        public event Action OnOpponentJoined;
        public event Action OnOpponentLeft;
        public event Action<bool> OnOpponentReadyChanged;
        public event Action OnMatchStartTriggered;

        public OnlineRoomSession()
        {
            LoadConfig();
#if UNITY_WEBGL && !UNITY_EDITOR
            ServerHost = "lenders-penguin-hardly-instruments.trycloudflare.com";
            ServerPort = 443;
            Transport = new WebSocketNetworkTransport();
#else
            Transport = new TcpNetworkTransport();
#endif
            Transport.OnConnected += HandleConnected;
            Transport.OnDisconnected += HandleDisconnected;
            Transport.OnDataReceived += HandleDataReceived;
        }

        public void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.txt");
                if (File.Exists(configPath))
                {
                    string text = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var parts = text.Split(':');
                        if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                        {
                            ServerHost = parts[0].Trim();
                        }
                        if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int port))
                        {
                            ServerPort = port;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void SetTransport(INetworkTransport customTransport)
        {
            if (Transport != null)
            {
                Transport.OnConnected -= HandleConnected;
                Transport.OnDisconnected -= HandleDisconnected;
                Transport.OnDataReceived -= HandleDataReceived;
                Transport.Dispose();
            }

            Transport = customTransport;
            if (Transport != null)
            {
                Transport.OnConnected += HandleConnected;
                Transport.OnDisconnected += HandleDisconnected;
                Transport.OnDataReceived += HandleDataReceived;
            }
        }

        public void ConnectToServer()
        {
            if (!Transport.IsConnected)
            {
                Transport.Connect(ServerHost, ServerPort);
            }
        }

        public void SendPacket(OnlinePacket packet)
        {
            if (packet == null) return;
            byte[] bytes = packet.Serialize();
            if (Transport != null && Transport.IsConnected)
            {
                Transport.Send(bytes);
            }
            else
            {
                _pendingSendQueue.Enqueue(bytes);
            }
        }

        public void CreateRoom()
        {
            IsHost = true;
            ConnectToServer();
            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.CreateRoom
            });
        }

        public void JoinRoom(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                OnRoomError?.Invoke("Mã phòng không hợp lệ");
                return;
            }

            IsHost = false;
            ConnectToServer();
            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.JoinRoom,
                RoomCode = code.Trim().ToUpperInvariant()
            });
        }

        public void SetReady(bool ready)
        {
            IsLocalReady = ready;
            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.PlayerReady,
                RoomCode = CurrentRoomCode,
                IntValue = ready ? 1 : 0
            });
        }

        public void SendSelectSync(string fighterId, string assistId)
        {
            LocalFighterId = fighterId;
            LocalAssisterId = assistId;

            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.SelectSync,
                RoomCode = CurrentRoomCode,
                StringData = $"{fighterId}|{assistId}"
            });
        }

        public void StartMatch(string mapId, int seed)
        {
            if (!IsHost) return;

            SelectedMapId = mapId;
            MatchRandomSeed = seed;

            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.MatchStart,
                RoomCode = CurrentRoomCode,
                StringData = mapId,
                IntValue = seed
            });
            OnMatchStartTriggered?.Invoke();
        }

        public void SendFrameInput(int frameIndex, ushort inputBits)
        {
            SendPacket(new OnlinePacket
            {
                Type = OnlinePacketType.FrameInput,
                RoomCode = CurrentRoomCode,
                FrameIndex = frameIndex,
                InputBits = inputBits
            });
        }

        public void LeaveRoom()
        {
            if (IsInRoom)
            {
                SendPacket(new OnlinePacket
                {
                    Type = OnlinePacketType.LeaveRoom,
                    RoomCode = CurrentRoomCode
                });
            }

            _pendingSendQueue.Clear();
            ResetState();
        }

        public void Update()
        {
            Transport?.Update();
        }

        private void HandleConnected()
        {
            var hello = new OnlinePacket { Type = OnlinePacketType.Hello };
            Transport.Send(hello.Serialize());

            while (_pendingSendQueue.Count > 0)
            {
                var queued = _pendingSendQueue.Dequeue();
                Transport.Send(queued);
            }
        }

        private void HandleDisconnected()
        {
            _pendingSendQueue.Clear();
            bool wasInRoom = IsInRoom;
            ResetState();
            if (!wasInRoom)
            {
                OnRoomError?.Invoke("Không thể kết nối đến máy chủ Relay");
            }
        }

        private void HandleDataReceived(byte[] data)
        {
            var packet = OnlinePacket.Deserialize(data);
            if (packet == null) return;

            switch (packet.Type)
            {
                case OnlinePacketType.RoomCreated:
                    CurrentRoomCode = packet.RoomCode;
                    IsInRoom = true;
                    IsOpponentInRoom = false;
                    OnRoomCreated?.Invoke(CurrentRoomCode);
                    break;

                case OnlinePacketType.RoomJoined:
                    CurrentRoomCode = packet.RoomCode;
                    IsInRoom = true;
                    IsOpponentInRoom = true;
                    OnRoomJoined?.Invoke(CurrentRoomCode);
                    break;

                case OnlinePacketType.RoomError:
                    OnRoomError?.Invoke(packet.StringData);
                    break;

                case OnlinePacketType.JoinRoom:
                    IsOpponentInRoom = true;
                    OnOpponentJoined?.Invoke();
                    break;

                case OnlinePacketType.LeaveRoom:
                    IsOpponentInRoom = false;
                    IsOpponentReady = false;
                    OnOpponentLeft?.Invoke();
                    break;

                case OnlinePacketType.PlayerReady:
                    IsOpponentReady = packet.IntValue == 1;
                    OnOpponentReadyChanged?.Invoke(IsOpponentReady);
                    break;

                case OnlinePacketType.SelectSync:
                    var parts = (packet.StringData ?? string.Empty).Split('|');
                    if (parts.Length >= 2)
                    {
                        RemoteFighterId = parts[0];
                        RemoteAssisterId = parts[1];
                    }
                    break;

                case OnlinePacketType.MatchStart:
                    SelectedMapId = packet.StringData;
                    MatchRandomSeed = packet.IntValue;
                    OnMatchStartTriggered?.Invoke();
                    break;

                case OnlinePacketType.FrameInput:
                    OnlineLockstepSync.Instance.OnOpponentInputReceived(packet.FrameIndex, packet.InputBits);
                    break;
            }
        }

        private void ResetState()
        {
            CurrentRoomCode = string.Empty;
            IsInRoom = false;
            IsOpponentInRoom = false;
            IsLocalReady = false;
            IsOpponentReady = false;
        }
    }
}
