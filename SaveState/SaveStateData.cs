using System;
using UnityEngine;

namespace SilksongManager.SaveState
{
    [Serializable]
    public class SaveStateData
    {
        public string SaveName;
        public string Timestamp;
        public string SceneName;

        // Serialized PlayerData (contains inventory, story progress, etc.)
        public string PlayerDataJson;
        // Serialized SceneData (contains semi-persistent world flags)
        public string SceneDataJson;

        // Hero State
        public Vector3 Position;
        public Vector2 Velocity;
        public bool FacingRight;
        public bool IsGrounded;
        public int Health;
        public int MaxHealth;
        public int Silk;
        public int MaxSilk;
        public int Geo;

        // Metadata
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(SaveName))
                return $"{SceneName} - {Timestamp}";
            return SaveName;
        }
    }
}
