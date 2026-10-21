using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Halloween2026
{
    [DefaultExecutionOrder(-200)]
    public sealed class SettingsService : MonoBehaviour
    {
        public const string PreferenceKey = "Halloween2026.Settings.v1";

        [SerializeField, Tooltip("The authored Input Actions asset, shared with gameplay action references.")]
        private InputActionAsset _input;

        private UserSettings _values;

        public float MasterVolume => _values.MasterVolume;
        public float MusicVolume => _values.MusicVolume;
        public float Sensitivity => _values.Sensitivity;
        public float FieldOfView => _values.FieldOfView;
        public InputActionAsset Input => _input;
        public event Action Changed;

        private void Awake()
        {
            string json = PlayerPrefs.GetString(PreferenceKey, "");

            _values = new UserSettings();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _values = JsonUtility.FromJson<UserSettings>(json) ?? new UserSettings();
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("Invalid settings were replaced with defaults.", this);
                }
            }

            _values.Validate();
            _input.RemoveAllBindingOverrides();

            if (!string.IsNullOrEmpty(_values.BindingOverrides))
            {
                try
                {
                    _input.LoadBindingOverridesFromJson(_values.BindingOverrides);
                }
                catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
                {
                    _input.RemoveAllBindingOverrides();
                    _values.BindingOverrides = "";
                    Debug.LogWarning("Invalid control bindings were reset: " + error.Message, this);
                }
            }

            Apply();
        }

        public void SetMasterVolume(float value)
        {
            _values.MasterVolume = value;
            Commit();
        }

        public void SetMusicVolume(float value)
        {
            _values.MusicVolume = value;
            Commit();
        }

        public void SetSensitivity(float value)
        {
            _values.Sensitivity = value;
            Commit();
        }

        public void SetFieldOfView(float value)
        {
            _values.FieldOfView = value;
            Commit();
        }

        public void SaveBindings()
        {
            _values.BindingOverrides = _input.SaveBindingOverridesAsJson();
            Commit();
        }

        public void RestoreDefaults()
        {
            _values = new UserSettings();
            _input.RemoveAllBindingOverrides();
            Commit();
        }

        private void Commit()
        {
            _values.Validate();
            Apply();
            PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(_values));
            PlayerPrefs.Save();
        }

        private void Apply()
        {
            AudioListener.volume = _values.MasterVolume;
            Changed?.Invoke();
        }
    }
}
