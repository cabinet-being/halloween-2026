using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Halloween2026
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField, Tooltip("Duration, clock hours and environmental curves for this night.")]
        private NightSettings _settings;

        [SerializeField, Tooltip("All secrets discoverable in this project. IDs must be unique and stable.")]
        private SecretDefinition[] _secrets = Array.Empty<SecretDefinition>();

        [SerializeField]
        private bool _startAutomatically = true;

        private readonly HashSet<string> _secretIds = new HashSet<string>(StringComparer.Ordinal);
        private PlayerProgress _progress;
        private ProgressStore _store;
        private bool _savingAllowed;
        private bool _dirty;

        public GameSession Session { get; private set; }
        public NightSettings Settings => _settings;
        public IReadOnlyList<SecretDefinition> Secrets => Array.AsReadOnly(_secrets);
        public int BestScore => _progress?.BestScore ?? 0;
        public int CompletedNights => _progress?.CompletedNights ?? 0;
        public string SaveError { get; private set; }

        public event Action ProgressChanged;

        private void Awake()
        {
            string warning;

            if (_settings == null || _settings.DurationSeconds <= 0f || float.IsNaN(_settings.DurationSeconds)
                || float.IsInfinity(_settings.DurationSeconds))
            {
                Debug.LogError("A valid Night Settings asset is required.", this);
                enabled = false;
                return;
            }

            foreach (var secret in _secrets)
            {
                if (secret != null && !string.IsNullOrWhiteSpace(secret.Id) && _secretIds.Add(secret.Id)) continue;

                Debug.LogError("Secret catalog contains a missing secret, empty ID or duplicate ID.", this);
                enabled = false;
                return;
            }

            _store = new ProgressStore(Path.Combine(Application.persistentDataPath, "progress.json"));
            try
            {
                _progress = _store.Load(out warning);
                _savingAllowed = true;
                if (warning != null) Debug.LogWarning(warning, this);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is NotSupportedException)
            {
                _progress = new PlayerProgress();
                SaveError = error.Message;
                Debug.LogError("Progress cannot be loaded; saving disabled to preserve existing files. " + SaveError, this);
            }

            Session = new GameSession(_settings.DurationSeconds);
            Session.Completed += CompleteNight;
        }

        private void Start()
        {
            if (_startAutomatically) Session?.Start();
        }

        private void Update()
        {
            Session?.Tick(Time.deltaTime);
        }

        public bool Award(ScoreSource source, int points) => Session != null && Session.Award(source, points);

        public bool IsDiscovered(string id) => _progress != null && _progress.DiscoveredSecrets.Contains(id);

        public bool Discover(string id)
        {
            if (Session == null || Session.State != SessionState.Running || !_secretIds.Contains(id)) return false;
            if (!_progress.Discover(id)) return false;

            _dirty = true;
            SaveProgress();
            ProgressChanged?.Invoke();

            return true;
        }

        public void SetPaused(bool paused)
        {
            Session?.SetPaused(paused);
        }

        public void RestartNight()
        {
            SaveProgress();
            if (_savingAllowed && _dirty) return; // Keep pending progress in memory if the write failed.

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void CompleteNight()
        {
            _progress.RecordNight(Session.Score);
            _dirty = true;
            SaveProgress();
            ProgressChanged?.Invoke();
        }

        public void SaveProgress()
        {
            if (!_savingAllowed || !_dirty) return;

            try
            {
                _store.Save(_progress);
                _dirty = false;
                SaveError = null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                SaveError = error.Message;
                Debug.LogError("Progress save failed; will retry at the next save point. " + SaveError, this);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnDestroy()
        {
            if (Session != null) Session.Completed -= CompleteNight;

            SaveProgress();
        }
    }
}
