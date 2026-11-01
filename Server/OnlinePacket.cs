using System;
using System.IO;
using System.Text;

namespace BVN.Server
{
    public class OnlinePacket
    {
        public OnlinePacketType Type { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public int IntValue { get; set; }
        public int FrameIndex { get; set; }
        public ushort InputBits { get; set; }
        public string StringData { get; set; } = string.Empty;
        public byte[] RawPayload { get; set; }

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            writer.Write((byte)Type);
            writer.Write(RoomCode ?? string.Empty);
            writer.Write(IntValue);
            writer.Write(FrameIndex);
            writer.Write(InputBits);
            writer.Write(StringData ?? string.Empty);

            if (RawPayload != null && RawPayload.Length > 0)
            {
                writer.Write(RawPayload.Length);
                writer.Write(RawPayload);
            }
            else
            {
                writer.Write((int)0);
            }

            return ms.ToArray();
        }

        public static OnlinePacket Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms, Encoding.UTF8);

                var packet = new OnlinePacket
                {
                    Type = (OnlinePacketType)reader.ReadByte(),
                    RoomCode = reader.ReadString(),
                    IntValue = reader.ReadInt32(),
                    FrameIndex = reader.ReadInt32(),
                    InputBits = reader.ReadUInt16(),
                    StringData = reader.ReadString()
                };

                if (ms.Position < ms.Length)
                {
                    int payloadLen = reader.ReadInt32();
                    if (payloadLen > 0 && ms.Position + payloadLen <= ms.Length)
                    {
                        packet.RawPayload = reader.ReadBytes(payloadLen);
                    }
                }

                return packet;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
