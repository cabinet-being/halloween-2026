using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace Halloween2026.Tests
{
    public sealed class ScreenFlowTests
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private bool _preferencesCaptured;
        private bool _hadPreferences;
        private string _originalPreferences;
        private InputSettings.EditorInputBehaviorInPlayMode _originalInputBehavior;
        private InputSettings.BackgroundBehavior _originalBackgroundBehavior;
        private bool _inputConfigured;
        private string _requestedAction;

        [Test]
        public void SettingsClampAndRecoverNonFiniteValues()
        {
            var settings = new UserSettings
            {
                MasterVolume = -1f,
                MusicVolume = 5f,
                Sensitivity = float.NaN,
                FieldOfView = float.PositiveInfinity
            };

            settings.Validate();

            Assert.AreEqual(0f, settings.MasterVolume);
            Assert.AreEqual(1f, settings.MusicVolume);
            Assert.AreEqual(1f, settings.Sensitivity);
            Assert.AreEqual(65f, settings.FieldOfView);
        }

        [Test]
        public void ConflictingAndReservedBindingsAreRejected()
        {
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InternalAssets/Input/InputSystem_Actions.inputactions");
            var input = Object.Instantiate(source);
            var forward = ControlBindings.Slots[0];
            var move = input.FindAction(forward.Action, true);
            int index = ControlBindings.Index(move, forward);

            try
            {
                input.RemoveAllBindingOverrides();
                move.ApplyBindingOverride(index, "<Keyboard>/s");

                Assert.IsTrue(ControlBindings.HasConflict(input, forward));

                move.ApplyBindingOverride(index, "<Keyboard>/escape");

                Assert.IsTrue(ControlBindings.HasConflict(input, forward));

                move.ApplyBindingOverride(index, "<Keyboard>/i");

                Assert.IsFalse(ControlBindings.HasConflict(input, forward));
            }
            finally
            {
                Object.DestroyImmediate(input);
            }
        }

        [UnityTest]
        public IEnumerator MenuSettingsGameplayPauseRebindAndReturnWorkTogether()
        {
            MenuScreen menu;
            GameplayScreen screen;
            SettingsService settings;
            GameBootstrapper game;
            UIDocument document;
            float pausedAt;
            Vector3 position;
            Quaternion rotation;
            GhostController ghost;
            InputAction move;
            int binding;

            EditorSceneManager.OpenScene("Assets/InternalAssets/Scenes/SCN_Menu.unity");
            yield return new EnterPlayMode();
            yield return null;

            _hadPreferences = PlayerPrefs.HasKey(SettingsService.PreferenceKey);
            _originalPreferences = PlayerPrefs.GetString(SettingsService.PreferenceKey, "");
            _preferencesCaptured = true;
            _originalInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            _originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _inputConfigured = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            menu = Object.FindAnyObjectByType<MenuScreen>();
            settings = Object.FindAnyObjectByType<SettingsService>();
            document = menu.GetComponent<UIDocument>();
            settings.RestoreDefaults();

            Assert.IsNotNull(menu);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

            Submit(document, "settings-button");
            yield return null;
            document.rootVisualElement.Q<Slider>("master-slider").value = 35f;
            document.rootVisualElement.Q<Slider>("music-slider").value = 20f;
            document.rootVisualElement.Q<Slider>("sensitivity-slider").value = 2f;
            document.rootVisualElement.Q<Slider>("fov-slider").value = 82f;

            Assert.IsTrue(menu.Settings.IsOpen);
            Assert.AreEqual(0.35f, AudioListener.volume, 0.001f);
            Assert.AreEqual(0.2f, settings.MusicVolume, 0.001f);
            Assert.AreEqual(0.06f, Object.FindAnyObjectByType<MusicVolumeApplier>().GetComponent<AudioSource>().volume, 0.001f);

            Submit(document, "settings-back-button");
            yield return null;
            Submit(document, "play-button");
            yield return null;
            yield return null;

            Assert.AreEqual(SceneFlow.GameplayScene, SceneManager.GetActiveScene().name);

            game = Object.FindAnyObjectByType<GameBootstrapper>();
            screen = Object.FindAnyObjectByType<GameplayScreen>();
            settings = Object.FindAnyObjectByType<SettingsService>();
            ghost = Object.FindAnyObjectByType<GhostController>();
            document = screen.GetComponent<UIDocument>();
            position = ghost.transform.position;
            rotation = ghost.transform.rotation;

            Assert.AreEqual(82f, Camera.main.fieldOfView, 0.001f);
            Assert.AreEqual(2f, settings.Sensitivity);

            SendKeys(Key.W);
            yield return null;
            yield return null;
            SendKeys();
            InputSystem.QueueDeltaStateEvent(_mouse.delta, new Vector2(60f, 0f));
            yield return null;
            yield return null;

            Assert.Greater(ghost.transform.position.z - position.z, 0.001f);
            Assert.Greater(Quaternion.Angle(rotation, ghost.transform.rotation), 0.1f);

            _requestedAction = null;
            ghost.ActionRequested += RecordAction;
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;

            Assert.AreEqual("Primary action", _requestedAction);

            InputSystem.QueueStateEvent(_mouse, new MouseState());
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right));
            yield return null;
            yield return null;

            Assert.AreEqual("Secondary action", _requestedAction);

            InputSystem.QueueStateEvent(_mouse, new MouseState());
            yield return null;

            SendKeys(Key.Escape);
            yield return null;
            SendKeys();
            yield return null;
            pausedAt = game.Session.Elapsed;
            position = ghost.transform.position;

            Assert.AreEqual(SessionState.Paused, game.Session.State);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

            Submit(document, "settings-button");
            yield return null;
            document.rootVisualElement.Q<Slider>("fov-slider").value = 75f;
            screen.Settings.BeginBinding(ControlBindings.Slots[0]);
            SendKeys(Key.I);
            yield return new WaitForSecondsRealtime(0.2f);
            SendKeys();
            yield return null;
            move = settings.Input.FindAction("Player/Move", true);
            binding = ControlBindings.Index(move, ControlBindings.Slots[0]);

            Assert.AreEqual(75f, Camera.main.fieldOfView);
            Assert.AreEqual(pausedAt, game.Session.Elapsed);
            Assert.AreEqual(position, ghost.transform.position);
            Assert.AreEqual("<Keyboard>/i", move.bindings[binding].effectivePath);

            screen.Settings.BeginBinding(ControlBindings.Slots[0]);
            SendKeys(Key.Escape);
            yield return null;
            SendKeys();
            yield return null;
            yield return null;

            Assert.IsFalse(screen.Settings.IsRebinding);
            Assert.IsTrue(screen.Settings.IsOpen);
            Assert.AreEqual("<Keyboard>/i", move.bindings[binding].effectivePath);

            SendKeys(Key.Escape);
            yield return null;
            SendKeys();
            yield return null;

            Assert.IsFalse(screen.Settings.IsOpen);
            Assert.AreEqual(SessionState.Paused, game.Session.State);

            Submit(document, "resume-button");
            yield return null;

            Assert.AreEqual(SessionState.Running, game.Session.State);
            Assert.AreEqual(1f, Time.timeScale);

            screen.Pause();
            Submit(document, "menu-button");
            yield return null;
            yield return null;

            Assert.AreEqual(SceneFlow.MenuScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(1f, Time.timeScale);

            menu = Object.FindAnyObjectByType<MenuScreen>();
            settings = Object.FindAnyObjectByType<SettingsService>();
            move = settings.Input.FindAction("Player/Move", true);
            binding = ControlBindings.Index(move, ControlBindings.Slots[0]);

            Assert.AreEqual(75f, settings.FieldOfView);
            Assert.AreEqual("<Keyboard>/i", move.bindings[binding].effectivePath);

            RestorePreferences();
            yield return new ExitPlayMode();
        }

        private void RecordAction(string action)
        {
            _requestedAction = action;
        }

        private void SendKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        }

        private static void Submit(UIDocument document, string name)
        {
            NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            Button button = document.rootVisualElement.Q<Button>(name);

            using (submit)
            {
                button.Focus();
                button.SendEvent(submit);
            }
        }

        private void RestorePreferences()
        {
            if (_preferencesCaptured)
            {
                if (_hadPreferences) PlayerPrefs.SetString(SettingsService.PreferenceKey, _originalPreferences);
                else PlayerPrefs.DeleteKey(SettingsService.PreferenceKey);

                PlayerPrefs.Save();
            }

            _preferencesCaptured = false;

            if (_inputConfigured)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = _originalInputBehavior;
                InputSystem.settings.backgroundBehavior = _originalBackgroundBehavior;
                _inputConfigured = false;
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);

            RestorePreferences();

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
