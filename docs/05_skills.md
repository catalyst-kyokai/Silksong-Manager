# Документация: Скиллы и Способности (Skills)

## Обзор

Система способностей в Silksong включает:

- **Базовые способности движения** - прыжки, рывки, карабканье
- **Боевые способности** - атаки, парирование
- **Инструменты** - активные предметы
- **Гербы (Crests)** - пассивные бонусы

## Базовые способности (PlayerData)

### Способности движения

```csharp
public class PlayerData
{
    // Двойной прыжок
    public bool hasDoubleJump;
    public int doubleJumpLevel;
    
    // Рывок
    public bool hasDash;
    public int dashLevel;
    
    // Прыжок от стены
    public bool hasWallJump;
    
    // Супер-рывок
    public bool hasSuperDash;
    
    // Карабканье по стенам
    public bool hasWallClimb;
    
    // Плавание
    public bool canSwim;
}
```

### Боевые способности

```csharp
public class PlayerData
{
    // Уровень гвоздя
    public int nailDamage;
    public int nailLevel;
    
    // Парирование
    public bool hasParry;
    
    // Заряженная атака
    public bool hasChargeAttack;
    
    // Атака вниз
    public bool hasDownAttack;
}
```

## HeroController - методы способностей

### Использование способностей

```csharp
public class HeroController
{
    // Рывок
    public void StartDash();
    public void EndDash();
    public bool CanDash();
    
    // Прыжок
    public void Jump();
    public void DoubleJump();
    public bool CanJump();
    public bool CanDoubleJump();
    
    // Атака
    public void Attack();
    public void ChargeAttack();
    public void DownAttack();
    
    // Инструменты
    public void ThrowTool();
    public bool CanThrowTool();
    public void SetToolCooldown(float time);
}
```

### Проверки состояний

```csharp
// Может ли использовать рывок
public bool CanDash()
{
    var pd = PlayerData.instance;
    var hero = HeroController.instance;
    
    return pd.hasDash && 
           !hero.cState.dashing && 
           !hero.cState.dashCooldown;
}

// Может ли использовать двойной прыжок
public bool CanDoubleJump()
{
    var pd = PlayerData.instance;
    var hero = HeroController.instance;
    
    return pd.hasDoubleJump && 
           !hero.cState.onGround && 
           !hero.cState.doubleJumping;
}
```

## Система инструментов (Tools)

### ToolItem

```csharp
public class ToolItem : ScriptableObject
{
    public string toolName;
    public ToolItemType itemType;
    public int maxAmount;
    public float cooldown;
    public Sprite icon;
    
    public bool IsUnlocked;
    public int CurrentAmount;
    
    // Использование
    public virtual void Use();
    public virtual bool CanUse();
    public virtual void Replenish(int amount);
}
```

### ToolItemStates

```csharp
public class ToolItemStates
{
    public bool isActive;
    public bool isOnCooldown;
    public float cooldownRemaining;
    public int amountRemaining;
}
```

## Система гербов (Crests)

### ToolCrest

```csharp
public class ToolCrest : ScriptableObject
{
    public string crestName;
    public int crestId;
    public Sprite icon;
    public string description;
    
    // Бонусы
    public float damageBonus;
    public float speedBonus;
    public int extraHealth;
    
    public bool IsUnlocked;
    public ToolItem[] equippedTools;
}
```

### Получение активного герба

```csharp
public ToolCrest GetEquippedCrest()
{
    var manager = ToolItemManager.UnsafeInstance;
    var pd = PlayerData.instance;
    
    return manager.GetCrestByName(pd.equippedCrestName);
}
```

## Конфигурации способностей

### HeroControllerConfig

```csharp
public class HeroControllerConfig
{
    // Скорости
    public float runSpeed;
    public float dashSpeed;
    public float wallSlideSpeed;
    
    // Прыжки
    public float jumpForce;
    public float doubleJumpForce;
    public float wallJumpForce;
    
    // Атаки
    public float attackCooldown;
    public int baseDamage;
    
    // Рывок
    public float dashDuration;
    public float dashCooldown;
}
```

## Примеры использования

### Разблокировка всех способностей

```csharp
public void UnlockAllAbilities()
{
    var pd = PlayerData.instance;
    
    // Движение
    pd.hasDash = true;
    pd.hasDoubleJump = true;
    pd.hasWallJump = true;
    pd.hasSuperDash = true;
    pd.hasWallClimb = true;
    
    // Боевые
    pd.hasParry = true;
    pd.hasChargeAttack = true;
    pd.hasDownAttack = true;
    
    // Максимальные уровни
    pd.nailLevel = 5;
    pd.dashLevel = 2;
    pd.doubleJumpLevel = 2;
}
```

### Проверка наличия способности

```csharp
public bool HasAbility(string abilityName)
{
    var pd = PlayerData.instance;
    if (pd == null) return false;
    
    return abilityName switch
    {
        "dash" => pd.hasDash,
        "doubleJump" => pd.hasDoubleJump,
        "wallJump" => pd.hasWallJump,
        "superDash" => pd.hasSuperDash,
        "parry" => pd.hasParry,
        _ => false
    };
}
```

### Использование инструмента

```csharp
public void UseTool(string toolName)
{
    var manager = ToolItemManager.UnsafeInstance;
    var tool = manager.GetToolByName(toolName);
    
    if (tool != null && tool.CanUse())
    {
        tool.Use();
    }
}
```

## Nail Arts (Искусства Гвоздя)

```csharp
public class PlayerData
{
    // Искусства гвоздя
    public bool hasNailArt1;
    public bool hasNailArt2;
    public bool hasNailArt3;
    
    // Уровень заряда
    public int nailArtChargeLevel;
}
```

## Связанные файлы

- `HeroController.cs` - Основной контроллер
- `HeroControllerConfig.cs` - Конфигурация способностей
- `PlayerData.cs` - Данные игрока
- `ToolItem.cs` - Инструменты
- `ToolItemManager.cs` - Менеджер инструментов
- `ToolCrest.cs` - Гербы
- `NailAttackBase.cs` - Базовая атака гвоздём
- `NailSlash.cs` - Удар гвоздём
