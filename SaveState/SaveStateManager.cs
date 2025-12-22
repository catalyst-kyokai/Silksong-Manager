using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using BepInEx;

namespace SilksongManager.SaveState
{
    public static class SaveStateManager
    {
        private static List<SaveStateData> _saveStates = new List<SaveStateData>();
        private static string _saveFilePath;
        private static SaveStateData _pendingLoadState;

        // Events
        public static event Action OnStatesChanged;

        public static void Initialize()
        {
            _saveFilePath = Path.Combine(Paths.ConfigPath, "SilksongManager_SaveStates.json");
            LoadStatesFromDisk();

            // Hook into scene load for cross-scene state loading
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static List<SaveStateData> GetStates()
        {
            return _saveStates;
        }

        public static void CaptureState(string name = null)
        {
            if (Plugin.PD == null || Plugin.Hero == null)
            {
                Plugin.Log.LogError("Cannot save state: PlayerData or Hero is null");
                return;
            }

            try
            {
                var state = new SaveStateData();
                state.SaveName = name;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                state.SceneName = SceneManager.GetActiveScene().name;

                // Capture PlayerData using Newtonsoft.Json
                // We utilize the fact that PlayerData fields are public/serializable
                state.PlayerDataJson = JsonConvert.SerializeObject(Plugin.PD);

                // Capture Hero State
                var hero = Plugin.Hero;
                state.Position = hero.transform.position;
                state.Velocity = hero.GetComponent<Rigidbody2D>().linearVelocity;
                state.FacingRight = hero.cState.facingRight;

                // Redundant info for UI
                state.Health = Plugin.PD.health;
                state.MaxHealth = Plugin.PD.maxHealth;
                state.Silk = Plugin.PD.silk;
                state.MaxSilk = Plugin.PD.silkMax;
                state.Geo = Plugin.PD.geo;

                _saveStates.Add(state);
                SaveStatesToDisk();
                OnStatesChanged?.Invoke();

                Plugin.Log.LogInfo($"Captured save state: {state.GetDisplayName()}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to capture state: {e.Message}");
            }
        }

        public static void LoadState(SaveStateData state)
        {
            if (state == null) return;

            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == state.SceneName)
            {
                // Instant load
                ApplyStateImmediate(state);
            }
            else
            {
                // Cross-scene load
                _pendingLoadState = state;
                Plugin.Log.LogInfo($"Loading cross-scene state: {state.SceneName}");

                // Use WorldActions to load scene safely
                // Or just SceneManager if we want raw speed, but Game manager usually handles transitions
                // Let's use GameManager to be safe with UI
                Plugin.GM.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = state.SceneName,
                    EntryGateName = "door1", // Dummy gate, we will teleport anyway
                    HeroLeaveDirection = GlobalEnums.GatePosition.unknown,
                    EntryDelay = 0f,
                    WaitForSceneTransitionCameraFade = false,
                    Visualization = GameManager.SceneLoadVisualizations.Default,
                    PreventCameraFadeOut = true
                });
            }
        }

        public static void DeleteState(SaveStateData state)
        {
            if (_saveStates.Remove(state))
            {
                SaveStatesToDisk();
                OnStatesChanged?.Invoke();
            }
        }

        private static void ApplyStateImmediate(SaveStateData state)
        {
            try
            {
                if (Plugin.PD == null || Plugin.Hero == null) return;

                // 1. Restore PlayerData
                JsonConvert.PopulateObject(state.PlayerDataJson, Plugin.PD);

                // 2. Refresh UI to reflect new stats
                // Plugin.UI.RefreshAll(); 

                // 3. Restore Hero State
                var hero = Plugin.Hero;

                // Teleport
                hero.transform.position = state.Position;
                hero.GetComponent<Rigidbody2D>().linearVelocity = state.Velocity;

                // Facing
                if (state.FacingRight)
                    hero.FaceRight();
                else
                    hero.FaceLeft();

                // Reset some hero logic components
                hero.StopAnimationControl();
                hero.AffectedByGravity(true);

                // Flash effect to indicate load
                // (Optional)

                Plugin.Log.LogInfo("State applied successfully!");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to apply state: {e.Message}");
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_pendingLoadState != null && scene.name == _pendingLoadState.SceneName)
            {
                // Wait one frame to let things settle? 
                // Using Coroutine in Plugin would be safer, but let's try direct call or use Plugin.Instance to start coroutine
                Plugin.Instance.StartCoroutine(ApplyStateAfterFrame(_pendingLoadState));
                _pendingLoadState = null;
            }
        }

        private static System.Collections.IEnumerator ApplyStateAfterFrame(SaveStateData state)
        {
            yield return new WaitForEndOfFrame(); // Wait for Start() of other objects
            ApplyStateImmediate(state);
        }

        private static void SaveStatesToDisk()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_saveStates, Formatting.Indented);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to save states to disk: {e.Message}");
            }
        }

        private static void LoadStatesFromDisk()
        {
            if (!File.Exists(_saveFilePath)) return;

            try
            {
                string json = File.ReadAllText(_saveFilePath);
                var loaded = JsonConvert.DeserializeObject<List<SaveStateData>>(json);
                if (loaded != null)
                {
                    _saveStates = loaded;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to load states from disk: {e.Message}");
            }
        }
    }
}
