using System;
using UnityEngine.InputSystem;

namespace Halloween2026
{
    public static class ControlBindings
    {
        public sealed class Slot
        {
            public string Name { get; }
            public string Action { get; }
            public string DefaultPath { get; }

            public Slot(string name, string action, string defaultPath)
            {
                Name = name;
                Action = action;
                DefaultPath = defaultPath;
            }
        }

        public static readonly Slot[] KeyboardMouseSlots =
        {
            new Slot("forward", "Player/Move", "<Keyboard>/w"),
            new Slot("backward", "Player/Move", "<Keyboard>/s"),
            new Slot("left", "Player/Move", "<Keyboard>/a"),
            new Slot("right", "Player/Move", "<Keyboard>/d"),
            new Slot("primary", "Player/Attack", "<Mouse>/leftButton"),
            new Slot("secondary", "Player/Interact", "<Mouse>/rightButton")
        };
        public static readonly Slot[] Slots = KeyboardMouseSlots;
        public static readonly Slot[] GamepadSlots =
        {
            new Slot("primary", "Player/Attack", "<Gamepad>/rightTrigger"),
            new Slot("secondary", "Player/Interact", "<Gamepad>/leftTrigger")
        };

        public static int Index(InputAction action, Slot slot)
        {
            int index;

            for (index = 0; index < action.bindings.Count; index++)
                if (action.bindings[index].path == slot.DefaultPath) return index;

            throw new InvalidOperationException("Missing authored binding: " + slot.DefaultPath);
        }

        public static bool HasConflict(InputActionAsset asset, Slot changed)
        {
            InputAction action = asset.FindAction(changed.Action, true);
            string path = action.bindings[Index(action, changed)].effectivePath;
            InputAction other;

            if (string.Equals(path, "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase)) return true;

            foreach (var slot in changed.DefaultPath.StartsWith("<Gamepad>/", StringComparison.Ordinal)
                         ? GamepadSlots : KeyboardMouseSlots)
            {
                if (slot == changed) continue;

                other = asset.FindAction(slot.Action, true);
                if (string.Equals(path, other.bindings[Index(other, slot)].effectivePath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsReservedGamepadControl(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            string value = path.ToLowerInvariant();
            return value.Contains("/start") || value.Contains("/select")
                || value.Contains("/buttonsouth") || value.Contains("/buttoneast")
                || value.Contains("/dpad") || value.Contains("/leftstick")
                || value.Contains("/rightstick");
        }

        public static bool IsPlayStation(Gamepad pad)
        {
            if (pad == null) return false;
            string product = pad.description.product ?? "";
            string manufacturer = pad.description.manufacturer ?? "";
            return product.IndexOf("Dual", StringComparison.OrdinalIgnoreCase) >= 0
                || manufacturer.IndexOf("Sony", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsXbox(Gamepad pad)
        {
            if (pad == null) return false;
            string product = pad.description.product ?? "";
            string manufacturer = pad.description.manufacturer ?? "";
            return product.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0
                || manufacturer.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string DisplayString(InputAction action, Slot slot, Gamepad pad)
        {
            var binding = action.bindings[Index(action, slot)];
            if (!slot.DefaultPath.StartsWith("<Gamepad>/", StringComparison.Ordinal)) return binding.ToDisplayString();
            string path = (binding.effectivePath ?? "").ToLowerInvariant();
            bool ps = IsPlayStation(pad);
            if (!ps && !IsXbox(pad)) return binding.ToDisplayString();
            if (path.EndsWith("/righttrigger")) return ps ? "R2" : "RT";
            if (path.EndsWith("/lefttrigger")) return ps ? "L2" : "LT";
            if (path.EndsWith("/rightshoulder")) return ps ? "R1" : "RB";
            if (path.EndsWith("/leftshoulder")) return ps ? "L1" : "LB";
            if (path.EndsWith("/buttonnorth")) return ps ? "△" : "Y";
            if (path.EndsWith("/buttonwest")) return ps ? "□" : "X";
            return binding.ToDisplayString();
        }
    }
}
