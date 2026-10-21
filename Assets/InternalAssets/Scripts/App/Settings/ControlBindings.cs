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

        public static readonly Slot[] Slots =
        {
            new Slot("forward", "Player/Move", "<Keyboard>/w"),
            new Slot("backward", "Player/Move", "<Keyboard>/s"),
            new Slot("left", "Player/Move", "<Keyboard>/a"),
            new Slot("right", "Player/Move", "<Keyboard>/d"),
            new Slot("primary", "Player/Attack", "<Mouse>/leftButton"),
            new Slot("secondary", "Player/Interact", "<Mouse>/rightButton")
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

            foreach (var slot in Slots)
            {
                if (slot == changed) continue;

                other = asset.FindAction(slot.Action, true);
                if (string.Equals(path, other.bindings[Index(other, slot)].effectivePath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
