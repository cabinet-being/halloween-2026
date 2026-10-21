using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Halloween2026
{
    public sealed class SettingsView : IDisposable
    {
        private readonly SettingsService _settings;
        private readonly VisualElement _screen;
        private readonly Action _back;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private InputAction _rebindingAction;
        private ControlBindings.Slot _slot;
        private int _bindingIndex;
        private string _oldOverride;
        private bool _wasEnabled;

        public bool IsOpen => !_screen.ClassListContains("hidden");
        public bool IsRebinding => _operation != null;
        public int IgnoreBackUntilFrame { get; private set; }

        public SettingsView(SettingsService settings, VisualElement root, VisualTreeAsset layout, Action back)
        {
            _settings = settings;
            _back = back;
            layout.CloneTree(root);
            _screen = root.Q("settings-screen");

            _screen.Q<Slider>("master-slider").RegisterValueChangedCallback(evt => _settings.SetMasterVolume(evt.newValue / 100f));
            _screen.Q<Slider>("music-slider").RegisterValueChangedCallback(evt => _settings.SetMusicVolume(evt.newValue / 100f));
            _screen.Q<Slider>("sensitivity-slider").RegisterValueChangedCallback(evt => _settings.SetSensitivity(evt.newValue));
            _screen.Q<Slider>("fov-slider").RegisterValueChangedCallback(evt => _settings.SetFieldOfView(evt.newValue));
            _screen.Q<Button>("settings-back-button").clicked += Back;
            _screen.Q<Button>("defaults-button").clicked += RestoreDefaults;

            foreach (var slot in ControlBindings.Slots)
                _screen.Q<Button>("bind-" + slot.Name).clicked += () => BeginBinding(slot);

            _settings.Changed += Refresh;
            Refresh();
        }

        public void Show()
        {
            _screen.RemoveFromClassList("hidden");
            _screen.Q<Button>("settings-back-button").Focus();
        }

        public void Hide()
        {
            CancelBinding();
            _screen.AddToClassList("hidden");
        }

        public void Back()
        {
            if (IsRebinding)
            {
                CancelBinding();
                return;
            }

            Hide();
            _back();
        }

        public void BeginBinding(ControlBindings.Slot slot)
        {
            if (IsRebinding) return;

            _slot = slot;
            _rebindingAction = _settings.Input.FindAction(slot.Action, true);
            _bindingIndex = ControlBindings.Index(_rebindingAction, slot);
            _oldOverride = _rebindingAction.bindings[_bindingIndex].overridePath;
            _wasEnabled = _rebindingAction.enabled;
            _rebindingAction.Disable();

            SetStatus("Press a " + (slot.Action == "Player/Move" ? "keyboard key" : "key or mouse button") + ". Escape cancels.");
            _screen.Q<ScrollView>("settings-scroll").SetEnabled(false);
            _screen.Q<Button>("defaults-button").SetEnabled(false);

            _operation = _rebindingAction.PerformInteractiveRebinding(_bindingIndex)
                .WithExpectedControlType("Button")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Gamepad>/*")
                .WithControlsExcluding("<Joystick>/*")
                .WithControlsExcluding("<Touchscreen>/*")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithTimeout(10f)
                .OnCancel(_ => FinishBinding(false))
                .OnComplete(_ => FinishBinding(true));

            if (slot.Action == "Player/Move") _operation.WithControlsHavingToMatchPath("<Keyboard>/*");

            _operation.Start();
        }

        private void FinishBinding(bool completed)
        {
            bool conflict = completed && ControlBindings.HasConflict(_settings.Input, _slot);

            _operation.Dispose();
            _operation = null;
            IgnoreBackUntilFrame = Time.frameCount + 1;

            if (conflict)
            {
                if (_oldOverride == null) _rebindingAction.RemoveBindingOverride(_bindingIndex);
                else _rebindingAction.ApplyBindingOverride(_bindingIndex, _oldOverride);
            }

            if (_wasEnabled) _rebindingAction.Enable();

            _screen.Q<ScrollView>("settings-scroll").SetEnabled(true);
            _screen.Q<Button>("defaults-button").SetEnabled(true);

            if (completed && !conflict) _settings.SaveBindings();

            Refresh();
            SetStatus(conflict ? "That control is already in use. Your previous binding was kept."
                : completed ? "Control saved." : "Rebinding cancelled. Your previous control is unchanged.");
            _screen.Q<Button>("bind-" + _slot.Name).Focus();
        }

        private void CancelBinding()
        {
            _operation?.Cancel();
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            SetStatus("Default settings and controls restored.");
        }

        private void Refresh()
        {
            InputAction action;

            RefreshSlider("master", _settings.MasterVolume * 100f, Mathf.RoundToInt(_settings.MasterVolume * 100f) + "%");
            RefreshSlider("music", _settings.MusicVolume * 100f, Mathf.RoundToInt(_settings.MusicVolume * 100f) + "%");
            RefreshSlider("sensitivity", _settings.Sensitivity, _settings.Sensitivity.ToString("F2") + "×");
            RefreshSlider("fov", _settings.FieldOfView, Mathf.RoundToInt(_settings.FieldOfView) + "°");

            foreach (var slot in ControlBindings.Slots)
            {
                action = _settings.Input.FindAction(slot.Action, true);
                _screen.Q<Button>("bind-" + slot.Name).text = action.bindings[ControlBindings.Index(action, slot)].ToDisplayString();
            }
        }

        private void RefreshSlider(string name, float value, string label)
        {
            _screen.Q<Slider>(name + "-slider").SetValueWithoutNotify(value);
            _screen.Q<Label>(name + "-value").text = label;
        }

        private void SetStatus(string text)
        {
            _screen.Q<Label>("settings-status").text = text;
        }

        public void Dispose()
        {
            CancelBinding();
            _settings.Changed -= Refresh;
            _screen.RemoveFromHierarchy();
        }
    }
}
