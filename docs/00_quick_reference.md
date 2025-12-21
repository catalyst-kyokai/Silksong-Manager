# Быстрый справочник: Основные классы и синглтоны

## Главные синглтоны

| Класс | Доступ | Описание |
|-------|--------|----------|
| `GameManager` | `GameManager.instance` | Главный менеджер игры |
| `PlayerData` | `PlayerData.instance` | Данные игрока |
| `HeroController` | `HeroController.instance` | Контроллер персонажа |
| `UIManager` | `UIManager.instance` | Менеджер UI |
| `AudioManager` | `AudioManager.Instance` | Менеджер аудио |
| `CurrencyManager` | `CurrencyManager` (static) | Менеджер валют |
| `ToolItemManager` | `ToolItemManager.UnsafeInstance` | Менеджер инструментов |
| `SceneData` | `SceneData.instance` | Данные сцен |
| `InputHandler` | `InputHandler.Instance` | Обработчик ввода |

## Быстрый доступ к данным

```csharp
// Здоровье
int health = PlayerData.instance.health;
int maxHealth = PlayerData.instance.maxHealth;

// Шёлк
int silk = PlayerData.instance.silk;
int maxSilk = PlayerData.instance.silkMax;

// Geo
int geo = PlayerData.instance.geo;

// Позиция персонажа
Vector3 pos = HeroController.instance.transform.position;

// Текущая сцена
string scene = GameManager.instance.sceneName;

// Проверка на земле
bool onGround = HeroController.instance.cState.onGround;

// Пауза
bool isPaused = GameManager.instance.IsGamePaused();
```

## Часто используемые методы

### Персонаж

```csharp
// Исцеление
HeroController.instance.AddHealth(amount);

// Добавить шёлк
HeroController.instance.AddSilk(amount, heroEffect: false);

// Телепортация
HeroController.instance.transform.position = newPosition;

// Неуязвимость
PlayerData.instance.isInvincible = true;
```

### Валюта

```csharp
// Добавить Geo
CurrencyManager.AddGeo(amount);

// Отнять Geo
CurrencyManager.TakeGeo(amount);

// Осколки
CurrencyManager.AddShards(amount);
```

### Сцены

```csharp
// Переход на сцену
var loadInfo = new GameManager.SceneLoadInfo
{
    SceneName = "SceneName",
    EntryGateName = ""
};
GameManager.instance.BeginSceneTransition(loadInfo);
```

### Враги

```csharp
// Найти всех врагов
var enemies = Object.FindObjectsOfType<HealthManager>();

// Убить врага
enemy.Die(0f, AttackTypes.Generic, false);

// Нанести урон
enemy.ApplyExtraDamage(damage);
```

### UI

```csharp
// Открыть меню паузы
UIManager.instance.UIGoToPauseMenu();

// Закрыть меню паузы
UIManager.instance.UIClosePauseMenu();
```

## Состояния персонажа (cState)

```csharp
var cs = HeroController.instance.cState;

// Проверки
bool onGround = cs.onGround;
bool jumping = cs.jumping;
bool falling = cs.falling;
bool dashing = cs.dashing;
bool attacking = cs.attacking;
bool dead = cs.dead;
bool invulnerable = cs.invulnerable;
bool facingRight = cs.facingRight;

// Установка
cs.invulnerable = true;
cs.SetState("stateName", true);
```

## Типы атак

```csharp
public enum AttackTypes
{
    Generic,
    Nail,
    Spell,
    Acid,
    Fire,
    SharpShadow,
    RuinousDash,
    Ranged,
    Crest,
    Tool
}
```

## Типы валют

```csharp
public enum CurrencyType
{
    Money = 0,  // Geo
    Shard = 1   // Осколки
}
```

## Горячие клавиши мода (по умолчанию)

| Клавиша | Действие |
|---------|----------|
| F1 | Открыть Debug Menu |
| F2 | Быстрое исцеление |
| F3 | Пополнить шёлк |
| F4 | Неуязвимость |
| F5 | +1000 Geo |
| F6 | Бесконечные прыжки |
| F7 | Noclip |
| F9 | Сохранить позицию |
| F10 | Загрузить позицию |

## Файлы проекта

```
silksong_manager/
├── Plugin.cs                 # Точка входа
├── PluginInfo.cs             # Метаданные
├── PluginConfig.cs           # Конфигурация
├── Player/
│   └── PlayerActions.cs      # Действия игрока
├── Currency/
│   └── CurrencyActions.cs    # Валюты
├── World/
│   └── WorldActions.cs       # Сцены и мир
├── Tools/
│   └── ToolActions.cs        # Инструменты
├── Enemies/
│   └── EnemyActions.cs       # Враги
├── DebugMenu/
│   └── DebugMenuController.cs # Debug UI
└── docs/                     # Документация
```
