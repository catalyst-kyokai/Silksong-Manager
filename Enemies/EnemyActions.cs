using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.Enemies
{
    /// <summary>
    /// Actions related to enemy management.
    /// </summary>
    public static class EnemyActions
    {
        /// <summary>
        /// Find all enemies in current scene.
        /// </summary>
        public static List<EnemyInfo> FindAllEnemies()
        {
            var result = new List<EnemyInfo>();

            var healthManagers = Object.FindObjectsOfType<HealthManager>();
            foreach (var hm in healthManagers)
            {
                if (hm != null && hm.gameObject != null)
                {
                    result.Add(new EnemyInfo
                    {
                        Name = hm.gameObject.name,
                        Position = hm.transform.position,
                        CurrentHP = hm.hp,
                        MaxHP = hm.hp, // HealthManager doesn't expose max HP directly
                        IsAlive = !hm.GetIsDead(),
                        GameObject = hm.gameObject
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Kill all enemies in current scene.
        /// </summary>
        public static void KillAllEnemies()
        {
            var healthManagers = Object.FindObjectsOfType<HealthManager>();
            int count = 0;

            foreach (var hm in healthManagers)
            {
                if (hm != null && !hm.GetIsDead())
                {
                    hm.Die(0f, AttackTypes.Generic, false);
                    count++;
                }
            }

            Plugin.Log.LogInfo($"Killed {count} enemies.");
        }

        /// <summary>
        /// Damage all enemies.
        /// </summary>
        public static void DamageAllEnemies(int damage)
        {
            var healthManagers = Object.FindObjectsOfType<HealthManager>();

            foreach (var hm in healthManagers)
            {
                if (hm != null && !hm.GetIsDead())
                {
                    hm.ApplyExtraDamage(damage);
                }
            }

            Plugin.Log.LogInfo($"Dealt {damage} damage to all enemies.");
        }

        /// <summary>
        /// Freeze all enemies.
        /// </summary>
        public static void FreezeAllEnemies()
        {
            var enemies = Object.FindObjectsOfType<HealthManager>();

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    var animators = enemy.GetComponentsInChildren<Animator>();
                    foreach (var anim in animators)
                    {
                        anim.speed = 0f;
                    }

                    var rbs = enemy.GetComponentsInChildren<Rigidbody2D>();
                    foreach (var rb in rbs)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.simulated = false;
                    }
                }
            }

            Plugin.Log.LogInfo("Froze all enemies.");
        }

        /// <summary>
        /// Unfreeze all enemies.
        /// </summary>
        public static void UnfreezeAllEnemies()
        {
            var enemies = Object.FindObjectsOfType<HealthManager>();

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    var animators = enemy.GetComponentsInChildren<Animator>();
                    foreach (var anim in animators)
                    {
                        anim.speed = 1f;
                    }

                    var rbs = enemy.GetComponentsInChildren<Rigidbody2D>();
                    foreach (var rb in rbs)
                    {
                        rb.simulated = true;
                    }
                }
            }

            Plugin.Log.LogInfo("Unfroze all enemies.");
        }

        /// <summary>
        /// Kill specific enemy by index.
        /// </summary>
        public static void KillEnemy(int index)
        {
            var enemies = FindAllEnemies();
            if (index >= 0 && index < enemies.Count)
            {
                var hm = enemies[index].GameObject.GetComponent<HealthManager>();
                if (hm != null)
                {
                    hm.Die(0f, AttackTypes.Generic, false);
                    Plugin.Log.LogInfo($"Killed enemy: {enemies[index].Name}");
                }
            }
        }
    }

    /// <summary>
    /// Enemy information.
    /// </summary>
    public struct EnemyInfo
    {
        public string Name;
        public Vector3 Position;
        public int CurrentHP;
        public int MaxHP;
        public bool IsAlive;
        public GameObject GameObject;
    }
}
