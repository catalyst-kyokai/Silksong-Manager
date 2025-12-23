using UnityEngine;

namespace SilksongManager.SaveState
{
    [System.Serializable]
    public class BossSceneStateData
    {
        public bool IsActive; // Only if BossSceneController exists
        public int BossLevel; // 'bossLevel' field
        public bool HasTransitionedIn;
        public int BossesLeft;
        // BossSceneController often has an FSM too
        public FsmStateData ControllerFsmState;
    }
}
