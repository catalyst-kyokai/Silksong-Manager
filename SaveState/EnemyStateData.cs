using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.SaveState
{
    [System.Serializable]
    public class SpriteRendererData
    {
        public bool Enabled;
        public Color Color;
        public int SortingOrder;
        public string SortingLayerName;
        public string SpriteName;
    }

    [System.Serializable]
    public class AnimatorData
    {
        public bool Enabled;
        public int StateHash;
        public float NormalizedTime;
        public float Speed;
    }

    [System.Serializable]
    public class ColliderData
    {
        public bool Enabled;
        public bool IsTrigger;
    }

    [System.Serializable]
    public class ObjectComponentData
    {
        public bool IsActive; // GameObject active state
        public SpriteRendererData SpriteRenderer;
        public AnimatorData Animator;
        public ColliderData Collider2D; // Generic for any Collider2D
    }

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

        // Component states (Main object)
        public ObjectComponentData MainObjectState;

        // Child object states (for restoring visual state, cocoons, effects, etc.)
        // Key is child name
        public Dictionary<string, ObjectComponentData> ChildStates = new Dictionary<string, ObjectComponentData>();

        // Deprecated but kept for compatibility during migration if needed
        public Dictionary<string, bool> ChildObjectStates = new Dictionary<string, bool>();
    }
}
