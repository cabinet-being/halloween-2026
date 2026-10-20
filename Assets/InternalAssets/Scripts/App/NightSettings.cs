using UnityEngine;

namespace Halloween2026
{
    [CreateAssetMenu(menuName = "Halloween/Night Settings")]
    public sealed class NightSettings : ScriptableObject
    {
        [SerializeField, Min(1f)]
        private float _durationSeconds = 480f;

        [SerializeField, Range(0f, 24f)]
        private float _startHour = 20f;

        [SerializeField, Range(0f, 24f)]
        private float _endHour = 6f;

        [SerializeField]
        private Gradient _skyColor = CreateSkyGradient();

        [SerializeField]
        private Gradient _ambientColor = CreateSkyGradient();

        [SerializeField]
        private Gradient _moonColor = CreateMoonGradient();

        [SerializeField]
        private AnimationCurve _moonIntensity = new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.5f, 0.65f), new Keyframe(1f, 0f));

        [SerializeField]
        private Vector2 _moonElevation = new Vector2(0f, 180f);

        [SerializeField]
        private float _moonAzimuth = -30f;

        public float DurationSeconds => _durationSeconds;
        public Gradient SkyColor => _skyColor;
        public Gradient AmbientColor => _ambientColor;
        public Gradient MoonColor => _moonColor;
        public AnimationCurve MoonIntensity => _moonIntensity;
        public Vector2 MoonElevation => _moonElevation;
        public float MoonAzimuth => _moonAzimuth;

        public float HourAt(float progress)
        {
            float hours = Mathf.Repeat(_endHour - _startHour, 24f);

            if (Mathf.Approximately(hours, 0f)) hours = 24f;

            return Mathf.Repeat(_startHour + hours * Mathf.Clamp01(progress), 24f);
        }

        private static Gradient CreateSkyGradient()
        {
            var gradient = new Gradient();

            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(0.18f, 0.10f, 0.28f), 0f),
                new GradientColorKey(new Color(0.015f, 0.025f, 0.08f), 0.35f),
                new GradientColorKey(new Color(0.025f, 0.04f, 0.12f), 0.8f),
                new GradientColorKey(new Color(0.65f, 0.36f, 0.32f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }

        private static Gradient CreateMoonGradient()
        {
            var gradient = new Gradient();

            gradient.SetKeys(new[] { new GradientColorKey(new Color(0.65f, 0.75f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.65f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }
    }
}
