using System;
using UnityEngine;

namespace SlotMachine.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SlotMachineConfig config;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        public event Action<AudioClip> ClipPlayed;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            ApplyAudioConfig();
        }

        public void ApplyAudioConfig()
        {
            if (config == null)
                return;

            if (audioSource == null)
                return;

            audioSource.loop = false;
        }

        public void PlayButtonClick()
        {
            PlayClip(config != null ? config.Audio.ButtonClick : null, 1f);
        }

        public void PlayLeverPull()
        {
            PlayClip(config != null ? config.Audio.LeverPull : null, 1f);
        }

        public void PlayReelSpin()
        {
            PlayClip(config != null ? config.Audio.ReelSpin : null, 0.8f);
        }

        public void PlayReelStop()
        {
            PlayClip(config != null ? config.Audio.ReelStop : null, 0.8f);
        }

        public void PlayWin()
        {
            PlayClip(config != null ? config.Audio.Win : null, 1f);
        }

        public void PlayJackpot()
        {
            PlayClip(config != null ? config.Audio.Jackpot : null, 1f);
        }

        public void PlayNoMoney()
        {
            PlayButtonClick();
        }

        public void PlayClip(AudioClip clip, float volumeScale = 1f)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip, volumeScale);
            ClipPlayed?.Invoke(clip);
        }
    }
}
