using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Ordered list with id lookup (ids are stringified like AS3 Object keys).</summary>
    public class ArrayMap
    {
        private Dictionary<string, object> _o = new Dictionary<string, object>();
        private List<object> _arr = new List<object>();

        static string k(object id) => id == null ? "null" : Str(id);

        public virtual int length => _arr.Count;

        public virtual void push(object id, object value)
        {
            _o[k(id)] = value;
            _arr.Add(value);
        }

        public virtual object getItemByIndex(int index)
        {
            return index >= 0 && index < _arr.Count ? _arr[index] : null;
        }

        public virtual object getItemById(object id)
        {
            return _o.TryGetValue(k(id), out var v) ? v : null;
        }

        public virtual void removeItemById(object id)
        {
            if (!_o.TryGetValue(k(id), out var v) || !Bool(v)) return;
            int index = _arr.IndexOf(v);
            if (index != -1) _arr.RemoveAt(index);
            _o.Remove(k(id));
        }
    }
}
