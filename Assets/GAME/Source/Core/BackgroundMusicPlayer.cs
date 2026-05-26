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

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = musicClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
