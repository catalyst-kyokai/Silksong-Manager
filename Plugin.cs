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

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Silksong Manager v{PluginInfo.VERSION} loading...");

            // Initialize configuration
            ModConfig = new PluginConfig(base.Config);

            // Initialize debug menu
            _debugMenu = gameObject.AddComponent<DebugMenu.DebugMenuController>();

            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo("Silksong Manager initialized successfully!");
        }

        private void Update()
        {
            // Toggle debug menu with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _debugMenu?.ToggleMenu();
            }

            // Quick actions hotkeys
            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            if (!ModConfig.EnableHotkeys) return;

            // F2 - Quick Heal
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Player.PlayerActions.QuickHeal();
            }

            // F3 - Refill Silk
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Player.PlayerActions.QuickSilk();
            }

            // F4 - Toggle Invincibility
            if (Input.GetKeyDown(KeyCode.F4))
            {
                Player.PlayerActions.ToggleInvincibility();
            }

            // F5 - Add 1000 Geo
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Currency.CurrencyActions.AddGeo(1000);
            }

            // F6 - Toggle Infinite Jumps
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Player.PlayerActions.ToggleInfiniteJumps();
            }

            // F7 - Toggle Noclip
            if (Input.GetKeyDown(KeyCode.F7))
            {
                Player.PlayerActions.ToggleNoclip();
            }

            // F9 - Save Position
            if (Input.GetKeyDown(KeyCode.F9))
            {
                World.WorldActions.SavePosition();
            }

            // F10 - Load Position
            if (Input.GetKeyDown(KeyCode.F10))
            {
                World.WorldActions.LoadPosition();
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

