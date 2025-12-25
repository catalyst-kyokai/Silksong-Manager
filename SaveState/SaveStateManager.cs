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
    /// <summary>
    /// Manages save state functionality for capturing and restoring complete game state snapshots.
    /// Supports saving player data, enemy states, battle/boss progress, and scene data.
    /// Uses Harmony patches to intercept scene loading for proper state restoration.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SaveStateManager
    {
        /// <summary>List of all captured save states.</summary>
        private static List<SaveStateData> _saveStates = new List<SaveStateData>();
        /// <summary>Path to the JSON file storing save states.</summary>
        private static string _saveFilePath;
        /// <summary>State pending to be loaded after scene transition.</summary>
        private static SaveStateData _pendingLoadState;
        /// <summary>Harmony instance for scene loading patches.</summary>
        private static Harmony _harmony;

        /// <summary>Event fired when save states list changes (add, delete).</summary>
        public static event Action OnStatesChanged;

        /// <summary>
        /// Initializes the save state system, loads existing states from disk, and applies Harmony patches.
        /// </summary>
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

        /// <summary>
        /// Gets all captured save states.
        /// </summary>
        /// <returns>List of all save state data objects.</returns>
        public static List<SaveStateData> GetStates()
        {
            return _saveStates;
        }

        /// <summary>
        /// Gets the most recently created save state.
        /// </summary>
        /// <returns>The last save state, or null if no states exist.</returns>
        public static SaveStateData GetLastState()
        {
            if (_saveStates.Count == 0) return null;
            return _saveStates[_saveStates.Count - 1];
        }

        /// <summary>
        /// Quick save with auto-generated name.
        /// </summary>
        /// <returns>The display name of the created state.</returns>
        public static string QuickSave()
        {
            CaptureState(null);
            var lastState = GetLastState();
            return lastState?.GetDisplayName() ?? "Unknown";
        }

        /// <summary>
        /// Loads the most recently created save state.
        /// </summary>
        /// <returns>The display name of the loaded state, or null if no states exist.</returns>
        public static string LoadLastState()
        {
            var lastState = GetLastState();
            if (lastState == null)
            {
                Plugin.Log.LogWarning("No save states available to load.");
                return null;
            }
            LoadState(lastState);
            return lastState.GetDisplayName();
        }

        /// <summary>
        /// Captures the current game state into a new save state.
        /// </summary>
        /// <param name="name">Optional custom name for the save state.</param>
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

                // Force all PersistentBoolItems to update their values in SceneData BEFORE serialization
                // This is necessary because the game only persists on scene exit, not in real-time
                ForceSavePersistentItems();

                // Capture SceneData
                if (SceneData.instance != null)
                {
                    state.SceneDataJson = JsonConvert.SerializeObject(SceneData.instance);
                    Plugin.Log.LogInfo($"[DEBUG] Captured SceneData, JSON length: {state.SceneDataJson?.Length ?? 0}");
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

                // --- Capture Advanced States ---
                state.EnemyStates = CaptureEnemyStates();
                state.BattleSceneState = CaptureBattleSceneState();
                state.BossSceneState = CaptureBossSceneState();

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

        /// <summary>
        /// Loads a previously captured save state, restoring all game data.
        /// </summary>
        /// <param name="state">The save state to load.</param>
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
                Plugin.Log.LogInfo($"[DEBUG] Restoring SceneData, JSON length: {state.SceneDataJson.Length}");
                JsonConvert.PopulateObject(state.SceneDataJson, SceneData.instance);
                Plugin.Log.LogInfo($"[DEBUG] SceneData restored");
            }
            else
            {
                Plugin.Log.LogWarning($"[DEBUG] SceneData NOT restored: instance={SceneData.instance != null}, json={!string.IsNullOrEmpty(state.SceneDataJson)}");
            }

            // CRITICAL: Refresh HUD to display correct HP/Silk values
            // Send events that HUD elements listen to for updates
            EventRegister.SendEvent(EventRegisterEvents.HealthUpdate);
            EventRegister.SendEvent(EventRegisterEvents.UpdateBlueHealth);
            EventRegister.SendEvent(EventRegisterEvents.RegeneratedSilkChunk);
            EventRegister.SendEvent(EventRegisterEvents.SilkCursedUpdate);
            Plugin.Log.LogInfo($"[DEBUG] Sent HUD refresh events: Health={state.Health}, Silk={state.Silk}");

            // Reset transitions only (NOT semi-persistent items - we want to keep our restored data)
            // GameManager.instance.ResetSemiPersistentItems(); // REMOVED - this was clearing SceneData
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

            // Restore Persistent Bool Items (breakables, etc.)
            try { RestorePersistentBoolItems(); } catch (Exception e) { Plugin.Log.LogError("Error restoring persistent items: " + e); }

            // Small delay to let FSMs initialize before applying enemy states
            yield return new WaitForSecondsRealtime(0.2f);

            // Restore Enemy & Scene States
            try { RestoreEnemyStates(state.EnemyStates); } catch (Exception e) { Plugin.Log.LogError("Error restoring enemies: " + e); }
            try { RestoreBattleSceneState(state.BattleSceneState); } catch (Exception e) { Plugin.Log.LogError("Error restoring battle: " + e); }
            try { RestoreBossSceneState(state.BossSceneState); } catch (Exception e) { Plugin.Log.LogError("Error restoring boss: " + e); }

            // Trigger battle/boss events to close arena gates if needed
            if (state.BattleSceneState != null || state.BossSceneState != null)
            {
                PlayMakerFSM.BroadcastEvent("BATTLE START");
            }

            // Apply Hero State
            ApplyStateImmediate(state);

            // Handle Unpause unconditionally with defensive blocks
            Plugin.Log.LogInfo("[DEBUG] Force Unpausing Game...");

            // 1. Reset Game Manager Pause State
            try
            {
                GameManager.instance.isPaused = false;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to unpause GM: " + ex.Message); }

            try { GameManager.instance.FadeSceneIn(); }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to fade scene in: " + ex.Message); }

            try { GameCameras.instance.ResumeCameraShake(); }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to resume camera shake: " + ex.Message); }

            // 2. Unlock Input
            try
            {
                if (GameManager.instance.inputHandler != null)
                {
                    GameManager.instance.inputHandler.StartAcceptingInput();
                    GameManager.instance.inputHandler.AllowPause();
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to unlock input: " + ex.Message); }

            // 3. Close Menus
            try
            {
                var menuButtonListType = Assembly.GetAssembly(typeof(GameManager)).GetType("MenuButtonList");
                if (menuButtonListType != null)
                {
                    var clearMethod = menuButtonListType.GetMethod("ClearAllLastSelected", BindingFlags.Public | BindingFlags.Static);
                    clearMethod?.Invoke(null, null);
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to clear menus: " + ex.Message); }

            // 4. Force TimeScale = 1 (CRITICAL: Must execute)
            try
            {
                Time.timeScale = 1f;

                // Reflection for TimeManager.TimeScale = 1f
                var timeManagerType = Assembly.GetAssembly(typeof(GameManager)).GetType("TimeManager");
                if (timeManagerType != null)
                {
                    var timeScaleProp = timeManagerType.GetProperty("TimeScale", BindingFlags.Public | BindingFlags.Static);
                    timeScaleProp?.SetValue(null, 1f);
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("Failed to set TimeScale: " + ex.Message); }

            Plugin.Log.LogInfo("[DEBUG] Unpause sequence completed");

            // Final Physics Tap to ensure transforms sync
            yield return new WaitForFixedUpdate();
            Plugin.Hero.transform.position = state.Position;

            // CRITICAL: Sync physics and re-trigger enemy detection
            // This fixes the issue where enemies with Alert Range triggers don't detect
            // the hero after scene reload because Physics2D "forgets" trigger overlaps
            Physics2D.SyncTransforms();

            // Force a physics simulation step to re-trigger all overlaps
            yield return new WaitForFixedUpdate();

            // Re-enable enemy Alert Range colliders to force re-detection
            // Many enemies use "Alert Range" triggers that need to re-detect the hero
            ForceEnemyRedetection();

            // CRITICAL: Force Physics2D to re-trigger OnTriggerEnter2D events
            // Move hero FAR away and back to force Physics2D to see this as a new overlap
            Vector3 savedPos = Plugin.Hero.transform.position;
            Plugin.Hero.transform.position = new Vector3(9999f, 9999f, savedPos.z);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Plugin.Hero.transform.position = savedPos;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Plugin.Log.LogInfo("[DEBUG] Forced hero position reset for Physics2D collision re-detection");

            // CRITICAL: Re-apply saved health/silk values AFTER scene transition!
            // BeginSceneTransition resets health to maxHealth, so we must restore from saved state
            Plugin.Log.LogInfo($"[DEBUG] Before restore: health={Plugin.PD.health}, silk={Plugin.PD.silk}");
            Plugin.PD.health = state.Health;
            Plugin.PD.silk = state.Silk;
            Plugin.PD.geo = state.Geo;
            Plugin.Log.LogInfo($"[DEBUG] After restore: health={Plugin.PD.health}, silk={Plugin.PD.silk}, geo={Plugin.PD.geo}");

            // CRITICAL: Force HUD refresh for Health and Silk display
            // Wait for HUD to be ready (outside try-catch since yield can't be in try-catch)
            yield return new WaitUntil(() => GameCameras.instance?.hudCanvasSlideOut != null);
            yield return null;

            // Now do the HUD refresh (no yields allowed here)
            try
            {
                // Use exact DebugMod approach: TakeHealth(1) + AddHealth(1) triggers UI refresh
                // Net effect: health stays same (4-1+1=4) but UI gets updated
                Plugin.Log.LogInfo($"[DEBUG] Triggering health UI update via TakeHealth/AddHealth, health={Plugin.PD.health}");
                Plugin.Hero.TakeHealth(1);
                Plugin.Hero.AddHealth(1);
                Plugin.Log.LogInfo($"[DEBUG] After TakeHealth/AddHealth, health={Plugin.PD.health}");

                // Clear damage effects that might have triggered
                Plugin.Hero.ClearEffects();

                // Add blue health if any was saved
                int blueHealth = Plugin.PD.healthBlue;
                for (int i = 0; i < blueHealth; i++)
                {
                    EventRegister.SendEvent("ADD BLUE HEALTH");
                }

                Plugin.Log.LogInfo($"[DEBUG] Health UI refresh completed, blueHealth={blueHealth}");

                // Refresh Silk Spool display
                if (GameCameras.instance.silkSpool != null)
                {
                    GameCameras.instance.silkSpool.DrawSpool();
                    Plugin.Log.LogInfo($"[DEBUG] Called silkSpool.DrawSpool()");
                }

                // Send FSM events for additional UI updates
                PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
                PlayMakerFSM.BroadcastEvent("TOOL EQUIPS CHANGED");

                Plugin.Log.LogInfo("[DEBUG] HUD refresh completed");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"HUD refresh failed: {ex.Message}");
            }

            // Restoring Time Scale and clearing pending state (CRITICAL for unfreezing)
            Time.timeScale = 1f;
            _pendingLoadState = null;

            Plugin.Log.LogInfo("Load complete!");
        }

        /// <summary>
        /// Forces all enemies to re-detect the hero by toggling their alert range colliders.
        /// This fixes the issue where Physics2D trigger overlaps are "forgotten" after scene reload.
        /// </summary>
        private static void ForceEnemyRedetection()
        {
            Plugin.Log.LogInfo("[DEBUG] ForceEnemyRedetection: Starting...");

            var enemies = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int redetectedCount = 0;
            int crawlersRestarted = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                // Find all child triggers that might be used for hero detection
                var childTriggers = enemy.GetComponentsInChildren<Collider2D>(true);
                foreach (var trigger in childTriggers)
                {
                    // Look for Alert Range, Wake Range, Attack Range type triggers
                    string name = trigger.gameObject.name.ToLower();
                    if ((name.Contains("alert") || name.Contains("wake") || name.Contains("range") ||
                         name.Contains("detect") || name.Contains("sense")) && trigger.isTrigger)
                    {
                        // Toggle the collider to force Physics2D to re-check overlaps
                        bool wasEnabled = trigger.enabled;
                        trigger.enabled = false;
                        trigger.enabled = wasEnabled;
                        redetectedCount++;
                    }
                }

                // Restart Crawler components - they need their coroutine running for movement
                var crawler = enemy.GetComponent<Crawler>();
                if (crawler != null && crawler.enabled)
                {
                    try
                    {
                        // Stop and restart crawling to reinitialize the coroutine
                        crawler.StopCrawling();
                        crawler.StartCrawling();
                        crawlersRestarted++;
                        Plugin.Log.LogInfo($"[DEBUG] Restarted Crawler on {enemy.gameObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"Failed to restart Crawler on {enemy.gameObject.name}: {ex.Message}");
                    }
                }

                // Also send a FINISHED event to enemy FSMs to help them re-initialize
                var fsms = enemy.GetComponents<PlayMakerFSM>();
                foreach (var fsm in fsms)
                {
                    if (fsm.FsmName == "Control" && fsm.enabled)
                    {
                        // Try to send FINISHED event which many FSMs use to re-evaluate
                        fsm.SendEvent("FINISHED");
                    }
                }

                // Toggle DamageHero component to force re-detection of collision with hero
                // DamageHero uses OnTriggerStay2D/OnCollisionStay2D - need to re-register
                var damageHero = enemy.GetComponent<DamageHero>();
                if (damageHero != null && damageHero.enabled)
                {
                    damageHero.enabled = false;
                    damageHero.enabled = true;
                    Plugin.Log.LogInfo($"[DEBUG] Toggled DamageHero on {enemy.gameObject.name}");
                }

                // Toggle main collider to force physics re-registration
                var mainCollider = enemy.GetComponent<Collider2D>();
                if (mainCollider != null && mainCollider.enabled)
                {
                    mainCollider.enabled = false;
                    mainCollider.enabled = true;
                }
            }

            Plugin.Log.LogInfo($"[DEBUG] ForceEnemyRedetection: Toggled {redetectedCount} alert triggers, restarted {crawlersRestarted} crawlers");
        }



        /// <summary>
        /// Deletes a save state from the list and persists changes to disk.
        /// </summary>
        /// <param name="state">The save state to delete.</param>
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
                hero.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                // Gravity
                hero.AffectedByGravity(true);
                hero.GetComponent<Rigidbody2D>().gravityScale = 0.79f; // Default gravity

                // Face direction
                if (state.FacingRight) hero.FaceRight();
                else hero.FaceLeft();

                // CRITICAL: Reset transition and damage state flags
                hero.cState.transitioning = false;
                hero.cState.dead = false;
                hero.cState.hazardDeath = false;
                hero.cState.recoiling = false;
                hero.cState.shadowDashing = false;
                hero.transitionState = HeroTransitionState.WAITING_TO_TRANSITION;

                // Reset damageMode to FULL_DAMAGE (this is what FinishedEnteringScene does)
                hero.SetDamageMode(DamageMode.FULL_DAMAGE);

                // CRITICAL: Reset HeroBox.Inactive - this static flag blocks ALL damage!
                // HeroBox checks this in OnTriggerEnter2D/OnTriggerStay2D
                HeroBox.Inactive = false;
                Plugin.Log.LogInfo("[DEBUG] Reset HeroBox.Inactive = false");

                // CRITICAL: Clear ALL invulnerability sources that block CanTakeDamage()
                // cState.Invulnerable returns true if invulnerable OR invulnerableCount > 0
                hero.cState.invulnerable = false;
                hero.cState.ClearInvulnerabilitySources();
                Plugin.Log.LogInfo("[DEBUG] Cleared cState invulnerability sources");

                // CRITICAL: Clear static HeroInvincibilitySource - blocks CanTakeDamage()!
                HeroInvincibilitySource.Clear();
                Plugin.Log.LogInfo($"[DEBUG] HeroInvincibilitySource.Clear(), IsActive={HeroInvincibilitySource.IsActive}");

                // Also reset parryInvulnTimer which blocks damage from enemies
                hero.parryInvulnTimer = 0f;

                // Reset downspikeInvulnerabilitySteps
                hero.cState.downspikeInvulnerabilitySteps = 0;

                // Reset invincibility state if not user-enabled
                if (!Player.CheatSystem.UserInvincible && !Player.CheatSystem.NoclipEnabled)
                {
                    Plugin.PD.isInvincible = false;
                }

                // Explicitly invoke FinishedEnteringScene to clear additional flags
                // Use Reflection as it is private in some versions
                var method = typeof(HeroController).GetMethod("FinishedEnteringScene", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(hero, new object[] { true, false });
                }

                // Notify GameManager that scene entry is complete
                // This ensures enemies properly recognize the player
                try
                {
                    GameManager.instance.FinishedEnteringScene();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to call GM.FinishedEnteringScene: {ex.Message}");
                }

                hero.GetComponent<MeshRenderer>().enabled = true;

                // Enable hero's main collider only (NOT child colliders - those include attack hitboxes!)
                var col2d = hero.GetComponent<Collider2D>();
                if (col2d != null) col2d.enabled = true;

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

                // Accept input
                hero.AcceptInput();

                // DEBUG: Log hero state for collision debugging
                Plugin.Log.LogInfo("========== HERO DEBUG LOG ==========");
                Plugin.Log.LogInfo($"Hero Layer: {hero.gameObject.layer} (LayerMask.LayerToName: {LayerMask.LayerToName(hero.gameObject.layer)})");
                Plugin.Log.LogInfo($"Hero Tag: {hero.gameObject.tag}");
                Plugin.Log.LogInfo($"Hero Active: {hero.gameObject.activeInHierarchy}");
                Plugin.Log.LogInfo($"Hero Position: {hero.transform.position}");
                Plugin.Log.LogInfo($"cState.transitioning: {hero.cState.transitioning}");
                Plugin.Log.LogInfo($"cState.dead: {hero.cState.dead}");
                Plugin.Log.LogInfo($"cState.hazardDeath: {hero.cState.hazardDeath}");
                Plugin.Log.LogInfo($"cState.Invulnerable: {hero.cState.Invulnerable}");
                Plugin.Log.LogInfo($"transitionState: {hero.transitionState}");
                Plugin.Log.LogInfo($"damageMode: {hero.damageMode}");
                Plugin.Log.LogInfo($"isInvincible (PD): {Plugin.PD?.isInvincible}");

                // Log all colliders on hero
                var heroColliders = hero.GetComponents<Collider2D>();
                Plugin.Log.LogInfo($"Hero Colliders ({heroColliders.Length}):");
                foreach (var col in heroColliders)
                {
                    Plugin.Log.LogInfo($"  - {col.GetType().Name}: enabled={col.enabled}, isTrigger={col.isTrigger}");
                }

                // Log heroBox if exists
                if (hero.heroBox != null)
                {
                    Plugin.Log.LogInfo($"HeroBox: active={hero.heroBox.gameObject.activeInHierarchy}");
                    var hbCol = hero.heroBox.GetComponent<Collider2D>();
                    if (hbCol != null)
                    {
                        Plugin.Log.LogInfo($"  HeroBox Collider: enabled={hbCol.enabled}, isTrigger={hbCol.isTrigger}");
                    }
                }

                // Log child objects with colliders
                Plugin.Log.LogInfo("Hero Child Colliders:");
                foreach (var col in hero.GetComponentsInChildren<Collider2D>(true))
                {
                    if (col.gameObject != hero.gameObject)
                    {
                        Plugin.Log.LogInfo($"  - {col.gameObject.name}: enabled={col.enabled}, active={col.gameObject.activeInHierarchy}");
                    }
                }
                Plugin.Log.LogInfo("========== END HERO DEBUG LOG ==========");

                Plugin.Log.LogInfo("[DEBUG] ApplyStateImmediate: Reset damageMode=FULL_DAMAGE, transitioning=false");
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

        // --- Enemy Debug Logging ---

        /// <summary>
        /// Logs detailed information about all enemies in the scene including hierarchy and components.
        /// </summary>
        private static void LogAllEnemiesDetailed(string context)
        {
            Plugin.Log.LogInfo($"========== ENEMY DEBUG LOG: {context} ==========");

            var enemies = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Plugin.Log.LogInfo($"Total enemies found: {enemies.Length}");

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.gameObject == null) continue;

                var go = enemy.gameObject;
                Plugin.Log.LogInfo($"--- ENEMY: {go.name} ---");
                Plugin.Log.LogInfo($"  Path: {GetGameObjectPath(go)}");
                Plugin.Log.LogInfo($"  Active: {go.activeInHierarchy} (self={go.activeSelf})");
                Plugin.Log.LogInfo($"  HP: {enemy.hp}, IsDead: {enemy.isDead}, IsInvincible: {enemy.IsInvincible}");
                Plugin.Log.LogInfo($"  Position: {go.transform.position}");

                // Log all components on main object
                var components = go.GetComponents<Component>();
                Plugin.Log.LogInfo($"  Components ({components.Length}):");
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    string enabled = "";
                    if (comp is Behaviour b) enabled = $" [enabled={b.enabled}]";
                    else if (comp is Collider2D c) enabled = $" [enabled={c.enabled}]";
                    else if (comp is Renderer r) enabled = $" [enabled={r.enabled}]";
                    Plugin.Log.LogInfo($"    - {comp.GetType().Name}{enabled}");
                }

                // Log FSM states
                var fsms = go.GetComponents<PlayMakerFSM>();
                if (fsms.Length > 0)
                {
                    Plugin.Log.LogInfo($"  FSMs ({fsms.Length}):");
                    foreach (var fsm in fsms)
                    {
                        Plugin.Log.LogInfo($"    - {fsm.FsmName}: state='{fsm.ActiveStateName}', enabled={fsm.enabled}");

                        // Log important FSM variables
                        if (fsm.FsmVariables != null)
                        {
                            foreach (var boolVar in fsm.FsmVariables.BoolVariables)
                            {
                                if (boolVar.Name.Contains("Spawn") || boolVar.Name.Contains("Active") ||
                                    boolVar.Name.Contains("Dead") || boolVar.Name.Contains("Alert") ||
                                    boolVar.Name.Contains("Hero") || boolVar.Name.Contains("Seen"))
                                {
                                    Plugin.Log.LogInfo($"      {boolVar.Name} = {boolVar.Value}");
                                }
                            }
                        }
                    }
                }

                // Log child hierarchy (first level only to avoid spam)
                Plugin.Log.LogInfo($"  Children ({go.transform.childCount}):");
                foreach (Transform child in go.transform)
                {
                    string childActive = child.gameObject.activeInHierarchy ? "active" : "INACTIVE";
                    Plugin.Log.LogInfo($"    - {child.name} [{childActive}]");
                }
            }

            Plugin.Log.LogInfo($"========== END ENEMY DEBUG LOG ==========");
        }

        // --- Enemy & Scene State Capture Methods ---

        private static List<EnemyStateData> CaptureEnemyStates()
        {
            // Log all enemies BEFORE capturing
            LogAllEnemiesDetailed("BEFORE CAPTURE");

            var enemyStates = new List<EnemyStateData>();
            // Find ALL enemies including inactive ones (important for enemies not yet spawned)
            var enemies = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Plugin.Log.LogInfo($"[DEBUG] CaptureEnemyStates: Found {enemies.Length} enemies (including inactive)");

            foreach (var enemy in enemies)
            {
                if (enemy.gameObject == null) continue;

                var data = new EnemyStateData();
                data.GameObjectName = enemy.gameObject.name;
                data.GameObjectPath = GetGameObjectPath(enemy.gameObject);
                data.IsActive = enemy.gameObject.activeInHierarchy;

                // HealthManager state
                data.HP = enemy.hp;
                data.IsDead = enemy.isDead;
                data.IsInvincible = enemy.IsInvincible;
                data.InvincibleFromDirection = enemy.InvincibleFromDirection;

                // Transform
                data.Position = enemy.transform.position;
                data.Rotation = enemy.transform.rotation;
                data.Scale = enemy.transform.localScale;

                // Rigidbody2D
                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    data.Velocity = rb.linearVelocity;
                    data.AngularVelocity = rb.angularVelocity;
                    data.IsKinematic = rb.bodyType == RigidbodyType2D.Kinematic;
                }

                // Recoil
                var recoil = enemy.GetComponent<Recoil>();
                if (recoil != null)
                {
                    // Access private fields via reflection
                    var stateField = typeof(Recoil).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
                    var durationField = typeof(Recoil).GetField("recoilTimeRemaining", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (stateField != null)
                    {
                        var stateVal = stateField.GetValue(recoil);
                        data.IsRecoiling = (int)stateVal == 2; // Assuming 2 is Recoiling
                    }
                    if (durationField != null)
                    {
                        data.RecoilTimeRemaining = (float)durationField.GetValue(recoil);
                    }
                }

                // FSMs
                var fsms = enemy.GetComponents<PlayMakerFSM>();
                foreach (var fsm in fsms)
                {
                    data.FsmStates.Add(CaptureFsmState(fsm));
                }

                // Capture detailed component state for main object
                data.MainObjectState = CaptureObjectComponentData(enemy.gameObject);

                // Capture detailed component state for all descendants (recursive)
                var allChildren = enemy.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child == enemy.transform) continue; // Skip root as it's already captured

                    string relativePath = GetRelativePath(child, enemy.transform);
                    data.ChildStates[relativePath] = CaptureObjectComponentData(child.gameObject);
                }

                enemyStates.Add(data);
            }

            return enemyStates;
        }

        private static FsmStateData CaptureFsmState(PlayMakerFSM fsm)
        {
            var data = new FsmStateData();
            data.FsmName = fsm.FsmName;
            data.ActiveStateName = fsm.ActiveStateName;

            // Capture Variables safely
            if (fsm.FsmVariables != null)
            {
                foreach (var v in fsm.FsmVariables.BoolVariables) data.BoolVariables[v.Name] = v.Value;
                foreach (var v in fsm.FsmVariables.IntVariables) data.IntVariables[v.Name] = v.Value;
                foreach (var v in fsm.FsmVariables.FloatVariables) data.FloatVariables[v.Name] = v.Value;
                foreach (var v in fsm.FsmVariables.StringVariables) data.StringVariables[v.Name] = v.Value;
                foreach (var v in fsm.FsmVariables.Vector3Variables) data.Vector3Variables[v.Name] = v.Value;
            }

            return data;
        }

        private static BattleSceneStateData CaptureBattleSceneState()
        {
            var battleScene = UnityEngine.Object.FindObjectOfType<BattleScene>();
            if (battleScene == null) return null;

            var data = new BattleSceneStateData();
            data.GameObjectPath = GetGameObjectPath(battleScene.gameObject);

            // Public fields
            data.CurrentWave = battleScene.currentWave;
            data.CurrentEnemies = battleScene.currentEnemies;
            data.EnemiesToNext = battleScene.enemiesToNext;

            // Private 'started' field - use reflection
            var startedField = typeof(BattleScene).GetField("started", BindingFlags.Instance | BindingFlags.NonPublic);
            if (startedField != null)
            {
                data.Started = (bool)(startedField.GetValue(battleScene) ?? false);
            }

            Plugin.Log.LogInfo($"[DEBUG] Captured BattleScene: wave={data.CurrentWave}, enemies={data.CurrentEnemies}, started={data.Started}");

            return data;
        }

        private static BossSceneStateData CaptureBossSceneState()
        {
            var bossCtrl = UnityEngine.Object.FindObjectOfType<BossSceneController>();
            if (bossCtrl == null) return null;

            var data = new BossSceneStateData();
            data.IsActive = true;

            // BossLevel and HasTransitionedIn are public properties
            data.BossLevel = bossCtrl.BossLevel;
            data.HasTransitionedIn = bossCtrl.HasTransitionedIn;

            return data;
        }

        private static void RestoreEnemyStates(List<EnemyStateData> enemyStates)
        {
            if (enemyStates == null) return;

            // Log all enemies BEFORE restoring
            LogAllEnemiesDetailed("BEFORE RESTORE");

            // Find ALL enemies including inactive ones
            var currentEnemies = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Plugin.Log.LogInfo($"[DEBUG] RestoreEnemyStates: Found {currentEnemies.Length} enemies to restore from {enemyStates.Count} saved states");

            foreach (var state in enemyStates)
            {
                var enemy = currentEnemies.FirstOrDefault(e => GetGameObjectPath(e.gameObject) == state.GameObjectPath);
                if (enemy == null)
                {
                    Plugin.Log.LogWarning($"Could not find enemy to restore: {state.GameObjectPath}");
                    continue;
                }

                // FIRST: Restore active state (important - must be done first!)
                enemy.gameObject.SetActive(state.IsActive);
                Plugin.Log.LogInfo($"[DEBUG] Restored enemy '{state.GameObjectName}' active={state.IsActive}");

                // Restore HealthManager
                enemy.hp = state.HP;
                enemy.isDead = state.IsDead;
                enemy.IsInvincible = state.IsInvincible;
                enemy.InvincibleFromDirection = state.InvincibleFromDirection;

                // Restore Transform
                enemy.transform.position = state.Position;
                enemy.transform.rotation = state.Rotation;
                enemy.transform.localScale = state.Scale;

                // Restore Rigidbody
                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = state.Velocity;
                    rb.angularVelocity = state.AngularVelocity;
                    if (state.IsKinematic) rb.bodyType = RigidbodyType2D.Kinematic;
                }

                // Restore Recoil
                var recoil = enemy.GetComponent<Recoil>();
                if (recoil != null)
                {
                    if (state.IsRecoiling)
                    {
                        var stateField = typeof(Recoil).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
                        stateField?.SetValue(recoil, 2);
                        var durationField = typeof(Recoil).GetField("recoilTimeRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
                        durationField?.SetValue(recoil, state.RecoilTimeRemaining);
                    }
                    else
                    {
                        var stateField = typeof(Recoil).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
                        stateField?.SetValue(recoil, 0);
                        recoil.CancelRecoil();
                    }
                }

                // Restore FSMs
                var fsms = enemy.GetComponents<PlayMakerFSM>();
                foreach (var fsmData in state.FsmStates)
                {
                    var fsm = fsms.FirstOrDefault(f => f.FsmName == fsmData.FsmName);
                    if (fsm != null)
                    {
                        RestoreFsmState(fsm, fsmData);
                    }
                }

                // Restore detailed component state for main object
                if (state.MainObjectState != null)
                {
                    RestoreObjectComponentData(enemy.gameObject, state.MainObjectState);
                }

                // Restore child object active states and components (recursive by path)
                if (state.ChildStates != null && state.ChildStates.Count > 0)
                {
                    foreach (var kvp in state.ChildStates)
                    {
                        string relativePath = kvp.Key;
                        Transform childTransform = enemy.transform.Find(relativePath); // Find supports "Path/To/Child"

                        if (childTransform != null)
                        {
                            RestoreObjectComponentData(childTransform.gameObject, kvp.Value);
                        }
                        else
                        {
                            Plugin.Log.LogError($"[ERROR] Could not find child '{relativePath}' on '{state.GameObjectName}' to restore state.");
                        }
                    }
                    Plugin.Log.LogInfo($"[DEBUG] Restored full state for {state.ChildStates.Count} child objects on {state.GameObjectName}");
                }
                // Fallback for old saves or mixed data (Legacy)
                else if (state.ChildObjectStates != null && state.ChildObjectStates.Count > 0)
                {
                    foreach (Transform child in enemy.transform)
                    {
                        if (state.ChildObjectStates.TryGetValue(child.name, out bool isActive))
                        {
                            child.gameObject.SetActive(isActive);
                        }
                    }
                    Plugin.Log.LogInfo($"[DEBUG] Restored (legacy) active state for {state.ChildObjectStates.Count} child objects on {state.GameObjectName}");
                }

                if (state.IsDead)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            // Handle enemies that exist now but weren't in the save (spawned after save was made)
            // These should be deactivated to match the saved state
            var savedPaths = new HashSet<string>(enemyStates.Select(s => s.GameObjectPath));
            foreach (var enemy in currentEnemies)
            {
                string path = GetGameObjectPath(enemy.gameObject);
                if (!savedPaths.Contains(path))
                {
                    // This enemy wasn't in the save - it was spawned after the save was made
                    // Deactivate it to match the saved state
                    if (enemy.gameObject.activeInHierarchy)
                    {
                        Plugin.Log.LogInfo($"[DEBUG] Deactivating enemy not in save: {enemy.gameObject.name}");
                        enemy.gameObject.SetActive(false);
                    }
                }
            }

            // Log all enemies AFTER restoring
            LogAllEnemiesDetailed("AFTER RESTORE");
        }

        private static void RestoreFsmState(PlayMakerFSM fsm, FsmStateData data)
        {
            Plugin.Log.LogInfo($"[DEBUG] RestoreFsmState: {fsm.gameObject.name}/{fsm.FsmName} -> target state '{data.ActiveStateName}'");

            if (fsm.FsmVariables != null)
            {
                int varCount = 0;
                foreach (var kvp in data.BoolVariables) { fsm.FsmVariables.GetFsmBool(kvp.Key).Value = kvp.Value; varCount++; }
                foreach (var kvp in data.IntVariables) { fsm.FsmVariables.GetFsmInt(kvp.Key).Value = kvp.Value; varCount++; }
                foreach (var kvp in data.FloatVariables) { fsm.FsmVariables.GetFsmFloat(kvp.Key).Value = kvp.Value; varCount++; }
                foreach (var kvp in data.StringVariables) { fsm.FsmVariables.GetFsmString(kvp.Key).Value = kvp.Value; varCount++; }
                foreach (var kvp in data.Vector3Variables) { fsm.FsmVariables.GetFsmVector3(kvp.Key).Value = kvp.Value; varCount++; }
                Plugin.Log.LogInfo($"[DEBUG] Restored {varCount} FSM variables");

                // For Control FSMs (bosses/enemies), ensure Spawned is true if it exists
                // The Spawned variable controls whether the enemy is visible (vs in cocoon)
                if (fsm.FsmName == "Control")
                {
                    var spawnedVar = fsm.FsmVariables.GetFsmBool("Spawned");
                    if (spawnedVar != null)
                    {
                        Plugin.Log.LogInfo($"[DEBUG] Found 'Spawned' variable, current={spawnedVar.Value}, forcing to true");
                        spawnedVar.Value = true;
                    }

                    // Also ensure "Done First Spawn" is true
                    var doneFirstSpawn = fsm.FsmVariables.GetFsmBool("Done First Spawn");
                    if (doneFirstSpawn != null && !doneFirstSpawn.Value)
                    {
                        Plugin.Log.LogInfo($"[DEBUG] Found 'Done First Spawn' variable, current={doneFirstSpawn.Value}, forcing to true");
                        doneFirstSpawn.Value = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(data.ActiveStateName))
            {
                try
                {
                    // Check current state before changing
                    string currentState = fsm.ActiveStateName;
                    Plugin.Log.LogInfo($"[DEBUG] FSM current state: '{currentState}', target: '{data.ActiveStateName}'");

                    // For "Control" FSMs (boss behavior), we need special handling
                    if (fsm.FsmName == "Control")
                    {
                        // Check if the saved state was an "active" state (enemy was awake/walking)
                        // These states require proper entry actions that don't run with SetState
                        bool wasAwakeState = data.ActiveStateName.ToLower().Contains("walk") ||
                                             data.ActiveStateName.ToLower().Contains("start") ||
                                             data.ActiveStateName.ToLower().Contains("idle") ||
                                             data.ActiveStateName.ToLower().Contains("attack") ||
                                             data.ActiveStateName.ToLower().Contains("chase");

                        // If enemy was in an "awake" state, send ALERT event instead of setting state directly
                        // This allows the FSM to go through proper wake-up transitions with entry actions
                        if (wasAwakeState && (currentState.ToLower().Contains("hid") ||
                                               currentState.ToLower().Contains("sleep") ||
                                               currentState.ToLower().Contains("wait")))
                        {
                            Plugin.Log.LogInfo($"[DEBUG] Enemy was awake (state={data.ActiveStateName}), sending ALERT to wake up naturally");
                            fsm.SendEvent("ALERT");
                            fsm.SendEvent("WAKE");
                            fsm.SendEvent("ACTIVATE");
                        }
                        else
                        {
                            // Set the state directly for non-wake states
                            fsm.Fsm.SetState(data.ActiveStateName);
                            Plugin.Log.LogInfo($"[DEBUG] Set FSM state to '{data.ActiveStateName}', new active: '{fsm.ActiveStateName}'");
                        }

                        // Try to restart the FSM to force it to properly enter the current state
                        // This forces PlayMaker to run the entry actions
                        try
                        {
                            // Use reflection to call Fsm.Start() which re-initializes and enters current state
                            var fsmType = fsm.Fsm.GetType();

                            // Try to call EnterState to properly enter with actions
                            var activeStateField = fsmType.GetField("ActiveState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (activeStateField != null)
                            {
                                var activeState = activeStateField.GetValue(fsm.Fsm);
                                if (activeState != null)
                                {
                                    // Try to call OnEnter actions manually via reflection
                                    var stateType = activeState.GetType();
                                    var actionsField = stateType.GetField("Actions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (actionsField != null)
                                    {
                                        var actions = actionsField.GetValue(activeState) as Array;
                                        if (actions != null)
                                        {
                                            foreach (var action in actions)
                                            {
                                                if (action != null)
                                                {
                                                    var onEnter = action.GetType().GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                                    onEnter?.Invoke(action, null);
                                                }
                                            }
                                            Plugin.Log.LogInfo($"[DEBUG] Triggered OnEnter for {actions.Length} actions in state '{data.ActiveStateName}'");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Plugin.Log.LogWarning($"[DEBUG] Could not run state entry actions: {e.Message}");
                        }

                        // Also sync the Animator if present
                        var animator = fsm.gameObject.GetComponent<Animator>();
                        if (animator != null)
                        {
                            // Try to set animator to appropriate state based on FSM state
                            animator.Play(data.ActiveStateName, -1, 0f);
                            Plugin.Log.LogInfo($"[DEBUG] Triggered Animator.Play('{data.ActiveStateName}')");
                        }
                    }
                    else
                    {
                        // For non-Control FSMs, just set the state
                        fsm.Fsm.SetState(data.ActiveStateName);
                        Plugin.Log.LogInfo($"[DEBUG] Set FSM state to '{data.ActiveStateName}', new active: '{fsm.ActiveStateName}'");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to set FSM state {data.ActiveStateName} on {fsm.name}: {e.Message}");
                }
            }
        }

        private static void ForceSavePersistentItems()
        {
            // The game only persists PersistentBoolItem states on scene exit
            // We need to force them to save their current state to SceneData NOW before we serialize it
            // IMPORTANT: UpdateValue() only updates itemData.Value, but SaveStateNoCondition() actually calls SaveValue()
            // which writes to SceneData.PersistentBools!

            var persistentItems = UnityEngine.Object.FindObjectsOfType<PersistentBoolItem>();
            // SaveStateNoCondition is public - no need for reflection!

            Plugin.Log.LogInfo($"[DEBUG] ForceSavePersistentItems: Found {persistentItems.Length} items");

            int savedCount = 0;
            foreach (var item in persistentItems)
            {
                try
                {
                    // Call SaveStateNoCondition() which:
                    // 1. Calls OnGetSaveState or UpdateActivatedFromFSM to get current value
                    // 2. Calls SaveValue(itemData) to actually write to SceneData.PersistentBools!
                    item.SaveStateNoCondition();
                    savedCount++;

                    // Log the value that was saved
                    string id = item.GetId();
                    string sceneName = item.GetSceneName();
                    if (string.IsNullOrEmpty(id)) id = item.name;
                    if (string.IsNullOrEmpty(sceneName)) sceneName = GameManager.GetBaseSceneName(item.gameObject.scene.name);

                    bool savedValue = SceneData.instance.PersistentBools.GetValueOrDefault(sceneName, id);
                    Plugin.Log.LogInfo($"[DEBUG] Saved '{item.name}' (scene={sceneName}, id={id}) = {savedValue}");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Failed to save persistent item {item.name}: {e.Message}");
                }
            }

            Plugin.Log.LogInfo($"[DEBUG] ForceSavePersistentItems: Saved {savedCount} items");
        }

        private static void RestorePersistentBoolItems()
        {
            // PersistentBoolItems already ran Start() during scene initialization
            // BUT they may have loaded from OLD SceneData before we restored ours
            // We need to FORCE them to re-read the restored SceneData values


            var persistentItems = UnityEngine.Object.FindObjectsOfType<PersistentBoolItem>();
            int restoredCount = 0;
            int trueCount = 0;

            // Get the base PersistentItem<bool> type for reflection
            var persistentItemType = typeof(PersistentBoolItem).BaseType; // PersistentItem<bool>
            var startedField = persistentItemType?.GetField("started", BindingFlags.Instance | BindingFlags.NonPublic);
            var hasSetupField = persistentItemType?.GetField("hasSetup", BindingFlags.Instance | BindingFlags.NonPublic);

            Plugin.Log.LogInfo($"[DEBUG] Found {persistentItems.Length} PersistentBoolItems to process");
            Plugin.Log.LogInfo($"[DEBUG] startedField found: {startedField != null}, hasSetupField found: {hasSetupField != null}");

            foreach (var item in persistentItems)
            {
                try
                {
                    // Get the item's ID and SceneName using public methods
                    string id = item.GetId();
                    string sceneName = item.GetSceneName();

                    // If ID is empty, use game object name (matching EnsureSetup behavior)
                    if (string.IsNullOrEmpty(id)) id = item.name;
                    if (string.IsNullOrEmpty(sceneName)) sceneName = GameManager.GetBaseSceneName(item.gameObject.scene.name);

                    // Check if SceneData has a TRUE value for this item
                    bool savedValue = SceneData.instance.PersistentBools.GetValueOrDefault(sceneName, id);

                    Plugin.Log.LogInfo($"[DEBUG] PersistentBoolItem '{item.name}' (scene={sceneName}, id={id}): SceneData value = {savedValue}");

                    if (savedValue)
                    {
                        // Force the value by calling SetValueOverride which triggers callbacks
                        item.SetValueOverride(true);
                        trueCount++;
                        Plugin.Log.LogInfo($"[DEBUG] Applied SetValueOverride(true) to '{item.name}'");
                    }

                    restoredCount++;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Failed to restore persistent item {item.name}: {e.Message}");
                }
            }

            Plugin.Log.LogInfo($"[DEBUG] Restored {restoredCount} PersistentBoolItems, {trueCount} had true value in SceneData");
        }

        private static void RestoreBattleSceneState(BattleSceneStateData data)
        {
            if (data == null) return;
            var battleScene = UnityEngine.Object.FindObjectOfType<BattleScene>();
            if (battleScene == null) return;

            var type = typeof(BattleScene);

            // Restore battle state
            battleScene.currentWave = data.CurrentWave;
            battleScene.currentEnemies = data.CurrentEnemies;
            battleScene.enemiesToNext = data.EnemiesToNext;

            // Set started=true if CurrentWave > 0 (battle has started)
            if (data.Started || data.CurrentWave > 0)
            {
                var startedField = type.GetField("started", BindingFlags.Instance | BindingFlags.NonPublic);
                if (startedField != null)
                {
                    startedField.SetValue(battleScene, true);
                    Plugin.Log.LogInfo("[DEBUG] Set BattleScene.started = true");
                }

                // Disable the collider (battle already triggered)
                var boxCollider = battleScene.GetComponent<BoxCollider2D>();
                if (boxCollider != null) boxCollider.enabled = false;

                var polyCollider = battleScene.GetComponent<PolygonCollider2D>();
                if (polyCollider != null) polyCollider.enabled = false;

                // IMPORTANT: Call LockInBattle() to close arena gates
                // This sends "BG CLOSE" event to gate FSMs and activates camera locks
                battleScene.LockInBattle();
                Plugin.Log.LogInfo("[DEBUG] Called BattleScene.LockInBattle() to close gates");
            }
        }

        private static void RestoreBossSceneState(BossSceneStateData data)
        {
            if (data == null || !data.IsActive) return;
            var bossCtrl = UnityEngine.Object.FindObjectOfType<BossSceneController>();
            if (bossCtrl == null) return;

            // BossLevel is a public property with setter
            bossCtrl.BossLevel = data.BossLevel;
            // HasTransitionedIn doesn't have a public setter, would need reflection if needed
            // For now, skip setting HasTransitionedIn as it's typically set by scene logic
        }

        private static void RestoreObjectComponentData(GameObject obj, ObjectComponentData data)
        {
            if (data == null) return;

            obj.SetActive(data.IsActive);

            if (data.Transform != null)
            {
                obj.transform.localPosition = data.Transform.LocalPosition;
                obj.transform.localRotation = data.Transform.LocalRotation;
                obj.transform.localScale = data.Transform.LocalScale;
            }

            if (data.SpriteRenderer != null)
            {
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = data.SpriteRenderer.Enabled;
                    sr.color = data.SpriteRenderer.Color;
                    sr.sortingOrder = data.SpriteRenderer.SortingOrder;
                    sr.sortingLayerName = data.SpriteRenderer.SortingLayerName;
                }
            }

            if (data.MeshRenderer != null)
            {
                var mr = obj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = data.MeshRenderer.Enabled;
                    mr.sortingOrder = data.MeshRenderer.SortingOrder;
                    mr.sortingLayerName = data.MeshRenderer.SortingLayerName;
                }
            }

            if (data.SkinnedMeshRenderer != null)
            {
                var smr = obj.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.enabled = data.SkinnedMeshRenderer.Enabled;
                    smr.sortingOrder = data.SkinnedMeshRenderer.SortingOrder;
                    smr.sortingLayerName = data.SkinnedMeshRenderer.SortingLayerName;
                }
            }

            if (data.Animator != null)
            {
                var anim = obj.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.enabled = data.Animator.Enabled;
                    if (data.Animator.Enabled)
                    {
                        anim.Play(data.Animator.StateHash, 0, data.Animator.NormalizedTime);
                        anim.speed = data.Animator.Speed;
                    }
                }
            }

            if (data.Collider2D != null)
            {
                var col = obj.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = data.Collider2D.Enabled;
                    col.isTrigger = data.Collider2D.IsTrigger;
                }
            }
        }

        private static ObjectComponentData CaptureObjectComponentData(GameObject obj)
        {
            var data = new ObjectComponentData();
            data.IsActive = obj.activeSelf;

            data.Transform = new TransformData
            {
                LocalPosition = obj.transform.localPosition,
                LocalRotation = obj.transform.localRotation,
                LocalScale = obj.transform.localScale
            };

            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                data.SpriteRenderer = new SpriteRendererData
                {
                    Enabled = sr.enabled,
                    Color = sr.color,
                    SortingOrder = sr.sortingOrder,
                    SortingLayerName = sr.sortingLayerName,
                    SpriteName = sr.sprite != null ? sr.sprite.name : "null"
                };
            }

            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                data.MeshRenderer = new MeshRendererData
                {
                    Enabled = mr.enabled,
                    SortingLayerName = mr.sortingLayerName,
                    SortingOrder = mr.sortingOrder
                };
            }

            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                data.SkinnedMeshRenderer = new SkinnedMeshRendererData
                {
                    Enabled = smr.enabled,
                    SortingLayerName = smr.sortingLayerName,
                    SortingOrder = smr.sortingOrder
                };
            }

            var anim = obj.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                data.Animator = new AnimatorData
                {
                    Enabled = anim.enabled,
                    StateHash = stateInfo.shortNameHash,
                    NormalizedTime = stateInfo.normalizedTime,
                    Speed = anim.speed
                };
            }

            var col = obj.GetComponent<Collider2D>();
            if (col != null)
            {
                data.Collider2D = new ColliderData
                {
                    Enabled = col.enabled,
                    IsTrigger = col.isTrigger
                };
            }

            // Detailed logging as requested
            // if (obj.activeSelf) // Log everything for now to be safe
            {
                string log = $"[DEBUG] Captured '{obj.name}': active={data.IsActive}";
                if (data.SpriteRenderer != null) log += $", SR[en={data.SpriteRenderer.Enabled}, sort={data.SpriteRenderer.SortingOrder}, sprite={data.SpriteRenderer.SpriteName}]";
                if (data.MeshRenderer != null) log += $", MR[en={data.MeshRenderer.Enabled}, sort={data.MeshRenderer.SortingOrder}]";
                if (data.SkinnedMeshRenderer != null) log += $", SMR[en={data.SkinnedMeshRenderer.Enabled}, sort={data.SkinnedMeshRenderer.SortingOrder}]";
                if (data.Animator != null) log += $", Anim[en={data.Animator.Enabled}, hash={data.Animator.StateHash}]";
                if (data.Collider2D != null) log += $", Col[en={data.Collider2D.Enabled}]";
                Plugin.Log.LogInfo(log);
            }

            return data;
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            if (child == root) return "";
            string path = child.name;
            Transform parent = child.parent;
            while (parent != null && parent != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
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
