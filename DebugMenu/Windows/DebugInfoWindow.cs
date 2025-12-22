using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Debug information window showing FPS, game stats, player info.
    /// Optimized with caching to prevent FPS drops.
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

        // Cached values (updated once per second to avoid FPS drops)
        private const float CACHE_REFRESH_INTERVAL = 1f;
        private float _cacheTimer = 0f;

        private int _cachedEnemyCount = 0;
        private float _cachedMemory = 0f;
        private Rigidbody2D _cachedRigidbody = null;
        private bool _rigidbodyCached = false;

        public override void Update()
        {
            base.Update();

            // FPS calculation (lightweight, ok every frame)
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            _fpsUpdateTimer += Time.unscaledDeltaTime;
            if (_fpsUpdateTimer >= 0.5f)
            {
                _fps = 1.0f / _deltaTime;
                _fpsUpdateTimer = 0f;
            }

            // Frame time tracking (lightweight)
            float frameTime = Time.unscaledDeltaTime * 1000f;
            _minFrameTime = Mathf.Min(_minFrameTime, frameTime);
            _maxFrameTime = Mathf.Max(_maxFrameTime, frameTime);
            _frameTimeSum += frameTime;
            _frameCount++;
            _avgFrameTime = _frameTimeSum / _frameCount;

            if (_frameCount > 300)
            {
                _minFrameTime = frameTime;
                _maxFrameTime = frameTime;
                _frameTimeSum = frameTime;
                _frameCount = 1;
            }

            // Update expensive cached values periodically
            _cacheTimer += Time.unscaledDeltaTime;
            if (_cacheTimer >= CACHE_REFRESH_INTERVAL)
            {
                RefreshCachedValues();
                _cacheTimer = 0f;
            }
        }

        private void RefreshCachedValues()
        {
            // These are expensive operations - only do once per second
            _cachedEnemyCount = Enemies.EnemyActions.GetEnemyCount();
            _cachedMemory = System.GC.GetTotalMemory(false) / (1024f * 1024f);

            // Cache rigidbody reference
            if (!_rigidbodyCached || _cachedRigidbody == null)
            {
                var hero = Plugin.Hero;
                if (hero != null)
                {
                    _cachedRigidbody = hero.GetComponent<Rigidbody2D>();
                    _rigidbodyCached = true;
                }
            }
        }

        protected override void DrawContent()
        {
            DrawPerformanceSection();
            DrawGameStateSection();
            DrawPlayerSection();
            DrawPositionSection();
            DrawSceneSection();
            DrawInputSection();
        }

        private void DrawPerformanceSection()
        {
            DebugMenuStyles.DrawSectionHeader("PERFORMANCE");

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
            GUILayout.Label($"Memory (GC): {_cachedMemory:F1} MB", DebugMenuStyles.Label);
        }

        private void DrawGameStateSection()
        {
            DebugMenuStyles.DrawSectionHeader("GAME STATE");

            var gm = Plugin.GM;

            GUILayout.Label($"Game Speed: {Time.timeScale:F2}x", DebugMenuStyles.Label);
            GUILayout.Label($"Time: {Time.time:F1}s", DebugMenuStyles.Label);

            if (gm != null)
            {
                GUILayout.Label($"Scene: {gm.sceneName}", DebugMenuStyles.Label);
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

            GUILayout.Label($"Health: {pd.health}/{pd.maxHealth}", DebugMenuStyles.Label);
            GUILayout.Label($"Silk: {pd.silk}/{pd.silkMax}", DebugMenuStyles.Label);
            GUILayout.Label($"Geo: {pd.geo}", DebugMenuStyles.Label);
            GUILayout.Label($"Invincible: {(pd.isInvincible ? "ON" : "OFF")}", DebugMenuStyles.Label);
            GUILayout.Label($"Noclip: {(Player.PlayerActions.IsNoclipEnabled ? "ON" : "OFF")}", DebugMenuStyles.Label);

            if (hero.cState != null)
            {
                GUILayout.Label($"Grounded: {(hero.cState.onGround ? "Yes" : "No")}", DebugMenuStyles.Label);
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
            GUILayout.Label($"X: {pos.x:F2}  Y: {pos.y:F2}", DebugMenuStyles.Label);

            // Use cached rigidbody
            if (_cachedRigidbody != null)
            {
                var vel = _cachedRigidbody.linearVelocity;
                GUILayout.Label($"Speed: {vel.magnitude:F1}", DebugMenuStyles.Label);
            }
        }

        private void DrawSceneSection()
        {
            DebugMenuStyles.DrawSectionHeader("SCENE");

            var scene = SceneManager.GetActiveScene();
            GUILayout.Label($"Name: {scene.name}", DebugMenuStyles.Label);
            GUILayout.Label($"Enemies: {_cachedEnemyCount}", DebugMenuStyles.Label);
        }

        private void DrawInputSection()
        {
            DebugMenuStyles.DrawSectionHeader("INPUT");

            var mousePos = Input.mousePosition;
            GUILayout.Label($"Mouse: ({mousePos.x:F0}, {mousePos.y:F0})", DebugMenuStyles.Label);
        }
    }
}

