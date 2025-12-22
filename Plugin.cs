using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilksongManager
{
    /// <summary>
    /// Main plugin entry point for Silksong Manager mod.
    /// Author: Catalyst (catalyst@kyokai.ru, Telegram: @Catalyst_Kyokai)
    /// </summary>
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>
        /// Static logger instance for the mod.
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>
        /// Configuration instance.
        /// </summary>
        public static PluginConfig ModConfig { get; private set; }

        /// <summary>
        /// Gets the current PlayerData instance.
        /// </summary>
        public static PlayerData PD => PlayerData.instance;

        /// <summary>
        /// Gets the current HeroController instance.
        /// </summary>
        public static HeroController Hero => HeroController.instance;

        /// <summary>
        /// Gets the current GameManager instance.
        /// </summary>
        public static GameManager GM => GameManager.instance;

        /// <summary>
        /// Gets the current UIManager instance.
        /// </summary>
        public static UIManager UI => UIManager.instance;

        /// <summary>
        /// Debug menu controller.
        /// </summary>
        private DebugMenu.DebugMenuController _debugMenu;

        /// <summary>
        /// Track if we already initialized menu hook
        /// </summary>
        private bool _menuHookInitialized = false;

        /// <summary>
        /// Track if enemies are currently frozen for toggle
        /// </summary>
        private bool _enemiesFrozen = false;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Silksong Manager v{PluginInfo.VERSION} loading...");

            // Initialize configuration
            ModConfig = new PluginConfig(base.Config);

            // Initialize keybind manager with config file
            Menu.Keybinds.ModKeybindManager.Initialize(Config);

            // Initialize cheat systems
            Player.CheatSystem.Initialize(Config);
            Damage.DamageSystem.Initialize(Config);

            // Apply Harmony patches for custom damage
            Patches.DamagePatches.Apply();

            // Apply Harmony patches for menu system
            Menu.Core.UIManagerPatches.Apply();

            // Initialize debug menu
            _debugMenu = gameObject.AddComponent<DebugMenu.DebugMenuController>();

            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo("Silksong Manager initialized successfully!");
        }

        private void Update()
        {
            // Toggle debug menu
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleDebugMenu))
            {
                _debugMenu?.ToggleMenu();
            }

            // Quick actions hotkeys
            HandleHotkeys();
        }

        private void LateUpdate()
        {
            // Process cheat systems in LateUpdate so we run AFTER game's Update
            // This ensures our onGround override happens after HeroController sets it
            Player.CheatSystem.Update();
        }


        private void HandleHotkeys()
        {
            if (!ModConfig.EnableHotkeys) return;

            // Toggle Noclip - use CheatSystem
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleNoclip))
            {
                Player.CheatSystem.ToggleNoclip();
            }

            // Toggle Invincibility
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInvincibility))
            {
                Player.PlayerActions.ToggleInvincibility();
            }

            // Toggle Infinite Jumps
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteJumps))
            {
                Player.CheatSystem.ToggleInfiniteJumps();
            }

            // Toggle Infinite Health
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteHealth))
            {
                Player.CheatSystem.ToggleInfiniteHealth();
            }

            // Toggle Infinite Silk
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ToggleInfiniteSilk))
            {
                Player.CheatSystem.ToggleInfiniteSilk();
            }

            // Save Position
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.SavePosition))
            {
                World.WorldActions.SavePosition();
            }

            // Load Position
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.LoadPosition))
            {
                World.WorldActions.LoadPosition();
            }

            // Kill All Enemies
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.KillAllEnemies))
            {
                Enemies.EnemyActions.KillAllEnemies();
            }

            // Freeze Enemies (toggle)
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.FreezeEnemies))
            {
                // Simple toggle - freeze if not frozen, unfreeze if frozen
                _enemiesFrozen = !_enemiesFrozen;
                if (_enemiesFrozen)
                    Enemies.EnemyActions.FreezeAllEnemies();
                else
                    Enemies.EnemyActions.UnfreezeAllEnemies();
            }

            // Add Geo
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.AddGeo))
            {
                Currency.CurrencyActions.AddGeo(1000);
            }

            // Add Shell Shards
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.AddShellShards))
            {
                Currency.CurrencyActions.AddShards(5);
            }

            // Max Silk
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.MaxSilk))
            {
                Player.PlayerActions.QuickSilk();
            }

            // Heal to Full
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.HealToFull))
            {
                Player.PlayerActions.QuickHeal();
            }

            // Game Speed controls
            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.IncreaseGameSpeed))
            {
                var current = Time.timeScale;
                World.WorldActions.SetGameSpeed(current + 0.25f);
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.DecreaseGameSpeed))
            {
                var current = Time.timeScale;
                World.WorldActions.SetGameSpeed(Mathf.Max(0.1f, current - 0.25f));
            }

            if (Menu.Keybinds.ModKeybindManager.WasActionPressed(Menu.Keybinds.ModAction.ResetGameSpeed))
            {
                World.WorldActions.SetGameSpeed(1f);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"Scene loaded: {scene.name}");

            // Check if this is the menu scene by name
            if (scene.name == "Menu_Title")
            {
                // Start waiting for MainMenuOptions to appear
                StartCoroutine(WaitForMainMenuAndInitialize());
            }
            else
            {
                // Reset menu hook when leaving menu scene
                _menuHookInitialized = false;
                Menu.MainMenuHook.Reset();
            }
        }

        private System.Collections.IEnumerator WaitForMainMenuAndInitialize()
        {
            float timeout = 10f;
            float elapsed = 0f;

            Log.LogInfo("Waiting for MainMenuOptions to appear...");

            // Wait until MainMenuOptions is found or timeout
            while (elapsed < timeout)
            {
                var mainMenuOptions = Object.FindObjectOfType<MainMenuOptions>();
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

        private void OnDestroy()
        {
            // Unsubscribe from events
            SceneManager.sceneLoaded -= OnSceneLoaded;

            Log.LogInfo("Silksong Manager unloaded.");
        }
    }
}

