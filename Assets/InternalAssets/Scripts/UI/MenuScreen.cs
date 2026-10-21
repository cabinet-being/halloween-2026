using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Halloween2026
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuScreen : MonoBehaviour
    {
        [SerializeField]
        private SettingsService _settings;

        [SerializeField]
        private VisualTreeAsset _settingsLayout;

        [SerializeField]
        private InputActionReference _pauseAction;

        private VisualElement _root;
        private SettingsView _settingsView;

        public SettingsView Settings => _settingsView;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settingsView = new SettingsView(_settings, _root, _settingsLayout, CloseSettings);
            _root.Q<Button>("play-button").clicked += SceneFlow.StartNight;
            _root.Q<Button>("settings-button").clicked += OpenSettings;
            _root.Q<Button>("quit-button").clicked += SceneFlow.Quit;
            _root.Q<Button>("play-button").Focus();

            _pauseAction.action.Enable();
            _pauseAction.action.performed += OnBack;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenSettings()
        {
            _root.Q("menu-screen").AddToClassList("hidden");
            _settingsView.Show();
        }

        private void CloseSettings()
        {
            _root.Q("menu-screen").RemoveFromClassList("hidden");
            _root.Q<Button>("settings-button").Focus();
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            if (!_settingsView.IsOpen || _settingsView.IsRebinding) return;
            if (Time.frameCount <= _settingsView.IgnoreBackUntilFrame) return;

            _settingsView.Back();
        }

        private void OnDestroy()
        {
            _settingsView?.Dispose();
            _pauseAction.action.performed -= OnBack;
            _pauseAction.action.Disable();
        }
    }
}
