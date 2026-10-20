using UnityEngine;
using UnityEngine.Rendering;

namespace Halloween2026
{
    public sealed class NightEnvironment : MonoBehaviour
    {
        [SerializeField, Tooltip("Session that drives normalized night progress.")]
        private GameBootstrapper _game;

        [SerializeField]
        private Light _moonLight;

        [SerializeField, Tooltip("Optional visible moon mesh; positioned relative to the viewing camera.")]
        private Transform _moonVisual;

        [SerializeField]
        private Camera _viewCamera;

        [SerializeField, Min(1f)]
        private float _moonDistance = 150f;

        private Material _originalSkybox;
        private Material _runtimeSkybox;
        private Color _originalAmbient;
        private Color _originalFog;
        private AmbientMode _originalAmbientMode;
        private bool _initialized;

        private void Start()
        {
            if (_game == null || _game.Session == null || _moonLight == null || _viewCamera == null)
            {
                Debug.LogError("Night Environment requires a running bootstrapper, moon light and camera.", this);
                enabled = false;
                return;
            }

            _originalSkybox = RenderSettings.skybox;
            _originalAmbient = RenderSettings.ambientLight;
            _originalAmbientMode = RenderSettings.ambientMode;
            _originalFog = RenderSettings.fogColor;

            if (_originalSkybox != null)
            {
                _runtimeSkybox = new Material(_originalSkybox);
                RenderSettings.skybox = _runtimeSkybox;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            _initialized = true;

            Apply();
        }

        private void LateUpdate()
        {
            if (_initialized) Apply();
        }

        private void Apply()
        {
            var settings = _game.Settings;
            float progress = _game.Session.Progress;
            Color sky = settings.SkyColor.Evaluate(progress);
            float elevation = Mathf.Lerp(settings.MoonElevation.x, settings.MoonElevation.y, progress);

            if (_runtimeSkybox != null)
            {
                if (_runtimeSkybox.HasProperty("_SkyTint")) _runtimeSkybox.SetColor("_SkyTint", sky);
                if (_runtimeSkybox.HasProperty("_Tint")) _runtimeSkybox.SetColor("_Tint", sky);
            }

            _viewCamera.backgroundColor = sky;
            RenderSettings.ambientLight = settings.AmbientColor.Evaluate(progress);
            RenderSettings.fogColor = sky;

            _moonLight.color = settings.MoonColor.Evaluate(progress);
            _moonLight.intensity = Mathf.Max(0f, settings.MoonIntensity.Evaluate(progress));
            _moonLight.transform.rotation = Quaternion.Euler(elevation, settings.MoonAzimuth, 0f);

            if (_moonVisual != null)
                _moonVisual.position = _viewCamera.transform.position - _moonLight.transform.forward * _moonDistance;
        }

        private void OnDestroy()
        {
            if (!_initialized) return;

            RenderSettings.skybox = _originalSkybox;
            RenderSettings.ambientLight = _originalAmbient;
            RenderSettings.ambientMode = _originalAmbientMode;
            RenderSettings.fogColor = _originalFog;

            if (_runtimeSkybox != null) Destroy(_runtimeSkybox);
        }
    }
}
