using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetBtnEvent : Event
    {
        public new const string SELECT = "SELECT";
        public const string OPTION_CHANGE = "OPTION_CHANGE";
        public const string APPLY_SET = "APPLY_SET";
        public const string CANCEL_SET = "CANCEL_SET";

        public SetBtnEvent(string type, bool bubbles = false, bool cancelable = false) : base(type, bubbles, cancelable)
        {
            _cancelable = cancelable;
        }

        // BVN.Flash.Event does not expose `cancelable`
        private bool _cancelable;
        public virtual bool cancelable => _cancelable;

        public string selectedLabel;
        public string optionKey;
        public object optionValue;

        public virtual SetBtnEvent newEvent()
        {
            SetBtnEvent @event = new SetBtnEvent(type, bubbles, cancelable);
            @event.selectedLabel = selectedLabel;
            @event.optionKey = optionKey;
            @event.optionValue = optionValue;
            return @event;
        }
    }
}
