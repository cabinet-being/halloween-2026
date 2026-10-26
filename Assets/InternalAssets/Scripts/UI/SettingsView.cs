using System;
using System.Collections.Generic;
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
        private bool _manualTab;
        private int _enableUiAtFrame = -1;

        public bool IsOpen => !_screen.ClassListContains("hidden");
        public bool IsRebinding => _operation != null;
        public int IgnoreBackUntilFrame { get; private set; }
        public ControlDeviceKind SelectedDeviceTab { get; private set; }

        public SettingsView(SettingsService settings, VisualElement root, VisualTreeAsset layout, Action back)
        {
            _settings = settings;
            _back = back;
            layout.CloneTree(root);
            _screen = root.Q("settings-screen");

            BindSlider("master", v => _settings.SetMasterVolume(v / 100f));
            BindSlider("music", v => _settings.SetMusicVolume(v / 100f));
            BindSlider("sensitivity", _settings.SetSensitivity);
            BindSlider("fov", _settings.SetFieldOfView);
            BindSlider("gamepad-sensitivity", _settings.SetGamepadSensitivity);
            BindSlider("move-deadzone", _settings.SetMoveDeadzone);
            BindSlider("look-deadzone", _settings.SetLookDeadzone);
            _screen.Q<Toggle>("gamepad-invert-y").RegisterValueChangedCallback(e => _settings.SetInvertGamepadY(e.newValue));
            _screen.Q<Button>("keyboard-tab").clicked += () => SelectDeviceTab(ControlDeviceKind.KeyboardMouse);
            _screen.Q<Button>("gamepad-tab").clicked += () => SelectDeviceTab(ControlDeviceKind.Gamepad);
            _screen.Q<Button>("settings-back-button").clicked += Back;
            _screen.Q<Button>("defaults-button").clicked += RestoreDeviceDefaults;
            _screen.Q<Button>("common-defaults-button").clicked += RestoreCommonDefaults;

            foreach (var slot in ControlBindings.KeyboardMouseSlots)
                _screen.Q<Button>("bind-" + slot.Name).clicked += () => BeginBinding(slot);
            foreach (var slot in ControlBindings.GamepadSlots)
                _screen.Q<Button>("bind-gamepad-" + slot.Name).clicked += () => BeginBinding(slot);

            _screen.RegisterCallback<FocusInEvent>(e =>
            {
                if (e.target is VisualElement element) ScrollFocusedIntoView(element);
            });
            _screen.RegisterCallback<NavigationMoveEvent>(NavigateVertically, TrickleDown.TrickleDown);
            _settings.Changed += Refresh;
            _settings.ActiveDeviceChanged += OnActiveDeviceChanged;
            SelectedDeviceTab = _settings.ActiveDevice;
            Refresh();
        }

        private void BindSlider(string name, Action<float> set)
        {
            _screen.Q<Slider>(name + "-slider").RegisterValueChangedCallback(e => set(e.newValue));
        }

        private void NavigateVertically(NavigationMoveEvent evt)
        {
            if (!IsOpen || IsRebinding || (evt.direction != NavigationMoveEvent.Direction.Up
                && evt.direction != NavigationMoveEvent.Direction.Down)) return;

            var focused = _screen.panel?.focusController?.focusedElement as VisualElement;
            if (focused == null) return;
            var items = new List<VisualElement>();
            AddNavigationItem(items, "master-slider");
            AddNavigationItem(items, "music-slider");
            AddNavigationItem(items, "fov-slider");
            AddNavigationItem(items, "keyboard-tab");
            AddNavigationItem(items, "gamepad-tab");
            if (SelectedDeviceTab == ControlDeviceKind.Gamepad)
            {
                AddNavigationItem(items, "gamepad-sensitivity-slider");
                AddNavigationItem(items, "gamepad-invert-y");
                AddNavigationItem(items, "move-deadzone-slider");
                AddNavigationItem(items, "look-deadzone-slider");
                foreach (var slot in ControlBindings.GamepadSlots)
                    AddNavigationItem(items, "bind-gamepad-" + slot.Name);
            }
            else
            {
                AddNavigationItem(items, "sensitivity-slider");
                foreach (var slot in ControlBindings.KeyboardMouseSlots)
                    AddNavigationItem(items, "bind-" + slot.Name);
            }
            AddNavigationItem(items, "defaults-button");
            AddNavigationItem(items, "common-defaults-button");
            AddNavigationItem(items, "settings-back-button");

            int current = -1;
            for (int i = 0; i < items.Count; i++)
                if (items[i] == focused || items[i].Contains(focused)) { current = i; break; }
            if (current < 0) return;

            int nextIndex = Mathf.Clamp(current + (evt.direction == NavigationMoveEvent.Direction.Down ? 1 : -1), 0, items.Count - 1);
            evt.StopPropagation();
            _screen.panel.focusController.IgnoreEvent(evt);
            var next = items[nextIndex];
            next.Focus();
            ScrollFocusedIntoView(next);
        }

        private void ScrollFocusedIntoView(VisualElement element)
        {
            var scroll = _screen.Q<ScrollView>("settings-scroll");
            if (!scroll.Contains(element)) return;

            var target = element;
            for (var parent = element.parent; parent != null && parent != scroll; parent = parent.parent)
                if (parent.ClassListContains("setting-row") || parent.ClassListContains("binding-row"))
                {
                    target = parent;
                    break;
                }

            ScrollToFocus(scroll, target, element);
            scroll.schedule.Execute(() =>
            {
                var focused = _screen.panel?.focusController?.focusedElement as VisualElement;
                if (focused == element || (focused != null && element.Contains(focused)))
                    ScrollToFocus(scroll, target, element);
            });
        }

        private static void ScrollToFocus(ScrollView scroll, VisualElement target, VisualElement focused)
        {
            scroll.ScrollTo(target);
            const float focusMargin = 4f;
            var viewport = scroll.contentViewport.worldBound;
            var bounds = focused.worldBound;
            float offset = 0f;
            if (bounds.yMax > viewport.yMax - focusMargin) offset = bounds.yMax - viewport.yMax + focusMargin;
            else if (bounds.yMin < viewport.yMin + focusMargin) offset = bounds.yMin - viewport.yMin - focusMargin;
            if (Mathf.Abs(offset) > 0.01f)
                scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, scroll.scrollOffset.y + offset);
        }

        private void AddNavigationItem(List<VisualElement> items, string name)
        {
            var element = _screen.Q(name);
            if (element == null || !element.focusable || !element.enabledInHierarchy) return;
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.resolvedStyle.display == DisplayStyle.None) return;
            items.Add(element);
        }

        public void Show()
        {
            _manualTab = false;
            SelectDeviceTabInternal(_settings.ActiveDevice);
            _screen.RemoveFromClassList("hidden");
            _screen.Q<Button>("settings-back-button").Focus();
        }

        public void Hide()
        {
            CancelBinding();
            _screen.AddToClassList("hidden");
            _manualTab = false;
        }

        public void Back()
        {
            if (IsRebinding) { CancelBinding(); return; }
            if (Time.frameCount <= IgnoreBackUntilFrame) return;
            Hide();
            _back();
        }

        public void Update()
        {
            if (_enableUiAtFrame >= 0 && Time.frameCount >= _enableUiAtFrame)
            {
                _enableUiAtFrame = -1;
                SetInteractiveEnabled(true);
                BindingButton(_slot).Focus();
            }
            if (IsRebinding && ((Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
                CancelBinding();
        }

        public void SelectDeviceTab(ControlDeviceKind kind)
        {
            if (IsRebinding) return;
            _manualTab = true;
            SelectDeviceTabInternal(kind);
        }

        private void SelectDeviceTabInternal(ControlDeviceKind kind)
        {
            var focused = _screen.panel?.focusController?.focusedElement as VisualElement;
            var oldPanel = _screen.Q(SelectedDeviceTab == ControlDeviceKind.Gamepad ? "gamepad-panel" : "keyboard-panel");
            bool focusWasHidden = focused != null && oldPanel.Contains(focused);
            SelectedDeviceTab = kind;
            _screen.Q("keyboard-panel").EnableInClassList("hidden", kind != ControlDeviceKind.KeyboardMouse);
            _screen.Q("gamepad-panel").EnableInClassList("hidden", kind != ControlDeviceKind.Gamepad);
            _screen.Q<Button>("keyboard-tab").EnableInClassList("selected-tab", kind == ControlDeviceKind.KeyboardMouse);
            _screen.Q<Button>("gamepad-tab").EnableInClassList("selected-tab", kind == ControlDeviceKind.Gamepad);
            _screen.Q<ScrollView>("settings-scroll").scrollOffset = Vector2.zero;
            if (focusWasHidden)
                _screen.Q<Button>(kind == ControlDeviceKind.Gamepad ? "gamepad-tab" : "keyboard-tab").Focus();
        }

        private void OnActiveDeviceChanged(ControlDeviceKind kind)
        {
            if (IsOpen && !_manualTab && !IsRebinding) SelectDeviceTabInternal(kind);
            RefreshPrompts();
        }

        public void BeginBinding(ControlBindings.Slot slot)
        {
            if (IsRebinding) return;
            bool gamepad = slot.DefaultPath.StartsWith("<Gamepad>/", StringComparison.Ordinal);
            _slot = slot;
            _rebindingAction = _settings.Input.FindAction(slot.Action, true);
            _bindingIndex = ControlBindings.Index(_rebindingAction, slot);
            _oldOverride = _rebindingAction.bindings[_bindingIndex].overridePath;
            _wasEnabled = _rebindingAction.enabled;
            _rebindingAction.Disable();
            _settings.SuppressDeviceIntent = true;
            SetStatus(gamepad ? "Press a gamepad button or trigger. B / Circle cancels. Menu controls are reserved."
                : "Press a keyboard key or mouse button. Escape cancels.");
            SetInteractiveEnabled(false);

            _operation = _rebindingAction.PerformInteractiveRebinding(_bindingIndex)
                .WithExpectedControlType("Button")
                .WithCancelingThrough(gamepad ? "<Gamepad>/buttonEast" : "<Keyboard>/escape")
                .WithTimeout(10f)
                .OnCancel(_ => FinishBinding(false))
                .OnComplete(_ => FinishBinding(true));

            if (gamepad)
            {
                _operation.WithControlsHavingToMatchPath("<Gamepad>/*");
                _operation.WithControlsExcluding("<Gamepad>/start");
                _operation.WithControlsExcluding("<Gamepad>/select");
                _operation.WithControlsExcluding("<Gamepad>/buttonSouth");
                _operation.WithControlsExcluding("<Gamepad>/buttonEast");
                _operation.WithControlsExcluding("<Gamepad>/dpad");
                _operation.WithControlsExcluding("<Gamepad>/leftStick");
                _operation.WithControlsExcluding("<Gamepad>/rightStick");
            }
            else
            {
                _operation.WithControlsExcluding("<Gamepad>/*");
                _operation.WithControlsExcluding("<Joystick>/*");
                _operation.WithControlsExcluding("<Touchscreen>/*");
                _operation.WithControlsExcluding("<Pointer>/position");
                _operation.WithControlsExcluding("<Pointer>/delta");
                _operation.WithControlsExcluding("<Mouse>/scroll");
                if (slot.Action == "Player/Move") _operation.WithControlsExcluding("<Mouse>/*");
            }
            _operation.Start();
        }

        private void FinishBinding(bool completed)
        {
            bool gamepad = _slot.DefaultPath.StartsWith("<Gamepad>/", StringComparison.Ordinal);
            bool reserved = completed && gamepad && ControlBindings.IsReservedGamepadControl(
                _rebindingAction.bindings[_bindingIndex].effectivePath);
            bool conflict = completed && (reserved || ControlBindings.HasConflict(_settings.Input, _slot));
            _operation.Dispose();
            _operation = null;
            IgnoreBackUntilFrame = Time.frameCount + 1;

            if (!completed || conflict)
            {
                if (_oldOverride == null) _rebindingAction.RemoveBindingOverride(_bindingIndex);
                else _rebindingAction.ApplyBindingOverride(_bindingIndex, _oldOverride);
            }
            if (_wasEnabled) _rebindingAction.Enable();

            _enableUiAtFrame = Time.frameCount + 2;
            _settings.SuppressDeviceIntent = false;

            if (completed && !conflict) _settings.SaveBindings();
            Refresh();
            SetStatus(reserved ? "That control is reserved for menus. Your previous binding was kept."
                : conflict ? "That control is already in use. Your previous binding was kept."
                : completed ? "Control saved." : "Rebinding cancelled. Your previous control is unchanged.");
        }

        private void SetInteractiveEnabled(bool enabled)
        {
            _screen.Q<ScrollView>("settings-scroll").SetEnabled(enabled);
            _screen.Q<Button>("keyboard-tab").SetEnabled(enabled);
            _screen.Q<Button>("gamepad-tab").SetEnabled(enabled);
            _screen.Q<Button>("defaults-button").SetEnabled(enabled);
            _screen.Q<Button>("common-defaults-button").SetEnabled(enabled);
            _screen.Q<Button>("settings-back-button").SetEnabled(enabled);
        }

        private Button BindingButton(ControlBindings.Slot slot) => _screen.Q<Button>(
            (slot.DefaultPath.StartsWith("<Gamepad>/", StringComparison.Ordinal) ? "bind-gamepad-" : "bind-") + slot.Name);

        private void CancelBinding() => _operation?.Cancel();

        private void RestoreDeviceDefaults()
        {
            _settings.ResetDevice(SelectedDeviceTab);
            SetStatus("This device's controls and sensitivity were restored.");
        }

        private void RestoreCommonDefaults()
        {
            _settings.ResetCommon();
            SetStatus("Sound and field of view were restored.");
        }

        private void Refresh()
        {
            RefreshSlider("master", _settings.MasterVolume * 100f, Mathf.RoundToInt(_settings.MasterVolume * 100f) + "%");
            RefreshSlider("music", _settings.MusicVolume * 100f, Mathf.RoundToInt(_settings.MusicVolume * 100f) + "%");
            RefreshSlider("sensitivity", _settings.Sensitivity, _settings.Sensitivity.ToString("F2") + "×");
            RefreshSlider("fov", _settings.FieldOfView, Mathf.RoundToInt(_settings.FieldOfView) + "°");
            RefreshSlider("gamepad-sensitivity", _settings.GamepadSensitivity, _settings.GamepadSensitivity.ToString("F2") + "×");
            RefreshSlider("move-deadzone", _settings.MoveDeadzone, _settings.MoveDeadzone.ToString("F2"));
            RefreshSlider("look-deadzone", _settings.LookDeadzone, _settings.LookDeadzone.ToString("F2"));
            _screen.Q<Toggle>("gamepad-invert-y").SetValueWithoutNotify(_settings.InvertGamepadY);
            foreach (var slot in ControlBindings.KeyboardMouseSlots) RefreshBinding(slot);
            foreach (var slot in ControlBindings.GamepadSlots) RefreshBinding(slot);
            RefreshPrompts();
        }

        private void RefreshBinding(ControlBindings.Slot slot)
        {
            var action = _settings.Input.FindAction(slot.Action, true);
            BindingButton(slot).text = ControlBindings.DisplayString(action, slot, _settings.ActiveGamepad);
        }

        private void RefreshPrompts()
        {
            bool gamepad = _settings.ActiveDevice == ControlDeviceKind.Gamepad;
            bool playStation = ControlBindings.IsPlayStation(_settings.ActiveGamepad);
            bool xbox = ControlBindings.IsXbox(_settings.ActiveGamepad);
            _screen.Q<Label>("settings-back-hint").text = gamepad
                ? (playStation ? "○ · Back" : xbox ? "B · Back" : "East button · Back") : "ESC · Back";
            _screen.Q<Label>("gamepad-help").text = playStation
                ? "✕ confirms, ○ goes back. Options pauses. Sticks, D-pad, ✕, ○, Options and Share are reserved for menus."
                : xbox ? "A confirms, B goes back. Start pauses. Sticks, D-pad, A, B, Start and Select are reserved for menus."
                : "South confirms, East goes back. Start pauses. Sticks, D-pad, South, East, Start and Select are reserved for menus.";
        }

        private void RefreshSlider(string name, float value, string label)
        {
            _screen.Q<Slider>(name + "-slider").SetValueWithoutNotify(value);
            _screen.Q<Label>(name + "-value").text = label;
        }

        private void SetStatus(string text) => _screen.Q<Label>("settings-status").text = text;

        public void Dispose()
        {
            CancelBinding();
            _settings.SuppressDeviceIntent = false;
            _settings.Changed -= Refresh;
            _settings.ActiveDeviceChanged -= OnActiveDeviceChanged;
            _screen.RemoveFromHierarchy();
        }
    }
}
