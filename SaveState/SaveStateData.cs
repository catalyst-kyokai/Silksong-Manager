using System;
using UnityEngine;

namespace SilksongManager.SaveState
{
    /// <summary>
    /// Represents a complete save state snapshot including player data, world state, and enemy positions.
    /// Used for save state functionality allowing instant game state restoration during debugging.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    [Serializable]
    public class SaveStateData
    {
        /// <summary>Optional custom name for this save state.</summary>
        public string SaveName;
        /// <summary>Timestamp when this save state was created.</summary>
        public string Timestamp;
        /// <summary>Name of the scene where this save state was captured.</summary>
        public string SceneName;

        /// <summary>JSON-serialized PlayerData containing inventory, story progress, and player stats.</summary>
        public string PlayerDataJson;
        /// <summary>JSON-serialized SceneData containing semi-persistent world state flags.</summary>
        public string SceneDataJson;

        /// <summary>Player's world position when save state was captured.</summary>
        public Vector3 Position;
        /// <summary>Player's velocity when save state was captured.</summary>
        public Vector2 Velocity;
        /// <summary>Whether player was facing right when save state was captured.</summary>
        public bool FacingRight;
        /// <summary>Whether player was on the ground when save state was captured.</summary>
        public bool IsGrounded;

        /// <summary>Player's current health at time of capture (redundant copy for UI display).</summary>
        public int Health;
        /// <summary>Player's maximum health at time of capture.</summary>
        public int MaxHealth;
        /// <summary>Player's current silk at time of capture.</summary>
        public int Silk;
        /// <summary>Player's maximum silk at time of capture.</summary>
        public int MaxSilk;
        /// <summary>Player's geo currency at time of capture.</summary>
        public int Geo;

        /// <summary>List of enemy states captured for restoring enemy positions and HP.</summary>
        public System.Collections.Generic.List<EnemyStateData> EnemyStates;

        /// <summary>Battle scene state if a battle arena was active during capture.</summary>
        public BattleSceneStateData BattleSceneState;
        /// <summary>Boss scene state if a boss fight was active during capture.</summary>
        public BossSceneStateData BossSceneState;

        /// <summary>
        /// Gets a display-friendly name for this save state.
        /// Returns the custom name if set, otherwise returns "SceneName - Timestamp".
        /// </summary>
        /// <returns>Human-readable name for this save state.</returns>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(SaveName))
                return $"{SceneName} - {Timestamp}";
            return SaveName;
        }
    }
}
