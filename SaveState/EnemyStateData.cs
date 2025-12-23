using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.SaveState
{
    [System.Serializable]
    public class EnemyStateData
    {
        // Identification
        public string GameObjectName;
        public string GameObjectPath; // Hierarchy path for finding

        // HealthManager state
        public int HP;
        public bool IsDead;
        public bool IsInvincible;
        public int InvincibleFromDirection;
        public bool HasHit; // Often used in HealthManager to track if it recently took a hit

        // Transform state
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        // Physics state
        public Vector2 Velocity;
        public float AngularVelocity;
        public bool IsKinematic;

        // Recoil state
        public bool IsRecoiling;
        public float RecoilTimeRemaining;
        public int RecoilDirection; // From research, direction is often passed

        // FSM states - list of all FSMs on this enemy
        public List<FsmStateData> FsmStates = new List<FsmStateData>();

        // Child object active states - for restoring visual state (cocoons, effects, etc.)
        public Dictionary<string, bool> ChildObjectStates = new Dictionary<string, bool>();
    }
}
