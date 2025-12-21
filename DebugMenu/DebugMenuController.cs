using UnityEngine;

namespace SilksongManager.DebugMenu
{
    /// <summary>
    /// Main debug menu controller using Unity IMGUI.
    /// </summary>
    public class DebugMenuController : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect;
        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Player", "Currency", "World", "Tools", "Enemies", "Debug" };

        private GUIStyle _windowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized = false;

        private void Start()
        {
            _windowRect = new Rect(20, 20, 400, 600);
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 5, 5)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        public void ToggleMenu()
        {
            _isVisible = !_isVisible;
            Plugin.Log.LogInfo($"Debug menu: {(_isVisible ? "Opened" : "Closed")}");
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            InitStyles();

            _windowRect = GUILayout.Window(
                12345,
                _windowRect,
                DrawWindow,
                $"Silksong Manager v{PluginInfo.VERSION}",
                _windowStyle
            );
        }

        private void DrawWindow(int windowId)
        {
            // Tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabNames.Length; i++)
            {
                if (GUILayout.Toggle(_selectedTab == i, _tabNames[i], "Button"))
                {
                    _selectedTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Content with scrollview
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(500));

            switch (_selectedTab)
            {
                case 0: DrawPlayerTab(); break;
                case 1: DrawCurrencyTab(); break;
                case 2: DrawWorldTab(); break;
                case 3: DrawToolsTab(); break;
                case 4: DrawEnemiesTab(); break;
                case 5: DrawDebugTab(); break;
            }

            GUILayout.EndScrollView();

            // Close button
            GUILayout.Space(10);
            if (GUILayout.Button("Close (F1)", _buttonStyle))
            {
                ToggleMenu();
            }

            GUI.DragWindow();
        }

        private void DrawPlayerTab()
        {
            var state = Player.PlayerActions.GetStateInfo();

            GUILayout.Label("=== Player Status ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"Scene: {state.SceneName}", _labelStyle);
            GUILayout.Label($"Position: {state.Position}", _labelStyle);
            GUILayout.Label($"Health: {state.Health}/{state.MaxHealth}", _labelStyle);
            GUILayout.Label($"Silk: {state.Silk}/{state.MaxSilk}", _labelStyle);
            GUILayout.Label($"Invincible: {state.IsInvincible}", _labelStyle);
            GUILayout.Label($"Facing: {(state.FacingRight ? "Right" : "Left")}", _labelStyle);
            GUILayout.Label($"On Ground: {state.OnGround}", _labelStyle);

            GUILayout.Space(10);
            GUILayout.Label("=== Actions ===", _headerStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Quick Heal (F2)", _buttonStyle))
                Player.PlayerActions.QuickHeal();

            if (GUILayout.Button("Refill Silk (F3)", _buttonStyle))
                Player.PlayerActions.QuickSilk();

            if (GUILayout.Button($"Toggle Invincibility (F4) [{(Plugin.PD?.isInvincible == true ? "ON" : "OFF")}]", _buttonStyle))
                Player.PlayerActions.ToggleInvincibility();

            if (GUILayout.Button($"Toggle Infinite Jumps (F6) [{(Player.PlayerActions.IsInfiniteJumpsEnabled ? "ON" : "OFF")}]", _buttonStyle))
                Player.PlayerActions.ToggleInfiniteJumps();

            if (GUILayout.Button($"Toggle Noclip (F7) [{(Player.PlayerActions.IsNoclipEnabled ? "ON" : "OFF")}]", _buttonStyle))
                Player.PlayerActions.ToggleNoclip();

            GUILayout.Space(5);

            if (GUILayout.Button("Unlock All Abilities", _buttonStyle))
                Player.PlayerActions.UnlockAllAbilities();

            if (GUILayout.Button("Kill Player", _buttonStyle))
                Player.PlayerActions.KillPlayer();

            if (GUILayout.Button("Respawn Player", _buttonStyle))
                Player.PlayerActions.RespawnPlayer();
        }

        private void DrawCurrencyTab()
        {
            var info = Currency.CurrencyActions.GetCurrencyInfo();

            GUILayout.Label("=== Currency Status ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"Geo: {info.Geo}", _labelStyle);
            GUILayout.Label($"Shell Shards: {info.ShellShards}", _labelStyle);

            GUILayout.Space(10);
            GUILayout.Label("=== Actions ===", _headerStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Add 100 Geo", _buttonStyle))
                Currency.CurrencyActions.AddGeo(100);

            if (GUILayout.Button("Add 1000 Geo (F5)", _buttonStyle))
                Currency.CurrencyActions.AddGeo(1000);

            if (GUILayout.Button("Add 10000 Geo", _buttonStyle))
                Currency.CurrencyActions.AddGeo(10000);

            if (GUILayout.Button("Set Geo to 99999", _buttonStyle))
                Currency.CurrencyActions.SetGeo(99999);

            if (GUILayout.Button("Reset Geo to 0", _buttonStyle))
                Currency.CurrencyActions.SetGeo(0);

            GUILayout.Space(5);

            if (GUILayout.Button("Add 10 Shards", _buttonStyle))
                Currency.CurrencyActions.AddShards(10);

            if (GUILayout.Button("Add 100 Shards", _buttonStyle))
                Currency.CurrencyActions.AddShards(100);
        }

        private void DrawWorldTab()
        {
            var info = World.WorldActions.GetWorldInfo();

            GUILayout.Label("=== World Status ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"Scene: {info.CurrentScene}", _labelStyle);
            GUILayout.Label($"Entry Gate: {info.EntryGate}", _labelStyle);
            GUILayout.Label($"Game Paused: {info.IsGamePaused}", _labelStyle);
            GUILayout.Label($"Time Scale: {Time.timeScale}x", _labelStyle);

            GUILayout.Space(10);
            GUILayout.Label("=== Position ===", _headerStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Save Position (F9)", _buttonStyle))
                World.WorldActions.SavePosition();

            if (GUILayout.Button("Load Position (F10)", _buttonStyle))
                World.WorldActions.LoadPosition();

            GUILayout.Space(10);
            GUILayout.Label("=== Game Speed ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.25x", _buttonStyle))
                World.WorldActions.SetGameSpeed(0.25f);
            if (GUILayout.Button("0.5x", _buttonStyle))
                World.WorldActions.SetGameSpeed(0.5f);
            if (GUILayout.Button("1x", _buttonStyle))
                World.WorldActions.SetGameSpeed(1f);
            if (GUILayout.Button("2x", _buttonStyle))
                World.WorldActions.SetGameSpeed(2f);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("Pause Game", _buttonStyle))
                World.WorldActions.PauseGame();

            if (GUILayout.Button("Resume Game", _buttonStyle))
                World.WorldActions.ResumeGame();

            if (GUILayout.Button("Reload Scene", _buttonStyle))
                World.WorldActions.ReloadCurrentScene();
        }

        private void DrawToolsTab()
        {
            GUILayout.Label("=== Tools & Crests ===", _headerStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Unlock All Tools", _buttonStyle))
                Tools.ToolActions.UnlockAllTools();

            if (GUILayout.Button("Unlock All Crests", _buttonStyle))
                Tools.ToolActions.UnlockAllCrests();

            if (GUILayout.Button("Replenish All Tools", _buttonStyle))
                Tools.ToolActions.ReplenishAllTools();

            GUILayout.Space(10);
            GUILayout.Label("=== Unlocked Tools ===", _headerStyle);
            GUILayout.Space(5);

            var tools = Tools.ToolActions.GetUnlockedTools();
            foreach (var tool in tools)
            {
                GUILayout.Label($"• {tool.Name}", _labelStyle);
            }

            if (tools.Count == 0)
            {
                GUILayout.Label("No tools unlocked", _labelStyle);
            }
        }

        private void DrawEnemiesTab()
        {
            GUILayout.Label("=== Enemies ===", _headerStyle);
            GUILayout.Space(5);

            if (GUILayout.Button("Kill All Enemies", _buttonStyle))
                Enemies.EnemyActions.KillAllEnemies();

            if (GUILayout.Button("Damage All Enemies (50 HP)", _buttonStyle))
                Enemies.EnemyActions.DamageAllEnemies(50);

            if (GUILayout.Button("Freeze All Enemies", _buttonStyle))
                Enemies.EnemyActions.FreezeAllEnemies();

            if (GUILayout.Button("Unfreeze All Enemies", _buttonStyle))
                Enemies.EnemyActions.UnfreezeAllEnemies();

            GUILayout.Space(10);
            GUILayout.Label("=== Enemy List ===", _headerStyle);
            GUILayout.Space(5);

            var enemies = Enemies.EnemyActions.FindAllEnemies();
            for (int i = 0; i < enemies.Count && i < 20; i++)
            {
                var enemy = enemies[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{enemy.Name} (HP: {enemy.CurrentHP})", _labelStyle, GUILayout.Width(250));
                if (GUILayout.Button("Kill", GUILayout.Width(50)))
                {
                    Enemies.EnemyActions.KillEnemy(i);
                }
                GUILayout.EndHorizontal();
            }

            if (enemies.Count == 0)
            {
                GUILayout.Label("No enemies found in scene", _labelStyle);
            }
            else if (enemies.Count > 20)
            {
                GUILayout.Label($"... and {enemies.Count - 20} more", _labelStyle);
            }
        }

        private void DrawDebugTab()
        {
            GUILayout.Label("=== Debug Info ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"FPS: {(1f / Time.deltaTime):F1}", _labelStyle);
            GUILayout.Label($"Time Scale: {Time.timeScale}", _labelStyle);
            GUILayout.Label($"Frame: {Time.frameCount}", _labelStyle);
            GUILayout.Label($"Time: {Time.time:F2}s", _labelStyle);

            var hero = Plugin.Hero;
            if (hero != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("=== Hero States ===", _headerStyle);
                GUILayout.Space(5);

                var cs = hero.cState;
                GUILayout.Label($"onGround: {cs.onGround}", _labelStyle);
                GUILayout.Label($"jumping: {cs.jumping}", _labelStyle);
                GUILayout.Label($"falling: {cs.falling}", _labelStyle);
                GUILayout.Label($"dashing: {cs.dashing}", _labelStyle);
                GUILayout.Label($"attacking: {cs.attacking}", _labelStyle);
                GUILayout.Label($"wallSliding: {cs.wallSliding}", _labelStyle);
                GUILayout.Label($"invulnerable: {cs.invulnerable}", _labelStyle);
                GUILayout.Label($"dead: {cs.dead}", _labelStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("=== Mod Info ===", _headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"Version: {PluginInfo.VERSION}", _labelStyle);
            GUILayout.Label($"Author: {PluginInfo.AUTHOR}", _labelStyle);
            GUILayout.Label($"Email: {PluginInfo.EMAIL}", _labelStyle);
            GUILayout.Label($"Telegram: {PluginInfo.TELEGRAM}", _labelStyle);
        }
    }
}
