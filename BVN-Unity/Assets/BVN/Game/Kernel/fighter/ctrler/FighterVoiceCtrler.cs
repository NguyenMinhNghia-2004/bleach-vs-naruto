using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterVoiceCtrler
    {
        public FighterVoiceCtrler()
        {
            _soundTransform = new SoundTransform();
            _soundTransform.volume = GameData.I.config.soundVolume;
        }

        private Dictionary<int, List<object>> _voiceObj = new Dictionary<int, List<object>>();

        private SoundChannel _channel;
        private int _curLength;
        private SoundTransform _soundTransform;

        public virtual void destroy()
        {
            if (_voiceObj != null)
            {
                _voiceObj = null;
            }
            if (_channel != null)
            {
                _channel.stop();
                _channel = null;
            }
        }

        public virtual void setVoice(int id, List<object> sounds)
        {
            _voiceObj[id] = sounds;
        }

        public virtual void playVoice(int id, double rate = 1)
        {
            if (_channel != null && _channel.position < _curLength)
            {
                return;
            }
            if (random() > rate)
            {
                return;
            }

            List<object> sounds = null;
            if (_voiceObj != null) _voiceObj.TryGetValue(id, out sounds);
            if (sounds != null && sounds.Count > 0)
            {
                object snd = sounds.Count > 1 ? KyoRandom.getRandomInArray(sounds) : sounds[0];

                if (snd != null)
                {
                    // AS3: new snd()  (snd is a LinkedClass sound; accept a ready Sound too)
                    var sound = snd as Sound ?? New(snd) as Sound;
                    if (sound == null) return;
                    _curLength = (int)sound.length;
                    _channel = sound.play(0, 0, _soundTransform);
                }
            }
        }
    }
}
