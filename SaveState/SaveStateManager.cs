using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using BepInEx;
using GlobalEnums;
using HutongGames.PlayMaker;

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
                state.IsGrounded = hero.cState.onGround;

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

            // 1. Restore PlayerData IMMEDIATELY
            // This is critical so that when the scene reloads, the world gen (enemies, bosses, walls)
            // sees the correct flags (e.g., boss dead = false).
            if (Plugin.PD != null && !string.IsNullOrEmpty(state.PlayerDataJson))
            {
                JsonConvert.PopulateObject(state.PlayerDataJson, Plugin.PD);
            }

            // 2. Force Scene Reload
            // We always trigger a transition, even if in the same scene, to reset the world state.
            _pendingLoadState = state;
            Plugin.Log.LogInfo($"Loading state (forcing reload): {state.SceneName}");

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

                // 3. Restore Hero State
                var hero = Plugin.Hero;
                var heroType = hero.GetType();
                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

                // Force Reset State
                hero.StopAnimationControl();

                // Invoke private ResetMotion
                var resetMotion = heroType.GetMethod("ResetMotion", bindingFlags);
                if (resetMotion != null)
                {
                    // Check parameters count to handle potential overloads
                    if (resetMotion.GetParameters().Length == 0)
                        resetMotion.Invoke(hero, null);
                    else
                        resetMotion.Invoke(hero, new object[] { false });
                }

                hero.ResetLook();

                // Invoke private ResetInput
                var resetInput = heroType.GetMethod("ResetInput", bindingFlags);
                resetInput?.Invoke(hero, null);

                // Physics & Position
                hero.transform.position = state.Position;
                hero.GetComponent<Rigidbody2D>().linearVelocity = state.Velocity;

                // Facing
                if (state.FacingRight)
                    hero.FaceRight();
                else
                    hero.FaceLeft();

                // Gravity & Collisions
                hero.AffectedByGravity(true);
                hero.GetComponent<HeroBox>().HeroBoxNormal();
                hero.GetComponent<MeshRenderer>().enabled = true;

                // Animation Force
                var setState = heroType.GetMethod("SetState", bindingFlags);

                hero.StartAnimationControl();

                HeroAnimationController anim = hero.GetComponent<HeroAnimationController>();

                if (state.IsGrounded)
                {
                    hero.cState.onGround = true;
                    // Force Zero Velocity to prevent popping/sliding
                    hero.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

                    if (setState != null)
                        setState.Invoke(hero, new object[] { GlobalEnums.ActorStates.grounded });
                    anim.PlayClip("Idle");

                    // Force ProxyFSM to Idle
                    var proxyFSMField = heroType.GetField("proxyFSM", bindingFlags);
                    if (proxyFSMField != null)
                    {
                        var proxyFSM = proxyFSMField.GetValue(hero) as PlayMakerFSM;
                        if (proxyFSM != null)
                            proxyFSM.SendEvent("HeroCtrl-Idle");
                    }
                }
                else
                {
                    hero.cState.onGround = false;
                    if (setState != null)
                        setState.Invoke(hero, new object[] { GlobalEnums.ActorStates.airborne });
                    anim.PlayClip("Fall");
                }

                // Reset Transition State explicitly
                hero.transitionState = HeroTransitionState.WAITING_TO_TRANSITION;
                hero.cState.transitioning = false;

                // Invoke FinishedEnteringScene to clean up all flags
                // private void FinishedEnteringScene(bool setHazardMarker = true, bool preventRunBob = false)
                var finishedEntering = heroType.GetMethod("FinishedEnteringScene", bindingFlags);
                if (finishedEntering != null)
                {
                    Plugin.Log.LogInfo("Invoking FinishedEnteringScene...");
                    finishedEntering.Invoke(hero, new object[] { true, false });
                }

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
                Plugin.Instance.StartCoroutine(ApplyStateAfterSceneLoad(_pendingLoadState));
                _pendingLoadState = null;
            }
        }

        private static System.Collections.IEnumerator ApplyStateAfterSceneLoad(SaveStateData state)
        {
            // Wait for Hero to be instantiated and moved to gate
            yield return new WaitForSeconds(0.1f);

            // Wait until HeroController is accepting input or ready
            // A hard delay is often safer for mods than checking fragile flags
            yield return new WaitForSeconds(0.2f);

            ApplyStateImmediate(state);

            // Double tap position after physics settles
            yield return new WaitForFixedUpdate();
            Plugin.Hero.transform.position = state.Position;
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
