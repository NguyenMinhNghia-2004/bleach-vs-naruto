using System;

namespace BVN.Game
{
    public enum OnlinePacketType : byte
    {
        None = 0,
        Hello = 1,
        CreateRoom = 2,
        RoomCreated = 3,
        JoinRoom = 4,
        RoomJoined = 5,
        RoomError = 6,
        PlayerReady = 7,
        SelectSync = 8,
        MatchStart = 9,
        FrameInput = 10,
        LeaveRoom = 11,
        Ping = 12,
        Pong = 13
    }
}
