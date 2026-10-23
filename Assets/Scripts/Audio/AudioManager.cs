using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Plays generated SFX (pooled sources, pitch variation, rate limiting) and per-zone music.</summary>
    public class AudioManager : MonoBehaviour
    {
        GameManager gm;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<int, AudioClip> music = new Dictionary<int, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource[] pool;
        AudioSource musicSource;
        int next;
        int currentMusicZone = -1;

        public void Init(GameManager g)
        {
            gm = g;
            if (FindAnyListener() == null) gameObject.AddComponent<AudioListener>();
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.35f;
        }

        static AudioListener FindAnyListener()
        {
            var cam = Camera.main;
            return cam != null ? cam.GetComponent<AudioListener>() : null;
        }

        AudioClip Clip(Sfx s)
        {
            if (!clips.TryGetValue(s, out var c) || c == null)
            {
                c = SoundFactory.Create(s);
                clips[s] = c;
            }
            return c;
        }

        public void Play(Sfx s, float volume = 0.7f, float pitchJitter = 0.06f)
        {
            if (gm == null || gm.Save == null || !gm.Save.sfxOn || pool == null) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(s, out float last) && now - last < 0.035f) return;
            lastPlayed[s] = now;
            var src = pool[next];
            next = (next + 1) % pool.Length;
            src.clip = Clip(s);
            src.volume = volume;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.Play();
        }

        public void PlayZoneMusic(ZoneDefinition z)
        {
            if (musicSource == null) return;
            if (currentMusicZone == z.index && musicSource.isPlaying) return;
            currentMusicZone = z.index;
            if (!music.TryGetValue(z.index, out var clip) || clip == null)
            {
                clip = SoundFactory.Music(z);
                music[z.index] = clip;
            }
            musicSource.clip = clip;
            ApplyMusicSetting();
        }

        public void ApplyMusicSetting()
        {
            if (musicSource == null || musicSource.clip == null) return;
            if (gm.Save.musicOn) { if (!musicSource.isPlaying) musicSource.Play(); }
            else musicSource.Stop();
        }
    }
}
