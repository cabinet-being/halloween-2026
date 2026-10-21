using System;
using UnityEngine;

namespace Halloween2026
{
    [Serializable]
    public sealed class UserSettings
    {
        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.5f;
        public float Sensitivity = 1f;
        public float FieldOfView = 65f;
        public string BindingOverrides = "";

        public void Validate()
        {
            MasterVolume = Sanitize(MasterVolume, 0f, 1f, 0.8f);
            MusicVolume = Sanitize(MusicVolume, 0f, 1f, 0.5f);
            Sensitivity = Sanitize(Sensitivity, 0.1f, 4f, 1f);
            FieldOfView = Sanitize(FieldOfView, 45f, 100f, 65f);
            BindingOverrides = BindingOverrides ?? "";
        }

        private static float Sanitize(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;

            return Mathf.Clamp(value, min, max);
        }
    }
}
