using System;
using System.IO;
using System.Text;

namespace BVN.Game
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
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write((byte)Type);
                    bw.Write(RoomCode ?? string.Empty);
                    bw.Write(IntValue);
                    bw.Write(FrameIndex);
                    bw.Write(InputBits);
                    bw.Write(StringData ?? string.Empty);

                    if (RawPayload != null && RawPayload.Length > 0)
                    {
                        bw.Write(RawPayload.Length);
                        bw.Write(RawPayload);
                    }
                    else
                    {
                        bw.Write((int)0);
                    }
                }
                return ms.ToArray();
            }
        }

        public static OnlinePacket Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    using (var br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        var packet = new OnlinePacket();
                        packet.Type = (OnlinePacketType)br.ReadByte();
                        packet.RoomCode = br.ReadString();
                        packet.IntValue = br.ReadInt32();
                        packet.FrameIndex = br.ReadInt32();
                        packet.InputBits = br.ReadUInt16();
                        packet.StringData = br.ReadString();

                        if (ms.Position < ms.Length)
                        {
                            int payloadLen = br.ReadInt32();
                            if (payloadLen > 0 && ms.Position + payloadLen <= ms.Length)
                            {
                                packet.RawPayload = br.ReadBytes(payloadLen);
                            }
                        }

                        return packet;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
