using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
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
        private readonly System.Collections.Generic.List<InputDevice> _externalDevices = new System.Collections.Generic.List<InputDevice>();
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Gamepad _gamepad;
        private Gamepad _otherGamepad;
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
        public void LegacySettingsRetainNewGamepadDefaultsAndClampInvalidValues()
        {
            var settings = new UserSettings();

            JsonUtility.FromJsonOverwrite("{\"Sensitivity\":2.5,\"FieldOfView\":80}", settings);
            settings.Validate();

            Assert.AreEqual(2.5f, settings.Sensitivity);
            Assert.AreEqual(1f, settings.GamepadSensitivity);
            Assert.IsFalse(settings.InvertGamepadY);
            Assert.AreEqual(0.2f, settings.MoveDeadzone);
            Assert.AreEqual(0.2f, settings.LookDeadzone);

            settings.GamepadSensitivity = float.NaN;
            settings.MoveDeadzone = -1f;
            settings.LookDeadzone = float.PositiveInfinity;
            settings.Validate();

            Assert.AreEqual(1f, settings.GamepadSensitivity);
            Assert.AreEqual(0.05f, settings.MoveDeadzone);
            Assert.AreEqual(0.2f, settings.LookDeadzone);
        }

        [Test]
        public void GamepadBindingConflictChecksOnlyGamepadAndReservesMenuControls()
        {
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InternalAssets/Input/InputSystem_Actions.inputactions");
            var input = Object.Instantiate(source);
            var primary = ControlBindings.GamepadSlots[0];
            var action = input.FindAction(primary.Action, true);
            int index = ControlBindings.Index(action, primary);

            try
            {
                action.ApplyBindingOverride(index, "<Gamepad>/leftTrigger");
                Assert.IsTrue(ControlBindings.HasConflict(input, primary));

                action.ApplyBindingOverride(index, "<Gamepad>/rightShoulder");
                Assert.IsFalse(ControlBindings.HasConflict(input, primary));
                Assert.IsTrue(ControlBindings.IsReservedGamepadControl("<Gamepad>/buttonSouth"));
                Assert.IsTrue(ControlBindings.IsReservedGamepadControl("<Gamepad>/start"));
                Assert.IsFalse(ControlBindings.IsReservedGamepadControl("<Gamepad>/rightShoulder"));
            }
            finally
            {
                Object.DestroyImmediate(input);
            }
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
            DisableExternalInput();
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

        [UnityTest]
        public IEnumerator GamepadIntentMenusGameplayAndDisconnectWorkTogether()
        {
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
            DisableExternalInput();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            yield return null;

            var menu = Object.FindAnyObjectByType<MenuScreen>();
            var settings = Object.FindAnyObjectByType<SettingsService>();
            var document = menu.GetComponent<UIDocument>();
            settings.RestoreDefaults();

            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice,
                "Connecting a controller must not take over the UI.");
            InputSystem.QueueDeltaStateEvent(_gamepad.leftStick, new Vector2(0.12f, 0f));
            yield return null;
            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice,
                "Stick drift must not take over the UI.");

            SendGamepadButton(GamepadButton.North, true);
            yield return null;
            Assert.AreEqual(ControlDeviceKind.Gamepad, settings.ActiveDevice);
            Assert.AreSame(_gamepad, settings.ActiveGamepad);
            SendGamepadButton(GamepadButton.North, false);
            yield return null;

            SendGamepadButton(GamepadButton.DpadDown, true);
            yield return null;
            SendGamepadButton(GamepadButton.DpadDown, false);
            yield return null;
            Assert.AreSame(document.rootVisualElement.Q<Button>("settings-button"),
                document.rootVisualElement.panel.focusController.focusedElement,
                "D-pad navigation should move focus to Settings.");
            SendGamepadButton(GamepadButton.South, true);
            yield return null;
            SendGamepadButton(GamepadButton.South, false);
            yield return null;
            Assert.IsTrue(menu.Settings.IsOpen, "A should submit the focused menu button.");
            Assert.AreEqual(ControlDeviceKind.Gamepad, menu.Settings.SelectedDeviceTab);

            menu.Settings.SelectDeviceTab(ControlDeviceKind.KeyboardMouse);
            settings.SetSensitivity(2.5f);
            settings.SetGamepadSensitivity(3f);
            settings.SetInvertGamepadY(true);
            settings.SetMoveDeadzone(0.3f);
            settings.SetLookDeadzone(0.4f);
            var keyboardSlot = ControlBindings.KeyboardMouseSlots[0];
            var gamepadSlot = ControlBindings.GamepadSlots[0];
            var move = settings.Input.FindAction(keyboardSlot.Action, true);
            var attack = settings.Input.FindAction(gamepadSlot.Action, true);
            move.ApplyBindingOverride(ControlBindings.Index(move, keyboardSlot), "<Keyboard>/i");
            attack.ApplyBindingOverride(ControlBindings.Index(attack, gamepadSlot), "<Gamepad>/rightShoulder");
            settings.SaveBindings();

            settings.ResetDevice(ControlDeviceKind.Gamepad);
            Assert.AreEqual(2.5f, settings.Sensitivity);
            Assert.AreEqual(1f, settings.GamepadSensitivity);
            Assert.IsFalse(settings.InvertGamepadY);
            Assert.AreEqual(0.2f, settings.MoveDeadzone);
            Assert.AreEqual(0.2f, settings.LookDeadzone);
            Assert.AreEqual("<Keyboard>/i", move.bindings[ControlBindings.Index(move, keyboardSlot)].effectivePath);
            Assert.AreEqual(gamepadSlot.DefaultPath, attack.bindings[ControlBindings.Index(attack, gamepadSlot)].effectivePath);
            menu.Settings.SelectDeviceTab(ControlDeviceKind.Gamepad);
            Assert.AreEqual(ControlDeviceKind.Gamepad, menu.Settings.SelectedDeviceTab,
                "The player can select a tab independently of the active input device.");

            SendKeys(Key.W);
            yield return null;
            SendKeys();
            yield return null;
            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice);
            Assert.AreEqual(ControlDeviceKind.Gamepad, menu.Settings.SelectedDeviceTab,
                "A manually chosen settings tab must stay pinned when the active device changes.");

            menu.Settings.BeginBinding(gamepadSlot);
            SendKeys(Key.K);
            yield return null;
            SendKeys();
            yield return null;
            Assert.IsTrue(menu.Settings.IsRebinding, "Keyboard input cannot bind a gamepad slot.");
            SendGamepadButton(GamepadButton.RightShoulder, true);
            yield return new WaitForSecondsRealtime(0.2f);
            SendGamepadButton(GamepadButton.RightShoulder, false);
            yield return null;
            yield return null;
            Assert.IsFalse(menu.Settings.IsRebinding);
            Assert.AreEqual("<Gamepad>/rightShoulder", attack.bindings[ControlBindings.Index(attack, gamepadSlot)].effectivePath);
            Assert.IsTrue(PlayerPrefs.GetString(SettingsService.PreferenceKey).Contains("rightShoulder"));

            menu.Settings.BeginBinding(gamepadSlot);
            SendGamepadButton(GamepadButton.East, true);
            yield return null;
            SendGamepadButton(GamepadButton.East, false);
            yield return null;
            yield return null;
            Assert.IsFalse(menu.Settings.IsRebinding);
            Assert.IsTrue(menu.Settings.IsOpen, "Cancelling rebinding must not also close settings.");
            Assert.AreEqual("<Gamepad>/rightShoulder", attack.bindings[ControlBindings.Index(attack, gamepadSlot)].effectivePath);
            settings.ResetDevice(ControlDeviceKind.Gamepad);

            SendGamepadButton(GamepadButton.East, true);
            yield return null;
            SendGamepadButton(GamepadButton.East, false);
            yield return null;
            Assert.IsFalse(menu.Settings.IsOpen, "B should leave settings.");
            Assert.AreSame(document.rootVisualElement.Q<Button>("settings-button"),
                document.rootVisualElement.panel.focusController.focusedElement);

            Submit(document, "play-button");
            yield return null;
            yield return null;
            Assert.AreEqual(SceneFlow.GameplayScene, SceneManager.GetActiveScene().name);

            settings = Object.FindAnyObjectByType<SettingsService>();
            var game = Object.FindAnyObjectByType<GameBootstrapper>();
            var screen = Object.FindAnyObjectByType<GameplayScreen>();
            var ghost = Object.FindAnyObjectByType<GhostController>();
            document = screen.GetComponent<UIDocument>();
            Vector3 before = ghost.transform.position;
            Quaternion facing = ghost.transform.rotation;
            _requestedAction = null;
            ghost.ActionRequested += RecordAction;

            InputSystem.QueueDeltaStateEvent(_gamepad.leftStick, new Vector2(0f, 1f));
            InputSystem.QueueDeltaStateEvent(_gamepad.rightStick, new Vector2(1f, 0f));
            yield return null;
            yield return null;
            Assert.Greater(Vector3.Distance(before, ghost.transform.position), 0.001f);
            Assert.Greater(Quaternion.Angle(facing, ghost.transform.rotation), 0.1f);

            InputSystem.QueueDeltaStateEvent(_gamepad.rightTrigger, 1f);
            yield return null;
            Assert.AreEqual("Primary action", _requestedAction);
            InputSystem.QueueDeltaStateEvent(_gamepad.rightTrigger, 0f);
            yield return null;
            InputSystem.QueueDeltaStateEvent(_gamepad.leftTrigger, 1f);
            yield return null;
            Assert.AreEqual("Secondary action", _requestedAction);
            InputSystem.QueueDeltaStateEvent(_gamepad.leftTrigger, 0f);
            InputSystem.QueueDeltaStateEvent(_gamepad.leftStick, Vector2.zero);
            InputSystem.QueueDeltaStateEvent(_gamepad.rightStick, Vector2.zero);
            yield return null;

            settings.SetLookDeadzone(0.05f);
            Assert.AreEqual(ControlDeviceKind.Gamepad, settings.ActiveDevice,
                "The small-deadzone check requires the controller to remain the active input device.");
            facing = ghost.transform.rotation;
            InputSystem.QueueDeltaStateEvent(_gamepad.rightStick, new Vector2(0.08f, 0f));
            yield return new WaitForSeconds(0.15f);
            Assert.Greater((ghost.transform.forward - facing * Vector3.forward).sqrMagnitude, 1e-12f,
                "A user deadzone below the Input System default must actually allow small stick input.");
            InputSystem.QueueDeltaStateEvent(_gamepad.rightStick, Vector2.zero);
            yield return null;

            SendGamepadButton(GamepadButton.Start, true);
            yield return null;
            Assert.AreEqual(SessionState.Paused, game.Session.State);
            SendGamepadButton(GamepadButton.Start, false);
            yield return null;
            SendGamepadButton(GamepadButton.East, true);
            yield return null;
            Assert.AreEqual(SessionState.Running, game.Session.State, "B should resume from pause.");
            SendGamepadButton(GamepadButton.East, false);
            yield return null;

            InputSystem.RemoveDevice(_gamepad);
            _gamepad = null;
            yield return null;
            Assert.AreEqual(SessionState.Paused, game.Session.State);
            Assert.IsFalse(document.rootVisualElement.Q("disconnect-notice").ClassListContains("hidden"),
                "Disconnecting the active controller should show a recovery message.");

            SendKeys(Key.Escape);
            yield return null;
            SendKeys();
            yield return null;
            Assert.AreEqual(SessionState.Running, game.Session.State, "Keyboard can resume after disconnection.");
            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice);
            var unusedPad = InputSystem.AddDevice<Gamepad>();
            yield return null;
            InputSystem.RemoveDevice(unusedPad);
            yield return null;
            Assert.AreEqual(SessionState.Running, game.Session.State, "Disconnecting an unused pad must not pause.");

            RestorePreferences();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator SettingsGamepadNavigationReachesClippedRowsWithoutFooterOverlap()
        {
            EditorSceneManager.OpenScene("Assets/InternalAssets/Scenes/SCN_Menu.unity");
            yield return new EnterPlayMode();
            yield return null;
            _originalInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            _originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _inputConfigured = true;
            _hadPreferences = PlayerPrefs.HasKey(SettingsService.PreferenceKey);
            _originalPreferences = PlayerPrefs.GetString(SettingsService.PreferenceKey, "");
            _preferencesCaptured = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            DisableExternalInput();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            yield return null;
            var menu = Object.FindAnyObjectByType<MenuScreen>();
            var root = menu.GetComponent<UIDocument>().rootVisualElement;
            var settings = Object.FindAnyObjectByType<SettingsService>();
            settings.RestoreDefaults();
            SendGamepadButton(GamepadButton.North, true);
            yield return null;
            SendGamepadButton(GamepadButton.North, false);
            yield return null;
            menu.OpenSettings();
            var screen = root.Q("settings-screen");
            screen.style.height = 520;
            screen.style.bottom = StyleKeyword.Auto;
            var scroll = root.Q<ScrollView>("settings-scroll");
            var footer = root.Q(className: "settings-actions");
            var card = root.Q(className: "settings-card");
            string[] common = { "master-slider", "music-slider", "fov-slider", "keyboard-tab", "gamepad-tab" };
            string[] gamepadControls = { "gamepad-sensitivity-slider", "gamepad-invert-y", "move-deadzone-slider",
                "look-deadzone-slider", "bind-gamepad-primary", "bind-gamepad-secondary" };
            string[] keyboardControls = { "sensitivity-slider", "bind-forward", "bind-backward", "bind-left",
                "bind-right", "bind-primary", "bind-secondary" };
            string[] footerControls = { "defaults-button", "common-defaults-button", "settings-back-button" };
            foreach (var kind in new[] { ControlDeviceKind.Gamepad, ControlDeviceKind.KeyboardMouse })
            {
                menu.Settings.SelectDeviceTab(kind);
                yield return null;
                yield return null;
                Assert.Greater(scroll.contentContainer.worldBound.height, scroll.contentViewport.worldBound.height,
                    "This regression must exercise content that needs scrolling.");
                Assert.GreaterOrEqual(card.worldBound.xMin, screen.worldBound.xMin - 1f);
                Assert.LessOrEqual(card.worldBound.xMax, screen.worldBound.xMax + 1f,
                    "Settings must fit horizontally in a narrow Game View.");
                Assert.GreaterOrEqual(footer.worldBound.yMin, scroll.worldBound.yMax - 1f,
                    "The footer must not overlap the scroll area on either device tab.");
                Assert.LessOrEqual(footer.worldBound.yMax, card.worldBound.yMax - card.resolvedStyle.paddingBottom + 1f);
                float previousRight = footer.worldBound.xMin;
                foreach (var name in footerControls)
                {
                    var button = root.Q<Button>(name);
                    Assert.GreaterOrEqual(button.worldBound.xMin, previousRight - 1f, name + " must not overlap its neighbour.");
                    Assert.LessOrEqual(button.worldBound.xMax, footer.worldBound.xMax + 1f, name + " must fit inside the footer.");
                    previousRight = button.worldBound.xMax;
                }
                foreach (var row in root.Query<VisualElement>(className: "binding-row").ToList())
                {
                    if (row.worldBound.height <= 0f) continue;
                    var label = row.Q<Label>();
                    var button = row.Q<Button>();
                    Assert.LessOrEqual(label.worldBound.xMax, button.worldBound.xMin + 1f,
                        "Binding labels must not overlap their buttons.");
                    Assert.GreaterOrEqual(label.worldBound.xMin, row.worldBound.xMin - 1f);
                }
                var order = new System.Collections.Generic.List<string>(common);
                order.AddRange(kind == ControlDeviceKind.Gamepad ? gamepadControls : keyboardControls);
                order.AddRange(footerControls);
                root.Q(order[0]).Focus();
                yield return null;
                for (int i = 1; i < order.Count; i++)
                {
                    SendGamepadButton(GamepadButton.DpadDown, true);
                    yield return null;
                    SendGamepadButton(GamepadButton.DpadDown, false);
                    yield return null;
                    yield return null;
                    var target = root.Q(order[i]);
                    var focused = root.panel.focusController.focusedElement as VisualElement;
                    Assert.IsTrue(focused == target || target.Contains(focused),
                        "Down must reach " + order[i] + " on " + kind + "; got " + focused?.name);
                    if (scroll.contentContainer.Contains(target))
                    {
                        Assert.GreaterOrEqual(target.worldBound.yMin, scroll.contentViewport.worldBound.yMin - 1f,
                            order[i] + " must not be clipped at the top.");
                        Assert.LessOrEqual(target.worldBound.yMax, scroll.contentViewport.worldBound.yMax + 1f,
                            order[i] + " must scroll fully into view.");
                    }
                }
                Assert.Greater(scroll.scrollOffset.y, 0f, "Navigation must scroll without mouse input.");
                for (int i = order.Count - 2; i >= 0; i--)
                {
                    SendGamepadButton(GamepadButton.DpadUp, true);
                    yield return null;
                    SendGamepadButton(GamepadButton.DpadUp, false);
                    yield return null;
                    var target = root.Q(order[i]);
                    var focused = root.panel.focusController.focusedElement as VisualElement;
                    Assert.IsTrue(focused == target || target.Contains(focused), "Up must reach " + order[i]);
                }
                var slider = root.Q<Slider>("master-slider");
                float before = slider.value;
                SendGamepadButton(GamepadButton.DpadRight, true);
                yield return null;
                SendGamepadButton(GamepadButton.DpadRight, false);
                yield return null;
                Assert.Greater(slider.value, before, "Horizontal navigation must still adjust sliders.");
            }
            menu.Settings.SelectDeviceTab(ControlDeviceKind.Gamepad);
            root.Q("bind-gamepad-secondary").Focus();
            yield return null;
            yield return null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "halloween-settings-gamepad.png"));
            yield return null;
            yield return null;
            RestorePreferences();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator HeldGamepadStickDoesNotBlockReboundKeyboardMovement()
        {
            EditorSceneManager.OpenScene("Assets/InternalAssets/Scenes/SCN_Main.unity");
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
            DisableExternalInput();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            yield return null;

            var settings = Object.FindAnyObjectByType<SettingsService>();
            var ghost = Object.FindAnyObjectByType<GhostController>();
            Assert.IsNotNull(settings);
            Assert.IsNotNull(ghost);
            settings.RestoreDefaults();
            var forwardSlot = ControlBindings.KeyboardMouseSlots[0];
            var move = settings.Input.FindAction(forwardSlot.Action, true);
            move.ApplyBindingOverride(ControlBindings.Index(move, forwardSlot), "<Keyboard>/i");
            settings.SaveBindings();

            InputSystem.QueueDeltaStateEvent(_gamepad.leftStick, Vector2.right);
            yield return null;
            yield return null;
            Assert.AreEqual(ControlDeviceKind.Gamepad, settings.ActiveDevice);
            var before = ghost.transform.position;
            var forward = ghost.transform.forward;
            var right = ghost.transform.right;

            SendKeys(Key.I);
            yield return null;
            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice);
            yield return null;
            yield return null;
            var traveled = ghost.transform.position - before;
            Assert.Greater(Vector3.Dot(traveled, forward), 0.001f,
                "The rebound forward key must move the ghost while the gamepad stick remains fully held.");
            Assert.Less(Mathf.Abs(Vector3.Dot(traveled, right)), 0.001f,
                "The held gamepad stick must not steer after keyboard intent takes over.");

            SendKeys();
            RestorePreferences();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator SettingsBindingLabelsRefreshWhenActiveGamepadChanges()
        {
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
            DisableExternalInput();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            yield return null;

            var menu = Object.FindAnyObjectByType<MenuScreen>();
            var settings = Object.FindAnyObjectByType<SettingsService>();
            var root = menu.GetComponent<UIDocument>().rootVisualElement;
            settings.RestoreDefaults();
            Assert.AreEqual(ControlDeviceKind.KeyboardMouse, settings.ActiveDevice);
            menu.OpenSettings();
            menu.Settings.SelectDeviceTab(ControlDeviceKind.Gamepad);
            var primary = root.Q<Button>("bind-gamepad-primary");
            var secondary = root.Q<Button>("bind-gamepad-secondary");

            DescribeGamepad(_gamepad, "DualShock 4", "Sony");
            SendGamepadButton(GamepadButton.North, true);
            yield return null;
            Assert.AreSame(_gamepad, settings.ActiveGamepad);
            Assert.AreEqual("R2", primary.text);
            Assert.AreEqual("L2", secondary.text);
            SendGamepadButton(GamepadButton.North, false);
            yield return null;

            _otherGamepad = InputSystem.AddDevice<Gamepad>();
            DescribeGamepad(_otherGamepad, "Xbox Controller", "Microsoft");
            InputSystem.QueueStateEvent(_otherGamepad, new GamepadState().WithButton(GamepadButton.North));
            yield return null;
            Assert.AreSame(_otherGamepad, settings.ActiveGamepad);
            Assert.AreEqual(ControlDeviceKind.Gamepad, menu.Settings.SelectedDeviceTab);
            Assert.AreEqual("RT", primary.text);
            Assert.AreEqual("LT", secondary.text);

            RestorePreferences();
            yield return new ExitPlayMode();
        }

        private static void DescribeGamepad(Gamepad pad, string product, string manufacturer)
        {
            var descriptionField = typeof(InputDevice).GetField("m_Description",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(descriptionField, "The test needs an Input System device description to model controller brands.");
            descriptionField.SetValue(pad, new InputDeviceDescription { product = product, manufacturer = manufacturer });
        }

        private void DisableExternalInput()
        {
            foreach (var device in InputSystem.devices)
            {
                if (!device.enabled || !(device is Keyboard || device is Mouse || device is Gamepad)) continue;
                _externalDevices.Add(device);
                InputSystem.DisableDevice(device);
            }
        }

        private void SendGamepadButton(GamepadButton button, bool pressed)
        {
            var state = new GamepadState
            {
                leftStick = _gamepad.leftStick.ReadUnprocessedValue(),
                rightStick = _gamepad.rightStick.ReadUnprocessedValue(),
                leftTrigger = _gamepad.leftTrigger.ReadValue(),
                rightTrigger = _gamepad.rightTrigger.ReadValue()
            }.WithButton(button, pressed);
            InputSystem.QueueStateEvent(_gamepad, state);
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
            if (_gamepad != null && _gamepad.added) InputSystem.RemoveDevice(_gamepad);
            if (_otherGamepad != null && _otherGamepad.added) InputSystem.RemoveDevice(_otherGamepad);

            RestorePreferences();
            foreach (var device in _externalDevices)
                if (device.added) InputSystem.EnableDevice(device);
            _externalDevices.Clear();

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
