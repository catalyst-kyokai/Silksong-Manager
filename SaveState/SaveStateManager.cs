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

            // Restore Enemy & Scene States
            try { RestoreEnemyStates(state.EnemyStates); } catch (Exception e) { Plugin.Log.LogError("Error restoring enemies: " + e); }
            try { RestoreBattleSceneState(state.BattleSceneState); } catch (Exception e) { Plugin.Log.LogError("Error restoring battle: " + e); }
            try { RestoreBossSceneState(state.BossSceneState); } catch (Exception e) { Plugin.Log.LogError("Error restoring boss: " + e); }

            // Apply Hero State
            ApplyStateImmediate(state);

            // Handle Unpause unconditionally
            // if (GameManager.instance.isPaused) // Do it always to be safe
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

                // Force GameState to PLAYING via Reflection
                try
                {
                    var setStateMethod = typeof(GameManager).GetMethod("SetState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (setStateMethod != null)
                    {
                        setStateMethod.Invoke(GameManager.instance, new object[] { GameState.PLAYING });
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("Failed to force GameState: " + ex.Message);
                }

                Plugin.Hero.AcceptInput();
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
                hero.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
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

        // --- Enemy & Scene State Capture Methods ---

        private static List<EnemyStateData> CaptureEnemyStates()
        {
            var enemyStates = new List<EnemyStateData>();
            var enemies = UnityEngine.Object.FindObjectsOfType<HealthManager>();

            foreach (var enemy in enemies)
            {
                if (enemy.gameObject == null) continue;

                var data = new EnemyStateData();
                data.GameObjectName = enemy.gameObject.name;
                data.GameObjectPath = GetGameObjectPath(enemy.gameObject);

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

            // currentWave is public in BattleScene
            data.CurrentWave = battleScene.currentWave;
            // currentEnemies and enemiesToNext are private, use reflection
            var type = typeof(BattleScene);
            data.CurrentEnemies = (int)(type.GetField("currentEnemies", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(battleScene) ?? 0);
            data.EnemiesToNext = (int)(type.GetField("enemiesToNext", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(battleScene) ?? 0);

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

            var currentEnemies = UnityEngine.Object.FindObjectsOfType<HealthManager>();

            foreach (var state in enemyStates)
            {
                var enemy = currentEnemies.FirstOrDefault(e => GetGameObjectPath(e.gameObject) == state.GameObjectPath);
                if (enemy == null)
                {
                    Plugin.Log.LogWarning($"Could not find enemy to restore: {state.GameObjectPath}");
                    continue;
                }

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

                if (state.IsDead)
                {
                    enemy.gameObject.SetActive(false);
                }
            }
        }

        private static void RestoreFsmState(PlayMakerFSM fsm, FsmStateData data)
        {
            if (fsm.FsmVariables != null)
            {
                foreach (var kvp in data.BoolVariables) fsm.FsmVariables.GetFsmBool(kvp.Key).Value = kvp.Value;
                foreach (var kvp in data.IntVariables) fsm.FsmVariables.GetFsmInt(kvp.Key).Value = kvp.Value;
                foreach (var kvp in data.FloatVariables) fsm.FsmVariables.GetFsmFloat(kvp.Key).Value = kvp.Value;
                foreach (var kvp in data.StringVariables) fsm.FsmVariables.GetFsmString(kvp.Key).Value = kvp.Value;
                foreach (var kvp in data.Vector3Variables) fsm.FsmVariables.GetFsmVector3(kvp.Key).Value = kvp.Value;
            }

            if (!string.IsNullOrEmpty(data.ActiveStateName))
            {
                try
                {
                    fsm.Fsm.SetState(data.ActiveStateName);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to set FSM state {data.ActiveStateName} on {fsm.name}: {e.Message}");
                }
            }
        }

        private static void RestoreBattleSceneState(BattleSceneStateData data)
        {
            if (data == null) return;
            var battleScene = UnityEngine.Object.FindObjectOfType<BattleScene>();
            if (battleScene == null) return;

            // currentWave is public
            battleScene.currentWave = data.CurrentWave;
            // currentEnemies and enemiesToNext are private - use reflection if needed
            var type = typeof(BattleScene);
            type.GetField("currentEnemies", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(battleScene, data.CurrentEnemies);
            type.GetField("enemiesToNext", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(battleScene, data.EnemiesToNext);
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
