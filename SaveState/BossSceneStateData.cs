namespace SilksongManager.SaveState
{
    /// <summary>
    /// Represents the captured state of a boss fight scene.
    /// Used to restore boss encounter progress when loading a save state.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    [System.Serializable]
    public class BossSceneStateData
    {
        /// <summary>Whether a BossSceneController was active during capture.</summary>
        public bool IsActive;
        /// <summary>Current boss difficulty/phase level.</summary>
        public int BossLevel;
        /// <summary>Whether the boss has completed its entrance transition.</summary>
        public bool HasTransitionedIn;
        /// <summary>Number of bosses remaining in multi-boss encounters.</summary>
        public int BossesLeft;
        /// <summary>FSM state for the boss scene controller logic.</summary>
        public FsmStateData ControllerFsmState;
    }
}
