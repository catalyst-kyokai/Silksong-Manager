# Документация: Персонаж (Hero/Player)

## Обзор

Персонаж в Hollow Knight: Silksong управляется через несколько основных классов:

- **HeroController** - главный контроллер персонажа (движение, атаки, состояния)
- **PlayerData** - данные игрока (здоровье, шёлк, способности, прогресс)
- **HeroControllerStates** - текущие состояния персонажа

## Доступ к компонентам

```csharp
// Получение контроллера героя
HeroController hero = HeroController.instance;

// Получение данных игрока
PlayerData pd = PlayerData.instance;

// Получение состояний
HeroControllerStates cState = hero.cState;
```

## HeroController (HeroController.cs)

Файл: `silksong_source_code/HeroController.cs` (~12000 строк)

### Основные методы

| Метод | Описание |
|-------|----------|
| `AddHealth(int amount)` | Добавить здоровье |
| `AddSilk(int amount, bool heroEffect)` | Добавить шёлк |
| `TakeHealth(int amount)` | Нанести урон |
| `TakeSilk(int amount)` | Потратить шёлк |
| `SceneInit()` | Инициализация при загрузке сцены |
| `ThrowTool()` | Использовать инструмент |
| `TeleportTo(Vector3 position)` | Телепортация (если реализовано) |

### Состояния движения

```csharp
// Проверка состояний
bool isOnGround = hero.cState.onGround;
bool isJumping = hero.cState.jumping;
bool isDashing = hero.cState.dashing;
bool isAttacking = hero.cState.attacking;
```

### Полезные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `cState` | HeroControllerStates | Текущие состояния |
| `transform.position` | Vector3 | Позиция персонажа |
| `rb2d` | Rigidbody2D | Физика персонажа |

## PlayerData (PlayerData.cs)

Файл: `silksong_source_code/PlayerData.cs` (~4000 строк)

### Основные поля здоровья

| Поле | Тип | Описание |
|------|-----|----------|
| `health` | int | Текущее здоровье |
| `maxHealth` | int | Максимальное здоровье |
| `healthBlue` | int | Lifeblood (синее здоровье) |

### Поля шёлка (Silk)

| Поле | Тип | Описание |
|------|-----|----------|
| `silk` | int | Текущий шёлк |
| `silkMax` | int | Максимальный шёлк |

### Способности

| Поле | Тип | Описание |
|------|-----|----------|
| `hasDash` | bool | Есть ли рывок |
| `hasDoubleJump` | bool | Есть ли двойной прыжок |
| `hasWallJump` | bool | Есть ли прыжок от стены |
| `hasSuperDash` | bool | Есть ли супер-рывок |

### Флаги состояния

| Поле | Тип | Описание |
|------|-----|----------|
| `isInvincible` | bool | Неуязвимость |
| `atBench` | bool | На скамейке |
| `disablePause` | bool | Запрет паузы |

### Методы работы с данными

```csharp
// Получение/установка bool значений
bool value = pd.GetBool("fieldName");
pd.SetBool("fieldName", true);

// Получение/установка int значений
int value = pd.GetInt("fieldName");
pd.SetInt("fieldName", 100);
pd.IncrementInt("fieldName");
pd.DecrementInt("fieldName");

// Получение/установка float значений
float value = pd.GetFloat("fieldName");
pd.SetFloat("fieldName", 1.5f);

// Получение/установка string значений
string value = pd.GetString("fieldName");
pd.SetString("fieldName", "value");
```

## HeroControllerStates

Файл: `silksong_source_code/HeroControllerStates.cs`

### Все доступные состояния

```csharp
public class HeroControllerStates
{
    public bool facingRight;      // Смотрит вправо
    public bool onGround;         // На земле
    public bool jumping;          // Прыжок
    public bool shuttleCock;      // Shuttlecock состояние
    public bool floating;         // Парение
    public bool wallJumping;      // Прыжок от стены
    public bool doubleJumping;    // Двойной прыжок
    public bool nailCharging;     // Заряд гвоздя
    public bool shadowDashing;    // Теневой рывок
    public bool swimming;         // Плавание
    public bool falling;          // Падение
    public bool dashing;          // Рывок
    public bool isSprinting;      // Спринт
    public bool isBackSprinting;  // Спринт назад
    public bool isBackScuttling;  // Движение назад
    public bool airDashing;       // Воздушный рывок
    public bool superDashing;     // Супер-рывок
    public bool superDashOnWall;  // Супер-рывок от стены
    public bool backDashing;      // Рывок назад
    public bool touchingWall;     // Касание стены
    public bool wallSliding;      // Скольжение по стене
    public bool wallClinging;     // Цепляние за стену
    public bool wallScrambling;   // Карабканье по стене
    public bool transitioning;    // Переход между сценами
    public bool attacking;        // Атака
    public int attackCount;       // Счётчик атак
    public bool lookingUp;        // Смотрит вверх
    public bool lookingDown;      // Смотрит вниз
    public bool altAttack;        // Альтернативная атака
    public bool upAttacking;      // Атака вверх
    public bool downAttacking;    // Атака вниз
    public bool downTravelling;   // Движение вниз
    public bool downSpiking;      // Downspike
    public bool bouncing;         // Отскок
    public bool recoilingRight;   // Отдача вправо
    public bool recoilingLeft;    // Отдача влево
    public bool dead;             // Мёртв
    public bool hazardDeath;      // Смерть от опасности
    public bool hazardRespawning; // Возрождение после опасности
    public bool invulnerable;     // Неуязвим
    public bool casting;          // Использование заклинания
    public bool preventDash;      // Запрет рывка
    public bool dashCooldown;     // Кулдаун рывка
    public bool nearBench;        // Рядом со скамейкой
    public bool inWalkZone;       // В зоне ходьбы
    public bool isPaused;         // Игра на паузе
    public bool onConveyor;       // На конвейере
    public bool focusing;         // Фокусировка
    public bool inAcid;           // В кислоте
    public bool parrying;         // Парирование
    public bool isMaggoted;       // Заражён личинками
    public bool isFrosted;        // Заморожен
    public bool isBinding;        // Связывание
    // ... и многие другие
}
```

### Методы работы с состояниями

```csharp
// Получение состояния по имени
bool isJumping = cState.GetState("jumping");

// Установка состояния по имени
cState.SetState("invulnerable", true);

// Проверка существования состояния
bool exists = HeroControllerStates.CStateExists("jumping");

// Сброс всех состояний
cState.Reset();
```

## Примеры использования

### Полное исцеление

```csharp
public static void FullHeal()
{
    var pd = PlayerData.instance;
    var hero = HeroController.instance;
    
    if (pd != null && hero != null)
    {
        int healAmount = pd.maxHealth - pd.health;
        hero.AddHealth(healAmount);
    }
}
```

### Телепортация

```csharp
public static void TeleportTo(Vector3 position)
{
    var hero = HeroController.instance;
    if (hero != null)
    {
        hero.transform.position = position;
    }
}
```

### Включение неуязвимости

```csharp
public static void SetInvincible(bool value)
{
    var pd = PlayerData.instance;
    if (pd != null)
    {
        pd.isInvincible = value;
    }
}
```

### Разблокировка способностей

```csharp
public static void UnlockAllAbilities()
{
    var pd = PlayerData.instance;
    if (pd != null)
    {
        pd.hasDash = true;
        pd.hasDoubleJump = true;
        pd.hasWallJump = true;
        pd.hasSuperDash = true;
    }
}
```

## События персонажа

HeroController отправляет различные события:

- `HeroInPosition` - герой готов после загрузки сцены
- `PreHeroInPosition` - перед готовностью героя
- События атак и получения урона

## Связанные файлы

- `HeroAnimationController.cs` - анимации персонажа
- `HeroAudioController.cs` - звуки персонажа
- `HeroVibrationController.cs` - вибрация контроллера
- `HeroWaterController.cs` - взаимодействие с водой
