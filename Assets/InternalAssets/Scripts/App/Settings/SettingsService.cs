using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Halloween2026
{
    public enum ControlDeviceKind { KeyboardMouse, Gamepad }

    [DefaultExecutionOrder(-200)]
    public sealed class SettingsService : MonoBehaviour
    {
        public const string PreferenceKey = "Halloween2026.Settings.v1";

        [SerializeField, Tooltip("The authored Input Actions asset, shared with gameplay action references.")]
        private InputActionAsset _input;

        private UserSettings _values;
        private readonly Dictionary<int, bool> _leftStickActive = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> _rightStickActive = new Dictionary<int, bool>();
        private Gamepad _activeGamepad;
        private static ControlDeviceKind _lastDevice = ControlDeviceKind.KeyboardMouse;
        private static int _lastGamepadId = -1;

        public float MasterVolume => _values.MasterVolume;
        public float MusicVolume => _values.MusicVolume;
        public float Sensitivity => _values.Sensitivity;
        public float GamepadSensitivity => _values.GamepadSensitivity;
        public bool InvertGamepadY => _values.InvertGamepadY;
        public float MoveDeadzone => _values.MoveDeadzone;
        public float LookDeadzone => _values.LookDeadzone;
        public float FieldOfView => _values.FieldOfView;
        public InputActionAsset Input => _input;
        public ControlDeviceKind ActiveDevice { get; private set; } = ControlDeviceKind.KeyboardMouse;
        public Gamepad ActiveGamepad => _activeGamepad;
        public bool SuppressDeviceIntent { get; set; }
        public event Action Changed;
        public event Action<ControlDeviceKind> ActiveDeviceChanged;
        public event Action ActiveGamepadDisconnected;

        private void Awake()
        {
            string json = PlayerPrefs.GetString(PreferenceKey, "");

            _values = new UserSettings();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, _values);
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
            if (_lastDevice == ControlDeviceKind.Gamepad)
            {
                foreach (var pad in Gamepad.all)
                    if (pad.deviceId == _lastGamepadId)
                    {
                        _activeGamepad = pad;
                        ActiveDevice = ControlDeviceKind.Gamepad;
                        break;
                    }
            }
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void Update()
        {
            if (SuppressDeviceIntent) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 9f)))
                SetActiveDevice(ControlDeviceKind.KeyboardMouse);

            foreach (var pad in Gamepad.all)
            {
                bool left = pad.leftStick.ReadUnprocessedValue().magnitude > Mathf.Max(0.35f, _values.MoveDeadzone);
                bool right = pad.rightStick.ReadUnprocessedValue().magnitude > Mathf.Max(0.35f, _values.LookDeadzone);
                bool wasLeft = _leftStickActive.TryGetValue(pad.deviceId, out bool oldLeft) && oldLeft;
                bool wasRight = _rightStickActive.TryGetValue(pad.deviceId, out bool oldRight) && oldRight;
                bool pressed = pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                    || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                    || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame
                    || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                    || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
                    || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                    || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
                if (pressed || (left && !wasLeft) || (right && !wasRight))
                {
                    bool changedPad = _activeGamepad != pad;
                    _activeGamepad = pad;
                    _lastGamepadId = pad.deviceId;
                    if (ActiveDevice == ControlDeviceKind.Gamepad)
                    {
                        if (changedPad) ActiveDeviceChanged?.Invoke(ControlDeviceKind.Gamepad);
                    }
                    else SetActiveDevice(ControlDeviceKind.Gamepad);
                }
                _leftStickActive[pad.deviceId] = left;
                _rightStickActive[pad.deviceId] = right;
            }
        }

        private void SetActiveDevice(ControlDeviceKind device)
        {
            if (ActiveDevice == device) return;
            ActiveDevice = device;
            _lastDevice = device;
            ActiveDeviceChanged?.Invoke(device);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device != _activeGamepad || (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed)) return;
            _activeGamepad = null;
            _lastGamepadId = -1;
            _leftStickActive.Remove(device.deviceId);
            _rightStickActive.Remove(device.deviceId);
            if (ActiveDevice == ControlDeviceKind.Gamepad) ActiveGamepadDisconnected?.Invoke();
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

        public void SetGamepadSensitivity(float value) { _values.GamepadSensitivity = value; Commit(); }
        public void SetInvertGamepadY(bool value) { _values.InvertGamepadY = value; Commit(); }
        public void SetMoveDeadzone(float value) { _values.MoveDeadzone = value; Commit(); }
        public void SetLookDeadzone(float value) { _values.LookDeadzone = value; Commit(); }

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

        public void ResetBindings(ControlDeviceKind device)
        {
            foreach (var slot in device == ControlDeviceKind.Gamepad ? ControlBindings.GamepadSlots : ControlBindings.KeyboardMouseSlots)
            {
                var action = _input.FindAction(slot.Action, true);
                action.RemoveBindingOverride(ControlBindings.Index(action, slot));
            }
            SaveBindings();
        }

        public void ResetDevice(ControlDeviceKind device)
        {
            if (device == ControlDeviceKind.Gamepad)
            {
                _values.GamepadSensitivity = 1f;
                _values.InvertGamepadY = false;
                _values.MoveDeadzone = 0.2f;
                _values.LookDeadzone = 0.2f;
            }
            else _values.Sensitivity = 1f;
            ResetBindings(device);
        }

        public void ResetCommon()
        {
            _values.MasterVolume = 0.8f;
            _values.MusicVolume = 0.5f;
            _values.FieldOfView = 65f;
            Commit();
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
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
