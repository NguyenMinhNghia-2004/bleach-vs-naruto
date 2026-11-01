using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Integer stored XOR-obfuscated in memory (anti memory-editing).</summary>
    public class WrapInteger
    {
        private static readonly int[] _rndArr = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public WrapInteger(int v)
        {
            _offset = (int)Math.Floor(random() * _rndArr.Length);
            _w = v ^ _rndArr[_offset];
        }

        private int _w;
        private int _offset;

        public virtual void setValue(int v)
        {
            _offset = (int)Math.Floor(random() * _rndArr.Length);
            _w = v ^ _rndArr[_offset];
        }

        public virtual int getValue()
        {
            return _w ^ _rndArr[_offset];
        }

        public virtual string toString()
        {
            return getValue().ToString();
        }

        public override string ToString() => toString();
    }
}
