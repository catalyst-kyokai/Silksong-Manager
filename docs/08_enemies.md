# Документация: Враги (Enemies)

## Обзор

Система врагов в Silksong включает:

- **HealthManager** - управление здоровьем врагов
- **EnemyDeathEffects** - эффекты смерти
- **EnemyHitEffects** - эффекты попадания
- **DamageEnemies** - система урона

## HealthManager (HealthManager.cs)

Файл: `silksong_source_code/HealthManager.cs` (~2500 строк)

Главный компонент здоровья для врагов.

### Основные поля

```csharp
public class HealthManager : MonoBehaviour
{
    public int hp;                    // Текущее здоровье
    public int geo;                   // Выпадающий Geo
    public bool isDead;               // Мёртв ли враг
    public bool IsInvincible;         // Неуязвим
    public GameObject corpse;         // Префаб трупа
    
    // Настройки урона
    public bool damagesEnemy = true;
    public int damageOverride;
    
    // Эффекты
    public EnemyDeathEffects deathEffects;
    public EnemyHitEffects hitEffects;
}
```

### Основные методы

| Метод | Описание |
|-------|----------|
| `Hit(HitInstance hitInstance)` | Получить удар |
| `TakeDamage(HitInstance hitInstance)` | Получить урон |
| `Die(float? attackDirection, AttackTypes attackType, bool ignoreEvasion)` | Умереть |
| `Invincible(HitInstance hitInstance)` | Проверка неуязвимости |
| `IsBlockingByDirection(float attackDirection)` | Блокирует ли удар |
| `ApplyExtraDamage(int damage)` | Нанести дополнительный урон |
| `SetDead()` | Установить как мёртвого |
| `GetIsDead()` | Проверить мертв ли |

### Перечисление активных врагов

```csharp
public static IEnumerable<HealthManager> EnumerateActiveEnemies()
{
    return Object.FindObjectsOfType<HealthManager>()
        .Where(hm => hm != null && !hm.isDead);
}
```

## HitInstance (HitInstance.cs)

Данные о попадании.

```csharp
public class HitInstance
{
    public GameObject Source;         // Источник удара
    public AttackTypes AttackType;    // Тип атаки
    public int DamageDealt;           // Нанесённый урон
    public float Direction;           // Направление
    public float MagnitudeMultiplier; // Множитель силы
    public float MoveDirection;       // Направление отталкивания
    public bool IsExtraDamage;        // Дополнительный урон
    public bool CircleDirection;      // Круговое направление
    public bool IgnoreInvulnerable;   // Игнорировать неуязвимость
    public bool DoesBlockerDamage;    // Урон блокеру
}
```

## AttackTypes (enum)

```csharp
public enum AttackTypes
{
    Generic,        // Обычная
    Nail,           // Гвоздь
    Spell,          // Заклинание
    Acid,           // Кислота
    Fire,           // Огонь
    SharpShadow,    // Острая тень
    RuinousDash,    // Разрушительный рывок
    Ranged,         // Дальняя
    Crest,          // Герб
    Tool            // Инструмент
}
```

## EnemyDeathEffects (EnemyDeathEffects.cs)

Эффекты смерти врага.

```csharp
public class EnemyDeathEffects : MonoBehaviour
{
    public GameObject corpse;
    public GameObject deathEffect;
    public AudioClip deathSound;
    
    public void EmitEffects();
    public void EmitCorpse(float? direction, bool spawnGeo);
    public void EmitSound();
}
```

### EnemyDeathEffectsProfile

```csharp
public class EnemyDeathEffectsProfile : ScriptableObject
{
    public GameObject deathParticles;
    public AudioClip[] deathSounds;
    public Color bloodColor;
}
```

## EnemyHitEffects

Эффекты при попадании по врагу.

```csharp
public class EnemyHitEffects : MonoBehaviour
{
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    public void EmitEffects(float direction);
}
```

### EnemyHitEffectsProfile

```csharp
public class EnemyHitEffectsProfile : ScriptableObject
{
    public GameObject hitParticles;
    public AudioClip[] hitSounds;
    public bool slashAble;
    public bool spikeable;
}
```

## DamageEnemies (DamageEnemies.cs)

Компонент, наносящий урон врагам.

```csharp
public class DamageEnemies : MonoBehaviour
{
    public int damageDealt;
    public AttackTypes attackType;
    public float direction;
    public bool specifyDirection;
    
    public void OnTriggerEnter2D(Collider2D collision);
    public void DealDamage(HealthManager hm);
}
```

## EnemySpawner (EnemySpawner.cs)

Спаунер врагов.

```csharp
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int maxEnemies;
    public float spawnInterval;
    
    public void Spawn();
    public void SpawnAll();
    public void DespawnAll();
}
```

## Примеры использования

### Найти всех врагов

```csharp
public List<EnemyInfo> FindAllEnemies()
{
    var enemies = Object.FindObjectsOfType<HealthManager>();
    
    return enemies.Select(e => new EnemyInfo
    {
        Name = e.gameObject.name,
        Position = e.transform.position,
        HP = e.hp,
        IsAlive = !e.GetIsDead()
    }).ToList();
}
```

### Убить всех врагов

```csharp
public void KillAllEnemies()
{
    var enemies = Object.FindObjectsOfType<HealthManager>();
    
    foreach (var enemy in enemies)
    {
        if (!enemy.GetIsDead())
        {
            enemy.Die(0f, AttackTypes.Generic, false);
        }
    }
}
```

### Нанести урон врагу

```csharp
public void DamageEnemy(HealthManager enemy, int damage)
{
    if (enemy == null || enemy.GetIsDead()) return;
    
    enemy.ApplyExtraDamage(damage);
}
```

### Заморозить врагов

```csharp
public void FreezeEnemies(bool freeze)
{
    var enemies = Object.FindObjectsOfType<HealthManager>();
    
    foreach (var enemy in enemies)
    {
        var animator = enemy.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = freeze ? 0f : 1f;
        }
        
        var rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = !freeze;
        }
    }
}
```

## Журнал врагов (Enemy Journal)

### EnemyJournalRecord

```csharp
public class EnemyJournalRecord
{
    public string enemyName;
    public int killCount;
    public bool isUnlocked;
    public Sprite portrait;
    public string description;
}
```

### EnemyJournalManager

```csharp
public class EnemyJournalManager
{
    public static EnemyJournalManager Instance;
    
    public void RecordKill(string enemyName);
    public EnemyJournalRecord GetRecord(string enemyName);
    public List<EnemyJournalRecord> GetAllRecords();
}
```

## Связанные файлы

- `HealthManager.cs` - Менеджер здоровья
- `HitInstance.cs` - Данные попадания
- `EnemyDeathEffects.cs` - Эффекты смерти
- `EnemyHitEffects*.cs` - Эффекты попадания
- `DamageEnemies.cs` - Нанесение урона
- `EnemySpawner.cs` - Спаунер
- `EnemyJournalRecord.cs` - Запись журнала
- `EnemyJournalManager.cs` - Менеджер журнала
- `AttackTypes.cs` - Типы атак
