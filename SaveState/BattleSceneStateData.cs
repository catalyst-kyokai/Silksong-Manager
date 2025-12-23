using UnityEngine;

namespace SilksongManager.SaveState
{
    [System.Serializable]
    public class BattleSceneStateData
    {
        public string GameObjectPath;
        public int CurrentWave;
        public int CurrentEnemies;
        public int EnemiesToNext;
        public bool Started;
        public bool Completed;
        // Potential FSM state for the battle scene itself (e.g. gate handling)
        public FsmStateData LogicFsmState;
    }
}
