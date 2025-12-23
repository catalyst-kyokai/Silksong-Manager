namespace SilksongManager.SaveState
{
    /// <summary>
    /// Represents the captured state of a battle arena (wave-based combat encounter).
    /// Used to restore battle progress when loading a save state.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    [System.Serializable]
    public class BattleSceneStateData
    {
        /// <summary>Hierarchy path to the BattleScene GameObject for identification.</summary>
        public string GameObjectPath;
        /// <summary>Current wave number in the battle sequence.</summary>
        public int CurrentWave;
        /// <summary>Number of enemies currently alive in the arena.</summary>
        public int CurrentEnemies;
        /// <summary>Number of enemies remaining before the next wave triggers.</summary>
        public int EnemiesToNext;
        /// <summary>Whether the battle has started (gates closed, enemies spawning).</summary>
        public bool Started;
        /// <summary>Whether the battle has been completed (all waves cleared).</summary>
        public bool Completed;
        /// <summary>FSM state for the battle scene logic (gate handling, wave progression).</summary>
        public FsmStateData LogicFsmState;
    }
}
