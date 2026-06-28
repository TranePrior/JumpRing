using UnityEngine;

namespace JumpRing.Game.Core
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusicPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioClip musicClip;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.5f;

        // Mirror of AudioSettingsService's music key. AudioSettingsService applies the
        // authoritative mute later (after async storage load); reading the local mirror here
        // avoids an audible blip on boot for players who disabled music.
        private const string MusicSettingKey = "Settings_Music";

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = musicClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.mute = PlayerPrefs.GetInt(MusicSettingKey, 1) == 0;
            audioSource.Play();
        }
    }
}
