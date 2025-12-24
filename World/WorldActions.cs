using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.World
{
    /// <summary>
    /// Actions related to world and scene management.
    /// Provides methods for position saving, scene transitions, and game speed control.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class WorldActions
    {
        #region State Fields

        /// <summary>Saved position for teleportation.</summary>
        private static Vector3 _savedPosition = Vector3.zero;
        /// <summary>Scene name where position was saved.</summary>
        private static string _savedScene = "";
        /// <summary>List of visited scenes.</summary>
        private static List<string> _visitedScenes = new List<string>();

        #endregion

        #region Position Management

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

        #endregion

        #region Scene Transition

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
        /// <returns>The scene name that was reloaded.</returns>
        public static string ReloadCurrentScene()
        {
            var sceneName = GetCurrentSceneName();
            if (sceneName == "Unknown") return null;

            TransitionToScene(sceneName);
            return sceneName;
        }

        /// <summary>
        /// Respawn the player at the last respawn point.
        /// </summary>
        public static void Respawn()
        {
            var gm = Plugin.GM;
            if (gm == null)
            {
                Plugin.Log.LogWarning("Cannot respawn: GameManager not available.");
                return;
            }

            // Use HazardRespawn for a clean respawn
            if (Plugin.Hero != null)
            {
                Plugin.Hero.StartCoroutine(Plugin.Hero.HazardRespawn());
                Plugin.Log.LogInfo("Player respawned.");
            }
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

        #endregion

        #region Game Speed Control

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
            // Use SpeedControlManager to restore the correct time scale
            SpeedControl.SpeedControlManager.ApplyGlobalSpeed();
            Plugin.Log.LogInfo("Game resumed.");
        }

        /// <summary>
        /// Set game speed (deprecated - use SpeedControlManager directly).
        /// </summary>
        [System.Obsolete("Use SpeedControl.SpeedControlManager.SetGlobalSpeed instead")]
        public static void SetGameSpeed(float speed)
        {
            SpeedControl.SpeedControlManager.SetGlobalSpeed(speed);
        }

        #endregion
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
