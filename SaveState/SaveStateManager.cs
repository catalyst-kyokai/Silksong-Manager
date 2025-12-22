using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Newtonsoft.Json;
using BepInEx;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;

namespace SilksongManager.SaveState
{
    public static class SaveStateManager
    {
        private static List<SaveStateData> _saveStates = new List<SaveStateData>();
        private static string _saveFilePath;
        private static SaveStateData _pendingLoadState;
        private static Harmony _harmony;

        // Events
        public static event Action OnStatesChanged;

        public static void Initialize()
        {
            _saveFilePath = Path.Combine(Paths.ConfigPath, "SilksongManager_SaveStates.json");
            LoadStatesFromDisk();

            // Initialize Harmony for FindEntryPoint patch
            _harmony = new Harmony("com.silksongmanager.savestate");
            _harmony.Patch(
                original: AccessTools.Method(typeof(GameManager), "FindEntryPoint"),
                prefix: new HarmonyMethod(typeof(SaveStateManager), nameof(FindEntryPointPatch))
            );
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

                // Capture PlayerData
                state.PlayerDataJson = JsonConvert.SerializeObject(Plugin.PD);

                // Capture SceneData
                if (SceneData.instance != null)
                {
                    state.SceneDataJson = JsonConvert.SerializeObject(SceneData.instance);
                }

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
            if (Plugin.Hero == null) return;

            // Start the loading coroutine
            Plugin.Instance.StartCoroutine(LoadStateCoro(state));
        }

        private static IEnumerator LoadStateCoro(SaveStateData state)
        {
            Plugin.Log.LogInfo($"Starting robust load for state: {state.SceneName}");
            _pendingLoadState = state;

            // 1. Force state cleanup & Pause
            Time.timeScale = 0f;

            // Clean up existing coroutines/invulnerability
            if (Plugin.Hero != null)
            {
                Plugin.Hero.StopAllCoroutines();
                Plugin.Hero.StopAllCoroutines();

                // Reflection for private hazardInvulnRoutine
                var hazardField = typeof(HeroController).GetField("hazardInvulnRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
                hazardField?.SetValue(Plugin.Hero, null);

                // Reflection for private CancelDamageRecoil
                var cancelRecoilMethod = typeof(HeroController).GetMethod("CancelDamageRecoil", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                cancelRecoilMethod?.Invoke(Plugin.Hero, null);
                // Reflection for InvulnerablePulse
                var invulnPulseComp = Plugin.Hero.GetComponent("InvulnerablePulse");
                if (invulnPulseComp != null)
                {
                    var stopMethod = invulnPulseComp.GetType().GetMethod("StopInvulnerablePulse");
                    stopMethod?.Invoke(invulnPulseComp, null);
                }
            }

            // Close UI and Dialogs
            EventRegister.SendEvent("INVENTORY CANCEL");
            DialogueBox.EndConversation();
            DialogueBox.HideInstant();
            DialogueYesNoBox.ForceClose();
            QuestYesNoBox.ForceClose();

            // Detach from surfaces
            SlideSurface[] surfaces = UnityEngine.Object.FindObjectsOfType<SlideSurface>();
            foreach (var surface in surfaces)
            {
                // Reflection for private isHeroAttached
                var isAttachedField = typeof(SlideSurface).GetField("isHeroAttached", BindingFlags.Instance | BindingFlags.NonPublic);
                bool isAttached = (bool)(isAttachedField?.GetValue(surface) ?? false);

                if (isAttached)
                {
                    // Reflection for private Detach
                    var detachMethod = typeof(SlideSurface).GetMethod("Detach", BindingFlags.Instance | BindingFlags.NonPublic);
                    detachMethod?.Invoke(surface, new object[] { false });
                }
            }

            // 2. Load Dummy Scene ("Demo Start")
            string dummySceneName = "Demo Start";
            GameManager.instance.entryGateName = "dreamGate";
            GameManager.instance.startedOnThisScene = true;

            // Use Addressables for async loading
            var loadOp = Addressables.LoadSceneAsync("Scenes/" + dummySceneName, LoadSceneMode.Single);
            yield return loadOp;
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == dummySceneName);

            // 3. Restore Data while in dummy scene
            if (Plugin.PD != null && !string.IsNullOrEmpty(state.PlayerDataJson))
            {
                JsonConvert.PopulateObject(state.PlayerDataJson, Plugin.PD);
            }
            if (SceneData.instance != null && !string.IsNullOrEmpty(state.SceneDataJson))
            {
                JsonConvert.PopulateObject(state.SceneDataJson, SceneData.instance);
            }

            // Reset transitions and semi-persistent items
            GameManager.instance.ResetSemiPersistentItems();
            StaticVariableList.ClearSceneTransitions();

            // 4. Load Destination Scene
            Plugin.GM.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = state.SceneName,
                EntryGateName = "dreamGate", // Hijacked by our patch
                HeroLeaveDirection = GatePosition.unknown,
                EntryDelay = 0f,
                WaitForSceneTransitionCameraFade = false,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                PreventCameraFadeOut = false, // Allow normal fade behavior
                AlwaysUnloadUnusedAssets = true
            });

            // Wait until destination scene is active
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == state.SceneName);
            yield return new WaitUntil(() => !Plugin.GM.IsInSceneTransition);

            // Camera Logic
            GameManager.instance.cameraCtrl.PositionToHero(false);
            // GameManager.instance.cameraCtrl.isGameplayScene = true; // Private, handled by Reflection below
            // GameManager.instance.UpdateUIStateFromGameState(); // Method does not exist in this version

            // Fade In
            GameManager.instance.FadeSceneIn();

            // 5. Post-Load Fixes
            Plugin.Hero.CharmUpdate();
            QuestManager.IncrementVersion();
            CollectableItemManager.IncrementVersion();
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
            PlayMakerFSM.BroadcastEvent("TOOL EQUIPS CHANGED");
            PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");

            // Force Camera Gameplay Scene (Reflection)
            var camField = typeof(CameraController).GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
            if (camField != null) camField.SetValue(GameManager.instance.cameraCtrl, true);

            yield return null;

            // Apply Hero State
            ApplyStateImmediate(state);

            // Handle Unpause if game was paused
            if (GameManager.instance.isPaused)
            {
                GameManager.instance.FadeSceneIn();
                GameManager.instance.isPaused = false;
                GameCameras.instance.ResumeCameraShake();
                Plugin.Hero.UnPause();
                // Reflection for MenuButtonList.ClearAllLastSelected()
                var menuButtonListType = Assembly.GetAssembly(typeof(GameManager)).GetType("MenuButtonList");
                if (menuButtonListType != null)
                {
                    var clearMethod = menuButtonListType.GetMethod("ClearAllLastSelected", BindingFlags.Public | BindingFlags.Static);
                    clearMethod?.Invoke(null, null);
                }

                // Reflection for TimeManager.TimeScale = 1f
                var timeManagerType = Assembly.GetAssembly(typeof(GameManager)).GetType("TimeManager");
                if (timeManagerType != null)
                {
                    var timeScaleProp = timeManagerType.GetProperty("TimeScale", BindingFlags.Public | BindingFlags.Static);
                    timeScaleProp?.SetValue(null, 1f);
                }

                // Fallback direct Time.timeScale
                Time.timeScale = 1f;
            }

            // Final Physics Tap
            yield return new WaitForFixedUpdate();
            Plugin.Hero.transform.position = state.Position;

            // Time Scale back
            Time.timeScale = 1f;
            _pendingLoadState = null;

            Plugin.Log.LogInfo("Load complete!");
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
                if (Plugin.Hero == null) return;
                var hero = Plugin.Hero;

                hero.transform.position = state.Position;
                hero.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                hero.GetComponent<Rigidbody2D>().isKinematic = false;
                // Gravity
                hero.AffectedByGravity(true);
                hero.GetComponent<Rigidbody2D>().gravityScale = 0.79f; // Default gravity

                // Face direction
                if (state.FacingRight) hero.FaceRight();
                else hero.FaceLeft();

                // Explicitly invoke FinishedEnteringScene to clear flags
                // Use Reflection as it is private in some versions
                var method = typeof(HeroController).GetMethod("FinishedEnteringScene", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(hero, new object[] { true, false });
                }
                hero.GetComponent<MeshRenderer>().enabled = true;

                // Animation Force
                hero.StartAnimationControl();
                var anim = hero.GetComponent<HeroAnimationController>();
                // Force Idle if grounded
                if (state.IsGrounded)
                {
                    hero.cState.onGround = true;
                    anim.PlayClip("Idle");

                    var proxyFSMField = typeof(HeroController).GetField("proxyFSM", BindingFlags.Instance | BindingFlags.NonPublic);
                    var proxyFSM = proxyFSMField?.GetValue(hero) as PlayMakerFSM;
                    proxyFSM?.SendEvent("HeroCtrl-Idle");
                }
                else
                {
                    hero.cState.onGround = false;
                    anim.PlayClip("Fall");
                }

                hero.transitionState = HeroTransitionState.WAITING_TO_TRANSITION;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to apply state: {e.Message}");
            }
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

        public static bool FindEntryPointPatch(GameManager __instance, ref Vector2? __result, string entryPointName)
        {
            if (_pendingLoadState != null && entryPointName == "dreamGate")
            {
                __result = _pendingLoadState.Position;
                return false;
            }
            return true;
        }
    }
}
