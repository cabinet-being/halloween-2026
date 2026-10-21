using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Halloween2026
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameplayScreen : MonoBehaviour
    {
        [SerializeField]
        private GameBootstrapper _game;

        [SerializeField]
        private SettingsService _settings;

        [SerializeField]
        private GhostController _ghost;

        [SerializeField]
        private VisualTreeAsset _settingsLayout;

        [SerializeField]
        private InputActionReference _pauseAction;

        private VisualElement _root;
        private SettingsView _settingsView;
        private float _noticeUntil;

        public SettingsView Settings => _settingsView;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settingsView = new SettingsView(_settings, _root, _settingsLayout, ShowPause);
            _root.Q<Button>("resume-button").clicked += Resume;
            _root.Q<Button>("settings-button").clicked += OpenSettings;
            _root.Q<Button>("menu-button").clicked += ExitToMenu;
            _root.Q<Button>("results-menu-button").clicked += ExitToMenu;
            _root.Q<Button>("retry-button").clicked += Restart;

            _pauseAction.action.Enable();
            _pauseAction.action.performed += OnPause;
            _game.Session.Completed += ShowResults;
            _ghost.ActionRequested += ShowAction;
            _settings.Changed += RefreshControls;

            SetWorldPaused(false);
            RefreshControls();
        }

        private void Update()
        {
            int seconds = Mathf.CeilToInt(_game.Session.Remaining);

            _root.Q<Label>("score-label").text = _game.Session.Score.ToString("N0");
            _root.Q<Label>("time-label").text = $"{seconds / 60:00}:{seconds % 60:00}";

            if (Time.unscaledTime > _noticeUntil) _root.Q<Label>("action-notice").text = "";
        }

        public void Pause()
        {
            if (_game.Session.State != SessionState.Running) return;

            _game.SetPaused(true);
            SetWorldPaused(true);
            ShowPause();
        }

        public void Resume()
        {
            if (_game.Session.State != SessionState.Paused) return;

            _settingsView.Hide();
            _root.Q("pause-screen").AddToClassList("hidden");
            _root.Q("hud").RemoveFromClassList("hidden");
            _game.SetPaused(false);
            SetWorldPaused(false);
        }

        public void OpenSettings()
        {
            _root.Q("pause-screen").AddToClassList("hidden");
            _settingsView.Show();
        }

        private void ShowPause()
        {
            _root.Q("hud").AddToClassList("hidden");
            _root.Q("pause-screen").RemoveFromClassList("hidden");
            _root.Q<Button>("resume-button").Focus();
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            if (_settingsView.IsRebinding || Time.frameCount <= _settingsView.IgnoreBackUntilFrame) return;

            if (_settingsView.IsOpen) _settingsView.Back();
            else if (_game.Session.State == SessionState.Running) Pause();
            else if (_game.Session.State == SessionState.Paused) Resume();
        }

        private void SetWorldPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        private void ShowResults()
        {
            SetWorldPaused(true);
            _root.Q("hud").AddToClassList("hidden");
            _root.Q("results-screen").RemoveFromClassList("hidden");
            _root.Q<Label>("result-score").text = _game.Session.Score.ToString("N0");
            _root.Q<Label>("best-score").text = "Best night: " + _game.BestScore.ToString("N0");
            _root.Q<Button>("retry-button").Focus();
        }

        private void ShowAction(string action)
        {
            _root.Q<Label>("action-notice").text = action;
            _noticeUntil = Time.unscaledTime + 1.2f;
        }

        private void RefreshControls()
        {
            InputAction move = _settings.Input.FindAction("Player/Move", true);
            string forward = move.bindings[ControlBindings.Index(move, ControlBindings.Slots[0])].ToDisplayString();
            string backward = move.bindings[ControlBindings.Index(move, ControlBindings.Slots[1])].ToDisplayString();
            string left = move.bindings[ControlBindings.Index(move, ControlBindings.Slots[2])].ToDisplayString();
            string right = move.bindings[ControlBindings.Index(move, ControlBindings.Slots[3])].ToDisplayString();

            _root.Q<Label>("controls-hint").text = $"{forward} {left} {backward} {right} · Move    Mouse · Look";
        }

        private void ExitToMenu()
        {
            _game.SaveProgress();
            SceneFlow.OpenMenu();
        }

        private void Restart()
        {
            _game.RestartNight();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _game.Session != null) Pause();
        }

        private void OnDestroy()
        {
            _settingsView?.Dispose();
            _pauseAction.action.performed -= OnPause;
            _pauseAction.action.Disable();
            _game.Session.Completed -= ShowResults;
            _ghost.ActionRequested -= ShowAction;
            _settings.Changed -= RefreshControls;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
