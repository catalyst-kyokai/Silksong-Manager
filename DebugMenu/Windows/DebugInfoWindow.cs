using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Debug information window showing FPS, game stats, player info.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class DebugInfoWindow : BaseWindow
    {
        public override int WindowId => 10008;
        public override string Title => "Debug Info";
        protected override Vector2 DefaultSize => new Vector2(320, 450);

        // FPS calculation
        private float _deltaTime = 0f;
        private float _fps = 0f;
        private float _fpsUpdateTimer = 0f;

        // Frame time tracking
        private float _minFrameTime = float.MaxValue;
        private float _maxFrameTime = 0f;
        private float _avgFrameTime = 0f;
        private int _frameCount = 0;
        private float _frameTimeSum = 0f;

        public override void Update()
        {
            base.Update();

            // FPS calculation
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            _fpsUpdateTimer += Time.unscaledDeltaTime;
            if (_fpsUpdateTimer >= 0.5f)
            {
                _fps = 1.0f / _deltaTime;
                _fpsUpdateTimer = 0f;
            }

            // Frame time tracking
            float frameTime = Time.unscaledDeltaTime * 1000f; // ms
            _minFrameTime = Mathf.Min(_minFrameTime, frameTime);
            _maxFrameTime = Mathf.Max(_maxFrameTime, frameTime);
            _frameTimeSum += frameTime;
            _frameCount++;
            _avgFrameTime = _frameTimeSum / _frameCount;

            // Reset stats periodically
            if (_frameCount > 300)
            {
                _minFrameTime = frameTime;
                _maxFrameTime = frameTime;
                _frameTimeSum = frameTime;
                _frameCount = 1;
            }
        }

        protected override void DrawContent()
        {
            // Performance section
            DrawPerformanceSection();

            // Game State section
            DrawGameStateSection();

            // Player section
            DrawPlayerSection();

            // Position section
            DrawPositionSection();

            // Scene section
            DrawSceneSection();

            // Input section
            DrawInputSection();
        }

        private void DrawPerformanceSection()
        {
            DebugMenuStyles.DrawSectionHeader("PERFORMANCE");

            // FPS with color coding
            Color fpsColor = _fps >= 60 ? DebugMenuStyles.StatusOn :
                             _fps >= 30 ? DebugMenuStyles.StatusWarning :
                             DebugMenuStyles.StatusOff;

            GUILayout.BeginHorizontal();
            GUILayout.Label("FPS:", DebugMenuStyles.Label, GUILayout.Width(80));
            var oldColor = GUI.color;
            GUI.color = fpsColor;
            GUILayout.Label($"{_fps:F1}", DebugMenuStyles.LabelBold);
            GUI.color = oldColor;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Frame Time: {_deltaTime * 1000f:F2} ms", DebugMenuStyles.Label);
            GUILayout.Label($"Min/Avg/Max: {_minFrameTime:F1}/{_avgFrameTime:F1}/{_maxFrameTime:F1} ms", DebugMenuStyles.Label);

            // Memory (approximate)
            float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            GUILayout.Label($"Memory (GC): {memoryMB:F1} MB", DebugMenuStyles.Label);
        }

        private void DrawGameStateSection()
        {
            DebugMenuStyles.DrawSectionHeader("GAME STATE");

            var gm = Plugin.GM;

            GUILayout.Label($"Game Speed: {Time.timeScale:F2}x", DebugMenuStyles.Label);
            GUILayout.Label($"Time: {Time.time:F1}s (unscaled: {Time.unscaledTime:F1}s)", DebugMenuStyles.Label);

            if (gm != null)
            {
                GUILayout.Label($"Scene: {gm.sceneName}", DebugMenuStyles.Label);
                GUILayout.Label($"Paused: {(Time.timeScale < 0.01f ? "Yes" : "No")}", DebugMenuStyles.Label);
            }
            else
            {
                GUILayout.Label("GameManager: N/A", DebugMenuStyles.Label);
            }
        }

        private void DrawPlayerSection()
        {
            DebugMenuStyles.DrawSectionHeader("PLAYER");

            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd == null || hero == null)
            {
                GUILayout.Label("Not in game", DebugMenuStyles.Label);
                return;
            }

            // Health & Silk
            GUILayout.Label($"Health: {pd.health}/{pd.maxHealth}", DebugMenuStyles.Label);
            GUILayout.Label($"Silk: {pd.silk}/{pd.silkMax}", DebugMenuStyles.Label);
            GUILayout.Label($"Geo: {pd.geo}", DebugMenuStyles.Label);

            // States
            GUILayout.Space(4);
            GUILayout.Label($"Invincible: {(pd.isInvincible ? "ON" : "OFF")}", DebugMenuStyles.Label);
            GUILayout.Label($"Noclip: {(Player.PlayerActions.IsNoclipEnabled ? "ON" : "OFF")}", DebugMenuStyles.Label);

            // Hero states
            if (hero.cState != null)
            {
                GUILayout.Label($"Grounded: {(hero.cState.onGround ? "Yes" : "No")}", DebugMenuStyles.Label);
                GUILayout.Label($"Dashing: {(hero.cState.dashing ? "Yes" : "No")}", DebugMenuStyles.Label);
                GUILayout.Label($"Attacking: {(hero.cState.attacking ? "Yes" : "No")}", DebugMenuStyles.Label);
            }
        }

        private void DrawPositionSection()
        {
            DebugMenuStyles.DrawSectionHeader("POSITION");

            var hero = Plugin.Hero;
            if (hero == null)
            {
                GUILayout.Label("N/A", DebugMenuStyles.Label);
                return;
            }

            var pos = hero.transform.position;
            GUILayout.Label($"X: {pos.x:F3}", DebugMenuStyles.Label);
            GUILayout.Label($"Y: {pos.y:F3}", DebugMenuStyles.Label);
            GUILayout.Label($"Z: {pos.z:F3}", DebugMenuStyles.Label);

            // Velocity
            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var vel = rb.linearVelocity;
                GUILayout.Label($"Velocity: ({vel.x:F1}, {vel.y:F1})", DebugMenuStyles.Label);
                GUILayout.Label($"Speed: {vel.magnitude:F1}", DebugMenuStyles.Label);
            }
        }

        private void DrawSceneSection()
        {
            DebugMenuStyles.DrawSectionHeader("SCENE");

            var scene = SceneManager.GetActiveScene();
            GUILayout.Label($"Name: {scene.name}", DebugMenuStyles.Label);
            GUILayout.Label($"Build Index: {scene.buildIndex}", DebugMenuStyles.Label);
            GUILayout.Label($"Objects: {scene.rootCount} root", DebugMenuStyles.Label);

            // Enemy count
            int enemyCount = Enemies.EnemyActions.GetEnemyCount();
            GUILayout.Label($"Enemies: {enemyCount}", DebugMenuStyles.Label);
        }

        private void DrawInputSection()
        {
            DebugMenuStyles.DrawSectionHeader("INPUT");

            var mousePos = Input.mousePosition;
            GUILayout.Label($"Mouse: ({mousePos.x:F0}, {mousePos.y:F0})", DebugMenuStyles.Label);

            // Current pressed keys
            string keys = "";
            if (Input.GetKey(KeyCode.W)) keys += "W ";
            if (Input.GetKey(KeyCode.A)) keys += "A ";
            if (Input.GetKey(KeyCode.S)) keys += "S ";
            if (Input.GetKey(KeyCode.D)) keys += "D ";
            if (Input.GetKey(KeyCode.Space)) keys += "Space ";
            if (Input.GetKey(KeyCode.LeftShift)) keys += "Shift ";

            GUILayout.Label($"Keys: {(string.IsNullOrEmpty(keys) ? "None" : keys)}", DebugMenuStyles.Label);
        }
    }
}
