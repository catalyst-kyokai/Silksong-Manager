using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.SaveState
{
    /// <summary>
    /// Captured state of a SpriteRenderer component.
    /// </summary>
    [System.Serializable]
    public class SpriteRendererData
    {
        /// <summary>Whether the renderer is enabled.</summary>
        public bool Enabled;
        /// <summary>Current tint color.</summary>
        public Color Color;
        /// <summary>Sorting order within the sorting layer.</summary>
        public int SortingOrder;
        /// <summary>Name of the sorting layer.</summary>
        public string SortingLayerName;
        /// <summary>Name of the currently displayed sprite.</summary>
        public string SpriteName;
    }

    /// <summary>
    /// Captured state of an Animator component.
    /// </summary>
    [System.Serializable]
    public class AnimatorData
    {
        /// <summary>Whether the animator is enabled.</summary>
        public bool Enabled;
        /// <summary>Hash of the current animation state.</summary>
        public int StateHash;
        /// <summary>Normalized time (0-1) within the current animation clip.</summary>
        public float NormalizedTime;
        /// <summary>Current playback speed multiplier.</summary>
        public float Speed;
    }

    /// <summary>
    /// Captured state of a Collider2D component.
    /// </summary>
    [System.Serializable]
    public class ColliderData
    {
        /// <summary>Whether the collider is enabled.</summary>
        public bool Enabled;
        /// <summary>Whether the collider is set as a trigger.</summary>
        public bool IsTrigger;
    }

    /// <summary>
    /// Captured local transform data.
    /// </summary>
    [System.Serializable]
    public class TransformData
    {
        /// <summary>Local position relative to parent.</summary>
        public Vector3 LocalPosition;
        /// <summary>Local rotation relative to parent.</summary>
        public Quaternion LocalRotation;
        /// <summary>Local scale.</summary>
        public Vector3 LocalScale;
    }

    /// <summary>
    /// Captured state of a MeshRenderer component.
    /// </summary>
    [System.Serializable]
    public class MeshRendererData
    {
        /// <summary>Whether the renderer is enabled.</summary>
        public bool Enabled;
        /// <summary>Name of the sorting layer.</summary>
        public string SortingLayerName;
        /// <summary>Sorting order within the layer.</summary>
        public int SortingOrder;
    }

    /// <summary>
    /// Captured state of a SkinnedMeshRenderer component.
    /// </summary>
    [System.Serializable]
    public class SkinnedMeshRendererData
    {
        /// <summary>Whether the renderer is enabled.</summary>
        public bool Enabled;
        /// <summary>Name of the sorting layer.</summary>
        public string SortingLayerName;
        /// <summary>Sorting order within the layer.</summary>
        public int SortingOrder;
    }

    /// <summary>
    /// Aggregated component state data for a single GameObject.
    /// Used for detailed restoration of visual and physics state.
    /// </summary>
    [System.Serializable]
    public class ObjectComponentData
    {
        /// <summary>Whether the GameObject was active in hierarchy.</summary>
        public bool IsActive;
        /// <summary>SpriteRenderer state (null if component not present).</summary>
        public SpriteRendererData SpriteRenderer;
        /// <summary>MeshRenderer state (null if component not present).</summary>
        public MeshRendererData MeshRenderer;
        /// <summary>SkinnedMeshRenderer state (null if component not present).</summary>
        public SkinnedMeshRendererData SkinnedMeshRenderer;
        /// <summary>Animator state (null if component not present).</summary>
        public AnimatorData Animator;
        /// <summary>Generic Collider2D state (null if component not present).</summary>
        public ColliderData Collider2D;
        /// <summary>Local transform state.</summary>
        public TransformData Transform;
    }

    /// <summary>
    /// Complete captured state of an enemy entity.
    /// Includes health, physics, FSM state, and visual component data.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    [System.Serializable]
    public class EnemyStateData
    {
        #region Identification

        /// <summary>Name of the enemy GameObject.</summary>
        public string GameObjectName;
        /// <summary>Full hierarchy path for finding the enemy on restore.</summary>
        public string GameObjectPath;
        /// <summary>Whether the enemy GameObject was active when captured.</summary>
        public bool IsActive = true;

        #endregion

        #region HealthManager State

        /// <summary>Current health points.</summary>
        public int HP;
        /// <summary>Whether the enemy is dead.</summary>
        public bool IsDead;
        /// <summary>Whether the enemy is currently invincible.</summary>
        public bool IsInvincible;
        /// <summary>Direction from which the enemy is invincible (0 = none).</summary>
        public int InvincibleFromDirection;
        /// <summary>Whether the enemy recently took a hit.</summary>
        public bool HasHit;

        #endregion

        #region Transform State

        /// <summary>World position.</summary>
        public Vector3 Position;
        /// <summary>World rotation.</summary>
        public Quaternion Rotation;
        /// <summary>Local scale.</summary>
        public Vector3 Scale;

        #endregion

        #region Physics State

        /// <summary>Rigidbody2D velocity.</summary>
        public Vector2 Velocity;
        /// <summary>Rigidbody2D angular velocity.</summary>
        public float AngularVelocity;
        /// <summary>Whether the Rigidbody2D is kinematic.</summary>
        public bool IsKinematic;

        #endregion

        #region Recoil State

        /// <summary>Whether the enemy is currently recoiling from a hit.</summary>
        public bool IsRecoiling;
        /// <summary>Time remaining in recoil state.</summary>
        public float RecoilTimeRemaining;
        /// <summary>Direction of recoil.</summary>
        public int RecoilDirection;

        #endregion

        #region FSM States

        /// <summary>List of all FSM states on this enemy (Control, Attack, etc.).</summary>
        public List<FsmStateData> FsmStates = new List<FsmStateData>();

        #endregion

        #region Component States

        /// <summary>Component state data for the main enemy GameObject.</summary>
        public ObjectComponentData MainObjectState;

        /// <summary>Component state data for child GameObjects (key: relative hierarchy path).</summary>
        public Dictionary<string, ObjectComponentData> ChildStates = new Dictionary<string, ObjectComponentData>();

        /// <summary>Legacy child active states (deprecated, kept for save file compatibility).</summary>
        public Dictionary<string, bool> ChildObjectStates = new Dictionary<string, bool>();

        #endregion
    }
}
