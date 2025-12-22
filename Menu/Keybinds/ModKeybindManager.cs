using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SilksongManager.Menu.Keybinds
{
    /// <summary>
    /// All mod actions that can be bound to keys.
    /// </summary>
    public enum ModAction
    {
        ToggleDebugMenu,
        ToggleNoclip,
        ToggleInvincibility,
        SavePosition,
        LoadPosition,
        KillAllEnemies,
        FreezeEnemies,
        AddGeo,
        AddShellShards,
        MaxSilk,
        HealToFull,
        IncreaseGameSpeed,
        DecreaseGameSpeed,
        ResetGameSpeed
    }

    /// <summary>
    /// Manages mod keybinds - storage, loading, conflict detection.
    /// </summary>
    public static class ModKeybindManager
    {
        private static Dictionary<ModAction, ConfigEntry<KeyCode>> _keybindConfigs;
        private static bool _initialized = false;

        /// <summary>
        /// Default keybinds for each mod action.
        /// </summary>
        public static readonly Dictionary<ModAction, KeyCode> DefaultKeybinds = new Dictionary<ModAction, KeyCode>
        {
            { ModAction.ToggleDebugMenu, KeyCode.Keypad5 },
            { ModAction.ToggleNoclip, KeyCode.N },
            { ModAction.ToggleInvincibility, KeyCode.I },
            { ModAction.SavePosition, KeyCode.F5 },
            { ModAction.LoadPosition, KeyCode.F9 },
            { ModAction.KillAllEnemies, KeyCode.K },
            { ModAction.FreezeEnemies, KeyCode.F },
            { ModAction.AddGeo, KeyCode.G },
            { ModAction.AddShellShards, KeyCode.H },
            { ModAction.MaxSilk, KeyCode.None },
            { ModAction.HealToFull, KeyCode.None },
            { ModAction.IncreaseGameSpeed, KeyCode.Equals },
            { ModAction.DecreaseGameSpeed, KeyCode.Minus },
            { ModAction.ResetGameSpeed, KeyCode.Alpha0 }
        };

        /// <summary>
        /// Human-readable names for mod actions.
        /// </summary>
        public static readonly Dictionary<ModAction, string> ActionNames = new Dictionary<ModAction, string>
        {
            { ModAction.ToggleDebugMenu, "Debug Menu" },
            { ModAction.ToggleNoclip, "Noclip" },
            { ModAction.ToggleInvincibility, "Invincibility" },
            { ModAction.SavePosition, "Save Position" },
            { ModAction.LoadPosition, "Load Position" },
            { ModAction.KillAllEnemies, "Kill All Enemies" },
            { ModAction.FreezeEnemies, "Freeze Enemies" },
            { ModAction.AddGeo, "Add Geo" },
            { ModAction.AddShellShards, "Add Shell Shards" },
            { ModAction.MaxSilk, "Max Silk" },
            { ModAction.HealToFull, "Heal to Full" },
            { ModAction.IncreaseGameSpeed, "Speed Up" },
            { ModAction.DecreaseGameSpeed, "Speed Down" },
            { ModAction.ResetGameSpeed, "Reset Speed" }
        };

        /// <summary>
        /// Initialize keybind manager with config file.
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            if (_initialized) return;

            _keybindConfigs = new Dictionary<ModAction, ConfigEntry<KeyCode>>();

            foreach (ModAction action in Enum.GetValues(typeof(ModAction)))
            {
                var defaultKey = DefaultKeybinds.ContainsKey(action) ? DefaultKeybinds[action] : KeyCode.None;
                var entry = config.Bind(
                    "Keybinds",
                    action.ToString(),
                    defaultKey,
                    $"Keybind for {GetActionName(action)}"
                );
                _keybindConfigs[action] = entry;
            }

            _initialized = true;
            Plugin.Log.LogInfo($"ModKeybindManager initialized with {_keybindConfigs.Count} keybinds");
        }

        /// <summary>
        /// Get the current keybind for an action.
        /// </summary>
        public static KeyCode GetKeybind(ModAction action)
        {
            if (!_initialized || !_keybindConfigs.ContainsKey(action))
                return DefaultKeybinds.ContainsKey(action) ? DefaultKeybinds[action] : KeyCode.None;

            return _keybindConfigs[action].Value;
        }

        /// <summary>
        /// Set the keybind for an action.
        /// </summary>
        public static void SetKeybind(ModAction action, KeyCode key)
        {
            if (!_initialized || !_keybindConfigs.ContainsKey(action)) return;

            _keybindConfigs[action].Value = key;
            Plugin.Log.LogInfo($"Set keybind for {action} to {key}");
        }

        /// <summary>
        /// Get human-readable name for action.
        /// </summary>
        public static string GetActionName(ModAction action)
        {
            return ActionNames.ContainsKey(action) ? ActionNames[action] : action.ToString();
        }

        /// <summary>
        /// Check if a key conflicts with other mod keybinds.
        /// </summary>
        public static bool IsModKeybindConflicting(KeyCode key, ModAction excludeAction, out ModAction conflictingAction)
        {
            conflictingAction = default;
            if (key == KeyCode.None) return false;

            foreach (ModAction action in Enum.GetValues(typeof(ModAction)))
            {
                if (action == excludeAction) continue;
                if (GetKeybind(action) == key)
                {
                    conflictingAction = action;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if key conflicts with game keybinds.
        /// </summary>
        public static bool IsGameKeybindConflicting(KeyCode key, out string gameActionName)
        {
            gameActionName = null;
            if (key == KeyCode.None) return false;

            // Check InputHandler's mapped keys
            try
            {
                var ih = GameManager.instance?.inputHandler;
                if (ih == null) return false;

                // Map Unity KeyCode to InControl Key for comparison
                var inControlKey = KeyCodeToInControlKey(key);
                if (inControlKey == InControl.Key.None) return false;

                // Check all mappable actions
                foreach (var action in ih.MappableKeyboardActions)
                {
                    var binding = ih.GetKeyBindingForAction(action);
                    if (!InputHandler.KeyOrMouseBinding.IsNone(binding))
                    {
                        if (binding.Key == inControlKey)
                        {
                            gameActionName = action.Name;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Game not ready or InputHandler not available
            }

            return false;
        }

        /// <summary>
        /// Reset all keybinds to defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            foreach (ModAction action in Enum.GetValues(typeof(ModAction)))
            {
                var defaultKey = DefaultKeybinds.ContainsKey(action) ? DefaultKeybinds[action] : KeyCode.None;
                SetKeybind(action, defaultKey);
            }
            Plugin.Log.LogInfo("All keybinds reset to defaults");
        }

        /// <summary>
        /// Check if an action's key was just pressed this frame.
        /// </summary>
        public static bool WasActionPressed(ModAction action)
        {
            var key = GetKeybind(action);
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        /// <summary>
        /// Convert Unity KeyCode to InControl Key for conflict checking.
        /// </summary>
        private static InControl.Key KeyCodeToInControlKey(KeyCode keyCode)
        {
            // Common mappings
            return keyCode switch
            {
                KeyCode.A => InControl.Key.A,
                KeyCode.B => InControl.Key.B,
                KeyCode.C => InControl.Key.C,
                KeyCode.D => InControl.Key.D,
                KeyCode.E => InControl.Key.E,
                KeyCode.F => InControl.Key.F,
                KeyCode.G => InControl.Key.G,
                KeyCode.H => InControl.Key.H,
                KeyCode.I => InControl.Key.I,
                KeyCode.J => InControl.Key.J,
                KeyCode.K => InControl.Key.K,
                KeyCode.L => InControl.Key.L,
                KeyCode.M => InControl.Key.M,
                KeyCode.N => InControl.Key.N,
                KeyCode.O => InControl.Key.O,
                KeyCode.P => InControl.Key.P,
                KeyCode.Q => InControl.Key.Q,
                KeyCode.R => InControl.Key.R,
                KeyCode.S => InControl.Key.S,
                KeyCode.T => InControl.Key.T,
                KeyCode.U => InControl.Key.U,
                KeyCode.V => InControl.Key.V,
                KeyCode.W => InControl.Key.W,
                KeyCode.X => InControl.Key.X,
                KeyCode.Y => InControl.Key.Y,
                KeyCode.Z => InControl.Key.Z,
                KeyCode.Alpha0 => InControl.Key.Key0,
                KeyCode.Alpha1 => InControl.Key.Key1,
                KeyCode.Alpha2 => InControl.Key.Key2,
                KeyCode.Alpha3 => InControl.Key.Key3,
                KeyCode.Alpha4 => InControl.Key.Key4,
                KeyCode.Alpha5 => InControl.Key.Key5,
                KeyCode.Alpha6 => InControl.Key.Key6,
                KeyCode.Alpha7 => InControl.Key.Key7,
                KeyCode.Alpha8 => InControl.Key.Key8,
                KeyCode.Alpha9 => InControl.Key.Key9,
                KeyCode.Space => InControl.Key.Space,
                KeyCode.Tab => InControl.Key.Tab,
                KeyCode.LeftShift => InControl.Key.LeftShift,
                KeyCode.RightShift => InControl.Key.RightShift,
                KeyCode.LeftControl => InControl.Key.LeftControl,
                KeyCode.RightControl => InControl.Key.RightControl,
                KeyCode.LeftAlt => InControl.Key.LeftAlt,
                KeyCode.RightAlt => InControl.Key.RightAlt,
                KeyCode.F1 => InControl.Key.F1,
                KeyCode.F2 => InControl.Key.F2,
                KeyCode.F3 => InControl.Key.F3,
                KeyCode.F4 => InControl.Key.F4,
                KeyCode.F5 => InControl.Key.F5,
                KeyCode.F6 => InControl.Key.F6,
                KeyCode.F7 => InControl.Key.F7,
                KeyCode.F8 => InControl.Key.F8,
                KeyCode.F9 => InControl.Key.F9,
                KeyCode.F10 => InControl.Key.F10,
                KeyCode.F11 => InControl.Key.F11,
                KeyCode.F12 => InControl.Key.F12,
                _ => InControl.Key.None
            };
        }
    }
}
