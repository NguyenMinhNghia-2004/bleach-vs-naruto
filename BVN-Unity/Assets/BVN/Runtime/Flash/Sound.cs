using System;
using System.Collections.Generic;
using BVN.Data;
using UnityEngine;

namespace BVN.Flash
{
    /// <summary>flash.media.SoundTransform</summary>
    public class SoundTransform
    {
        public double volume = 1, pan;
        public SoundTransform(double volume = 1, double pan = 0) { this.volume = volume; this.pan = pan; }
    }

    /// <summary>flash.media.Sound wrapping a Unity AudioClip.</summary>
    public class Sound : EventDispatcher
    {
        public AudioClip Clip;
        public Sound(AudioClip clip = null) { Clip = clip; }
        public double length => Clip != null ? Clip.length * 1000.0 : 0;

        public SoundChannel play(double startTime = 0, int loops = 0, SoundTransform transform = null)
        {
            if (Clip == null) return null;
            return SoundSystem.Play(Clip, startTime / 1000.0, loops, transform?.volume ?? 1, transform?.pan ?? 0);
        }

        public void close() { }
    }

    /// <summary>flash.media.SoundChannel</summary>
    public class SoundChannel : EventDispatcher
    {
        internal AudioSource Source;
        internal int LoopsLeft;
        SoundTransform _transform = new SoundTransform();

        public double position => Source != null && Source.clip != null ? Source.time * 1000.0 : 0;

        public SoundTransform soundTransform
        {
            get => _transform;
            set
            {
                _transform = value ?? new SoundTransform();
                if (Source != null)
                {
                    Source.volume = (float)(_transform.volume * SoundSystem.MasterVolume);
                    Source.panStereo = (float)_transform.pan;
                }
            }
        }

        public void stop()
        {
            if (Source != null)
            {
                Source.Stop();
                SoundSystem.Release(this);
            }
        }

        internal void Completed()
        {
            dispatchEvent(new Event(Event.SOUND_COMPLETE));
        }
    }

    /// <summary>flash.media.SoundMixer</summary>
    public static class SoundMixer
    {
        public static SoundTransform soundTransform = new SoundTransform();
        public static void stopAll() => SoundSystem.StopAll();
    }

    /// <summary>Pooled AudioSources playing Flash-style sounds.</summary>
    public static class SoundSystem
    {
        static GameObject _host;
        static readonly List<SoundChannel> Active = new List<SoundChannel>();
        static readonly Stack<AudioSource> Pool = new Stack<AudioSource>();
        public static double MasterVolume = 1;
        public static double EffectVolume = 1;

        static AudioSource Acquire()
        {
            if (_host == null)
            {
                _host = new GameObject("BVN_Sound");
                UnityEngine.Object.DontDestroyOnLoad(_host);
            }
            var s = Pool.Count > 0 ? Pool.Pop() : _host.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0;
            return s;
        }

        public static SoundChannel Play(AudioClip clip, double startSec, int loops, double volume, double pan)
        {
            if (clip == null) return null;
            if (Active.Count > 48)
            {
                // drop oldest one-shot to avoid unbounded overlap
                var old = Active[0];
                old.stop();
            }
            var src = Acquire();
            src.clip = clip;
            src.loop = loops >= int.MaxValue / 2;
            src.volume = (float)(volume * MasterVolume);
            src.panStereo = (float)pan;
            src.time = Mathf.Clamp((float)startSec, 0, Mathf.Max(0, clip.length - 0.01f));
            src.Play();
            var ch = new SoundChannel { Source = src, LoopsLeft = Math.Max(0, loops - 1) };
            ch.soundTransform.volume = volume;
            Active.Add(ch);
            return ch;
        }

        internal static void Release(SoundChannel ch)
        {
            if (ch.Source == null) return;
            Active.Remove(ch);
            ch.Source.clip = null;
            Pool.Push(ch.Source);
            ch.Source = null;
        }

        public static void StopAll()
        {
            foreach (var ch in Active.ToArray()) ch.stop();
        }

        /// <summary>Called every frame by FlashPlayer.</summary>
        public static void Update()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var ch = Active[i];
                if (ch.Source == null) { Active.RemoveAt(i); continue; }
                if (ch.Source.isPlaying) continue;
                if (ch.LoopsLeft > 0)
                {
                    ch.LoopsLeft--;
                    ch.Source.time = 0;
                    ch.Source.Play();
                    continue;
                }
                Release(ch);
                ch.Completed();
            }
        }

        /// <summary>Default handler for timeline frame sounds.</summary>
        public static void PlayTimelineSound(MovieClip mc, SoundEventDef s)
        {
            if (s.Sync == "stop") return;
            var clip = mc.Library?.GetSoundFile(s.File);
            double vol = EffectVolume;
            for (DisplayObject o = mc; o != null; o = o.parent)
                if (o is MovieClip m && m.soundTransform != null) vol *= m.soundTransform.volume;
            if (clip != null && vol > 0) Play(clip, 0, Math.Max(1, s.Loop), vol, 0);
        }
    }
}
