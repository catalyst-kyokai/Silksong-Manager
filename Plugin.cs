using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilksongManager
{
    /// <summary>
    /// Main plugin entry point for Silksong Manager mod.
    /// Provides debug utilities, cheats, and quality-of-life features for Hollow Knight: Silksong.
    /// Author: Catalyst (catalyst@kyokai.ru, Telegram: @Catalyst_Kyokai)
    /// </summary>
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        #region Static Properties

        /// <summary>
        /// Static logger instance for the mod.
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>
        /// Singleton instance of the plugin.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Configuration instance for mod settings.
        /// </summary>
        public static PluginConfig ModConfig { get; private set; }

        /// <summary>
        /// Gets the current PlayerData instance (player stats, inventory, progress).
        /// </summary>
        public static PlayerData PD => PlayerData.instance;

        /// <summary>
        /// Gets the current HeroController instance (player character controller).
        /// </summary>
        public static HeroController Hero => HeroController.instance;

        /// <summary>
        /// Gets the current GameManager instance (game state manager).
        /// </summary>
        public static GameManager GM => GameManager.instance;

        /// <summary>
        /// Gets the current UIManager instance (UI state manager).
        /// </summary>
        public static UIManager UI => UIManager.instance;

        #endregion

        #region Private Fields

        /// <summary>
        /// Debug menu controller instance.
        /// </summary>
        private DebugMenu.DebugMenuController _debugMenu;

        /// <summary>
        /// Tracks whether the main menu hook has been initialized.
        /// </summary>
        private bool _menuHookInitialized = false;

        /// <summary>
        /// Tracks whether enemies are currently frozen (for toggle functionality).
        /// </summary>
        private bool _enemiesFrozen = false;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake callback. Initializes all mod systems.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"Silksong Manager v{PluginInfo.VERSION} loading...");

            InitializeConfiguration();
            InitializeSystems();
            InitializePatches();

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo("Silksong Manager initialized successfully!");
        }

        /// <summary>
        /// Unity Update callback. Handles hotkey input each frame.
        /// </summary>
        private void Update()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleDebugMenu))
            {
                _debugMenu?.ToggleMenu();
            }

            HandleHotkeys();
        }

        /// <summary>
        /// Unity LateUpdate callback. Processes cheat systems after game logic.
        /// </summary>
        private void LateUpdate()
        {
            Player.CheatSystem.Update();
        }

        /// <summary>
        /// Unity OnDestroy callback. Cleans up event subscriptions.
        /// </summary>
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Log.LogInfo("Silksong Manager unloaded.");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes configuration and keybind systems.
        /// </summary>
        private void InitializeConfiguration()
        {
            ModConfig = new PluginConfig(base.Config);
            Menu.Keybinds.ModKeybindManager.Initialize(Config);
        }

        /// <summary>
        /// Initializes all mod subsystems.
        /// </summary>
        private void InitializeSystems()
        {
            Player.CheatSystem.Initialize(Config);
            Damage.DamageSystem.Initialize(Config);
            _debugMenu = gameObject.AddComponent<DebugMenu.DebugMenuController>();
            gameObject.AddComponent<UI.NotificationManager>();
            Hitbox.HitboxManager.Initialize(gameObject);
            SaveState.SaveStateManager.Initialize();
            SpeedControl.SpeedControlManager.Initialize();
        }

        /// <summary>
        /// Applies Harmony patches for game modifications.
        /// </summary>
        private void InitializePatches()
        {
            Patches.DamagePatches.Apply();
        }

        #endregion

        #region Hotkey Handling

        /// <summary>
        /// Processes all configurable hotkeys when enabled.
        /// </summary>
        private void HandleHotkeys()
        {
            if (!ModConfig.EnableHotkeys) return;

            HandleMovementHotkeys();
            HandleCombatHotkeys();
            HandleResourceHotkeys();
            HandleGameSpeedHotkeys();
            HandleDebugHotkeys();
            HandleSaveStateHotkeys();
            HandleSceneHotkeys();
        }

        /// <summary>
        /// Handles movement-related hotkeys (noclip, position save/load).
        /// </summary>
        private void HandleMovementHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleNoclip))
            {
                Player.CheatSystem.ToggleNoclip();
                bool enabled = Player.CheatSystem.NoclipEnabled;
                SilksongManager.UI.NotificationManager.Show("Noclip", enabled ? "ON" : "OFF");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.SavePosition))
            {
                World.WorldActions.SavePosition();
                var pos = Hero?.transform.position ?? Vector3.zero;
                SilksongManager.UI.NotificationManager.Show("Position Saved", $"X: {pos.x:F1}, Y: {pos.y:F1}");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.LoadPosition))
            {
                World.WorldActions.LoadPosition();
                SilksongManager.UI.NotificationManager.Show("Position Loaded");
            }
        }

        /// <summary>
        /// Handles combat-related hotkeys (invincibility, infinite jumps, enemy control).
        /// </summary>
        private void HandleCombatHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInvincibility))
            {
                Player.PlayerActions.ToggleInvincibility();
                bool enabled = PD?.isInvincible ?? false;
                SilksongManager.UI.NotificationManager.Show("Invincibility", enabled ? "ON" : "OFF");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteJumps))
            {
                Player.CheatSystem.ToggleInfiniteJumps();
                bool enabled = Player.CheatSystem.InfiniteJumps;
                SilksongManager.UI.NotificationManager.Show("Infinite Jumps", enabled ? "ON" : "OFF");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteHealth))
            {
                Player.CheatSystem.ToggleInfiniteHealth();
                bool enabled = Player.CheatSystem.InfiniteHealth;
                SilksongManager.UI.NotificationManager.Show("Infinite Health", enabled ? "ON" : "OFF");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteSilk))
            {
                Player.CheatSystem.ToggleInfiniteSilk();
                bool enabled = Player.CheatSystem.InfiniteSilk;
                SilksongManager.UI.NotificationManager.Show("Infinite Silk", enabled ? "ON" : "OFF");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.KillAllEnemies))
            {
                int count = Enemies.EnemyActions.GetEnemyCount();
                Enemies.EnemyActions.KillAllEnemies();
                SilksongManager.UI.NotificationManager.Show("Kill All Enemies", $"{count} enemies killed");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.FreezeEnemies))
            {
                _enemiesFrozen = !_enemiesFrozen;
                if (_enemiesFrozen)
                    Enemies.EnemyActions.FreezeAllEnemies();
                else
                    Enemies.EnemyActions.UnfreezeAllEnemies();
                SilksongManager.UI.NotificationManager.Show("Freeze Enemies", _enemiesFrozen ? "ON" : "OFF");
            }
        }

        /// <summary>
        /// Handles resource-related hotkeys (geo, silk, health).
        /// </summary>
        private void HandleResourceHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.AddGeo))
            {
                Currency.CurrencyActions.AddGeo(1000);
                SilksongManager.UI.NotificationManager.Show("+1000 Geo", $"Total: {PD?.geo ?? 0}");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.AddShellShards))
            {
                Currency.CurrencyActions.AddShards(5);
                SilksongManager.UI.NotificationManager.Show("+5 Shell Shards");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.MaxSilk))
            {
                Player.PlayerActions.QuickSilk();
                SilksongManager.UI.NotificationManager.Show("Max Silk");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.HealToFull))
            {
                Player.PlayerActions.QuickHeal();
                SilksongManager.UI.NotificationManager.Show("Full Health");
            }
        }

        /// <summary>
        /// Handles game speed control hotkeys.
        /// </summary>
        private void HandleGameSpeedHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.IncreaseGameSpeed))
            {
                float newSpeed = SpeedControl.SpeedControlConfig.GlobalSpeed + 0.25f;
                SpeedControl.SpeedControlManager.SetGlobalSpeed(newSpeed);
                SilksongManager.UI.NotificationManager.Show("Game Speed", $"{SpeedControl.SpeedControlConfig.GlobalSpeed:F2}x");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.DecreaseGameSpeed))
            {
                float newSpeed = Mathf.Max(0.1f, SpeedControl.SpeedControlConfig.GlobalSpeed - 0.25f);
                SpeedControl.SpeedControlManager.SetGlobalSpeed(newSpeed);
                SilksongManager.UI.NotificationManager.Show("Game Speed", $"{SpeedControl.SpeedControlConfig.GlobalSpeed:F2}x");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ResetGameSpeed))
            {
                SpeedControl.SpeedControlManager.ResetAll();
                SilksongManager.UI.NotificationManager.Show("Game Speed", "1.0x (Reset)");
            }
        }

        /// <summary>
        /// Handles debug-related hotkeys (hitbox visualization).
        /// </summary>
        private void HandleDebugHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleHitboxes))
            {
                Hitbox.HitboxManager.ToggleHitboxes();
                bool enabled = Hitbox.HitboxConfig.ShowHitboxes;
                SilksongManager.UI.NotificationManager.Show("Hitboxes", enabled ? "ON" : "OFF");
            }
        }

        /// <summary>
        /// Handles save state hotkeys (quick save/load).
        /// </summary>
        private void HandleSaveStateHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.SaveState))
            {
                string stateName = SaveState.SaveStateManager.QuickSave();
                SilksongManager.UI.NotificationManager.Show("State Saved", $"\"{stateName}\"");
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.LoadLastState))
            {
                string stateName = SaveState.SaveStateManager.LoadLastState();
                if (stateName != null)
                {
                    SilksongManager.UI.NotificationManager.Show("Loading State", $"\"{stateName}\"");
                }
                else
                {
                    SilksongManager.UI.NotificationManager.Show("No States", "Save a state first");
                }
            }
        }

        /// <summary>
        /// Handles scene-related hotkeys (reload, respawn).
        /// </summary>
        private void HandleSceneHotkeys()
        {
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ReloadScene))
            {
                string sceneName = World.WorldActions.ReloadCurrentScene();
                if (sceneName != null)
                {
                    SilksongManager.UI.NotificationManager.Show("Reload Scene", sceneName);
                }
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.Respawn))
            {
                World.WorldActions.Respawn();
                SilksongManager.UI.NotificationManager.Show("Respawn");
            }
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Callback for scene load events. Initializes menu hooks when appropriate.
        /// </summary>
        /// <param name="scene">The loaded scene.</param>
        /// <param name="mode">The scene load mode.</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"Scene loaded: {scene.name}");

            if (scene.name == "Menu_Title")
            {
                StartCoroutine(WaitForMainMenuAndInitialize());
            }
            else
            {
                _menuHookInitialized = false;
                Menu.MainMenuHook.Reset();
            }
        }

        /// <summary>
        /// Coroutine that waits for MainMenuOptions to appear before initializing menu hooks.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        private System.Collections.IEnumerator WaitForMainMenuAndInitialize()
        {
            float timeout = 10f;
            float elapsed = 0f;

            Log.LogInfo("Waiting for MainMenuOptions to appear...");

            while (elapsed < timeout)
            {
                var mainMenuOptions = Object.FindAnyObjectByType<MainMenuOptions>();
                if (mainMenuOptions != null)
                {
                    Log.LogInfo($"MainMenuOptions found after {elapsed:F2}s");

                    if (!_menuHookInitialized)
                    {
                        Menu.MainMenuHook.Initialize();
                        _menuHookInitialized = true;
                    }
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            Log.LogWarning($"MainMenuOptions not found after {timeout}s timeout!");
        }

        #endregion
    }
}
