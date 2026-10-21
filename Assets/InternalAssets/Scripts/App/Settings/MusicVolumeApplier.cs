using UnityEngine;

namespace Halloween2026
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicVolumeApplier : MonoBehaviour
    {
        [SerializeField]
        private SettingsService _settings;

        [SerializeField, Range(0f, 1f)]
        private float _baseVolume = 0.3f;

        private AudioSource _source;

        private void Start()
        {
            _source = GetComponent<AudioSource>();
            _source.ignoreListenerPause = true;
            _settings.Changed += Apply;

            Apply();
        }

        private void Apply()
        {
            _source.volume = _baseVolume * _settings.MusicVolume;
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= Apply;
        }
    }
}
