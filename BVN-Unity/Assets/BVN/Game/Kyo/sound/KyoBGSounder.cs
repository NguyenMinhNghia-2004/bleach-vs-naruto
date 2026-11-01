using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Background music player (single looping channel with pause/resume).</summary>
    public class KyoBGSounder
    {
        private static KyoBGSounder _i;

        public static KyoBGSounder I
        {
            get
            {
                _i ??= new KyoBGSounder();
                return _i;
            }
        }

        /// <summary>Sound, url String, or Class.</summary>
        public object sound;
        public bool playing;

        private Sound _snd;
        private SoundChannel _channel;
        private SoundTransform _soundTransform = new SoundTransform();
        private int _channelPausePosition;

        public virtual double volume
        {
            get => _soundTransform.volume;
            set
            {
                _soundTransform.volume = value;
                if (_channel != null) _channel.soundTransform = _soundTransform;
            }
        }

        public virtual void play(object snd = null, bool isLoop = true)
        {
            trace("bgm play");
            if (_snd != null) return;
            if (snd == null) snd = sound;
            if (snd != null)
            {
                sound = snd;
            }
            else
            {
                trace("没有可播放的音乐");
                return;
            }

            if (sound is string url) _snd = AssetBridge.LoadSound(url);
            if (sound is Type || sound is LinkedClass) _snd = New(sound) as Sound;
            if (sound is Sound s) _snd = s;

            playSound(0, isLoop);
            playing = true;
        }

        public virtual void stop()
        {
            trace("bgm stop");
            if (_channel != null)
            {
                _channel.removeEventListener(Event.SOUND_COMPLETE, playCompleteHandler);
                _channel.stop();
                _channel = null;
            }
            if (_snd != null)
            {
                try
                {
                    _snd.close();
                }
                catch (Exception e)
                {
                    trace("KyoBGSounder", e);
                }
                _snd = null;
            }
            playing = false;
        }

        public virtual void pause()
        {
            trace("bgm pause");
            if (_channel != null)
            {
                _channelPausePosition = (int)_channel.position;
                _channel.removeEventListener(Event.SOUND_COMPLETE, playCompleteHandler);
                _channel.stop();
            }
        }

        public virtual void resume()
        {
            trace("bgm resume");
            if (_channel != null) playSound(_channelPausePosition);
        }

        public virtual void toggle()
        {
            if (playing) stop();
            else play();
        }

        private void playSound(int position = 0, bool isLoop = true)
        {
            if (_snd == null) return;
            _channel = _snd.play(position, 1, _soundTransform);
            // without a sound device the channel is null
            if (_channel == null) return;
            _channel.removeEventListener(Event.SOUND_COMPLETE, playCompleteHandler);
            if (isLoop) _channel.addEventListener(Event.SOUND_COMPLETE, playCompleteHandler);
        }

        private void playCompleteHandler(Event e)
        {
            if (_channel != null)
            {
                _channel.removeEventListener(Event.SOUND_COMPLETE, playCompleteHandler);
                _channel = null;
            }
            playSound(0);
        }
    }
}
