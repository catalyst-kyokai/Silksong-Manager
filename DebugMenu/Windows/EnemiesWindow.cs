using UnityEngine;
using SilksongManager.Menu.Keybinds;
using System.Collections.Generic;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Enemies manipulation window with enemy list.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class EnemiesWindow : BaseWindow
    {
        public override int WindowId => 10004;
        public override string Title => "Enemies";
        protected override Vector2 DefaultSize => new Vector2(300, 400);

        private Vector2 _enemyListScroll;
        private List<Enemies.EnemyInfo> _cachedEnemies;
        private float _cacheTimer = 0f;
        private const float CACHE_INTERVAL = 0.5f;

        public override void Update()
        {
            base.Update();

            // Cache enemies periodically for performance
            _cacheTimer += Time.unscaledDeltaTime;
            if (_cacheTimer >= CACHE_INTERVAL)
            {
                _cachedEnemies = Enemies.EnemyActions.FindAllEnemies();
                _cacheTimer = 0f;
            }
        }

        protected override void DrawContent()
        {
            // Quick actions
            DebugMenuStyles.DrawSectionHeader("ACTIONS");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill All", DebugMenuStyles.Button))
            {
                Enemies.EnemyActions.KillAllEnemies();
            }
            DrawKeybindHint(ModAction.KillAllEnemies);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool isFrozen = Enemies.EnemyActions.AreEnemiesFrozen;
            if (DebugMenuStyles.DrawToggleButton(isFrozen ? "Freeze ✓" : "Freeze", isFrozen))
            {
                Enemies.EnemyActions.FreezeEnemies(!isFrozen);
            }
            DrawKeybindHint(ModAction.FreezeEnemies);
            GUILayout.EndHorizontal();

            // Stats
            DebugMenuStyles.DrawSectionHeader("STATS");

            int enemyCount = _cachedEnemies?.Count ?? 0;
            GUILayout.Label($"Enemies in room: {enemyCount}", DebugMenuStyles.Label);
            DebugMenuStyles.DrawStatus("Frozen", isFrozen);

            // Enemy List
            DebugMenuStyles.DrawSectionHeader("ENEMY LIST");

            if (_cachedEnemies == null || _cachedEnemies.Count == 0)
            {
                GUILayout.Label("No enemies in room", DebugMenuStyles.LabelCentered);
            }
            else
            {
                _enemyListScroll = GUILayout.BeginScrollView(_enemyListScroll, GUILayout.Height(150));

                for (int i = 0; i < _cachedEnemies.Count; i++)
                {
                    var enemy = _cachedEnemies[i];
                    if (!enemy.IsAlive) continue;

                    GUILayout.BeginHorizontal();

                    // Enemy name (truncated if too long)
                    string name = enemy.Name;
                    if (name.Length > 18) name = name.Substring(0, 15) + "...";

                    GUILayout.Label(name, DebugMenuStyles.Label, GUILayout.Width(140));
                    GUILayout.Label($"HP:{enemy.CurrentHP}", DebugMenuStyles.Label, GUILayout.Width(60));

                    if (GUILayout.Button("Kill", DebugMenuStyles.ButtonSmall, GUILayout.Width(40)))
                    {
                        Enemies.EnemyActions.KillEnemy(i);
                        _cachedEnemies = Enemies.EnemyActions.FindAllEnemies(); // Refresh
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            // Refresh button
            if (GUILayout.Button("Refresh List", DebugMenuStyles.Button))
            {
                _cachedEnemies = Enemies.EnemyActions.FindAllEnemies();
            }
        }

        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(50));
            }
        }
    }
}

