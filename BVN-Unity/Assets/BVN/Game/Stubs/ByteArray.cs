using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub for flash.utils.ByteArray (binary file IO is out of scope, see PORTING.md §6).</summary>
    public class ByteArray
    {
        public List<byte> Data = new List<byte>();
        public uint position;
        public uint length => (uint)Data.Count;
        public uint bytesAvailable => position < length ? length - position : 0;
        public string endian = "bigEndian";
        public virtual void clear() { Data.Clear(); position = 0; }
        public virtual void compress(string algorithm = null) { }
        public virtual void uncompress(string algorithm = null) { }
        public virtual void writeUTFBytes(string s) { Data.AddRange(System.Text.Encoding.UTF8.GetBytes(s ?? "")); position = length; }
        public virtual string readUTFBytes(uint len)
        {
            int start = (int)Math.Min(position, length);
            int n = (int)Math.Min(len, length - (uint)start);
            position = (uint)(start + n);
            return System.Text.Encoding.UTF8.GetString(Data.GetRange(start, n).ToArray());
        }
        public virtual void writeObject(object o) { writeUTFBytes(JSONStringify(o)); }
        public virtual object readObject() { position = 0; return JSONParse(readUTFBytes(length)); }
        public override string ToString() => System.Text.Encoding.UTF8.GetString(Data.ToArray());
    }
}
