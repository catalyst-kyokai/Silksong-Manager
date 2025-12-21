using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.World
{
    /// <summary>
    /// Actions related to world/scene management.
    /// </summary>
    public static class WorldActions
    {
        private static Vector3 _savedPosition = Vector3.zero;
        private static string _savedScene = "";
        private static List<string> _visitedScenes = new List<string>();

        /// <summary>
        /// Save current position.
        /// </summary>
        public static void SavePosition()
        {
            var hero = Plugin.Hero;
            if (hero == null) return;

            _savedPosition = hero.transform.position;
            _savedScene = Plugin.GM?.sceneName ?? "";
            Plugin.Log.LogInfo($"Saved position: {_savedPosition} in scene {_savedScene}");
        }

        /// <summary>
        /// Load saved position.
        /// </summary>
        public static void LoadPosition()
        {
            if (_savedPosition == Vector3.zero)
            {
                Plugin.Log.LogWarning("No position saved.");
                return;
            }

            var currentScene = Plugin.GM?.sceneName ?? "";
            if (currentScene != _savedScene)
            {
                Plugin.Log.LogWarning($"Cannot teleport: different scene. Saved: {_savedScene}, Current: {currentScene}");
                return;
            }

            Player.PlayerActions.TeleportTo(_savedPosition);
        }

        /// <summary>
        /// Get current scene name.
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return Plugin.GM?.sceneName ?? "Unknown";
        }

        /// <summary>
        /// Transition to another scene.
        /// </summary>
        public static void TransitionToScene(string sceneName, string gateName = "")
        {
            var gm = Plugin.GM;
            if (gm == null)
            {
                Plugin.Log.LogWarning("Cannot transition: GameManager not available.");
                return;
            }

            var loadInfo = new GameManager.SceneLoadInfo
            {
                SceneName = sceneName,
                EntryGateName = gateName,
                PreventCameraFadeOut = false,
                WaitForSceneTransitionCameraFade = true,
                EntryDelay = 0f,
                Visualization = GameManager.SceneLoadVisualizations.Default
            };

            gm.BeginSceneTransition(loadInfo);
            Plugin.Log.LogInfo($"Transitioning to scene: {sceneName}");
        }

        /// <summary>
        /// Reload current scene.
        /// </summary>
        public static void ReloadCurrentScene()
        {
            var sceneName = GetCurrentSceneName();
            if (sceneName == "Unknown") return;

            TransitionToScene(sceneName);
        }

        /// <summary>
        /// Get world info.
        /// </summary>
        public static WorldInfo GetWorldInfo()
        {
            var gm = Plugin.GM;
            if (gm == null)
            {
                return new WorldInfo();
            }

            return new WorldInfo
            {
                CurrentScene = gm.sceneName,
                EntryGate = gm.GetEntryGateName(),
                IsGamePaused = gm.IsGamePaused()
            };
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public static void PauseGame()
        {
            Time.timeScale = 0f;
            Plugin.Log.LogInfo("Game paused.");
        }

        /// <summary>
        /// Resume the game.
        /// </summary>
        public static void ResumeGame()
        {
            Time.timeScale = 1f;
            Plugin.Log.LogInfo("Game resumed.");
        }

        /// <summary>
        /// Set game speed.
        /// </summary>
        public static void SetGameSpeed(float speed)
        {
            Time.timeScale = Mathf.Clamp(speed, 0f, 10f);
            Plugin.Log.LogInfo($"Game speed set to {Time.timeScale}x");
        }
    }

    /// <summary>
    /// World/scene information.
    /// </summary>
    public struct WorldInfo
    {
        public string CurrentScene;
        public string EntryGate;
        public bool IsGamePaused;
    }
}
