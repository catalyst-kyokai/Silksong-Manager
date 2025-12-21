# Документация: Состояния (States)

## Обзор

Система состояний в Silksong включает:

- **HeroControllerStates** - состояния персонажа
- **FSM (PlayMaker)** - конечные автоматы
- **Game States** - состояния игры

## HeroControllerStates

Файл: `silksong_source_code/HeroControllerStates.cs`

### Полный список состояний

```csharp
public class HeroControllerStates
{
    // Направление
    public bool facingRight;          // Смотрит вправо
    
    // Базовое движение
    public bool onGround;             // На земле
    public bool jumping;              // Прыжок
    public bool falling;              // Падение
    public bool floating;             // Парение
    
    // Рывки
    public bool dashing;              // Рывок
    public bool airDashing;           // Воздушный рывок
    public bool backDashing;          // Рывок назад
    public bool shadowDashing;        // Теневой рывок
    public bool superDashing;         // Супер-рывок
    public bool superDashOnWall;      // Супер-рывок от стены
    
    // Спринт
    public bool isSprinting;          // Спринт
    public bool isBackSprinting;      // Спринт назад
    public bool isBackScuttling;      // Движение назад
    
    // Стены
    public bool touchingWall;         // Касание стены
    public bool wallSliding;          // Скольжение по стене
    public bool wallClinging;         // Цепляние за стену
    public bool wallScrambling;       // Карабканье
    public bool wallJumping;          // Прыжок от стены
    
    // Прыжки
    public bool shuttleCock;          // Shuttlecock
    public bool doubleJumping;        // Двойной прыжок
    public bool bouncing;             // Отскок
    public bool shroomBouncing;       // Отскок от гриба
    
    // Атаки
    public bool attacking;            // Атака
    public int attackCount;           // Счётчик атак
    public bool altAttack;            // Альтернативная атака
    public bool upAttacking;          // Атака вверх
    public bool downAttacking;        // Атака вниз
    public bool nailCharging;         // Заряд гвоздя
    public bool parrying;             // Парирование
    public bool parryAttack;          // Парирующая атака
    
    // Downspike
    public bool downTravelling;       // Движение вниз
    public bool downSpikeAntic;       // Подготовка downspike
    public bool downSpiking;          // Downspike
    public bool downSpikeBouncing;    // Отскок downspike
    public bool downSpikeBouncingShort;
    public bool downSpikeRecovery;    // Восстановление
    
    // Направление взгляда
    public bool lookingUp;            // Смотрит вверх
    public bool lookingDown;          // Смотрит вниз
    public bool lookingUpRing;        // Смотрит вверх (кольцо)
    public bool lookingDownRing;      // Смотрит вниз (кольцо)
    public bool lookingUpAnim;        // Анимация вверх
    public bool lookingDownAnim;      // Анимация вниз
    
    // Отдача
    public bool recoiling;            // Отдача
    public bool recoilingRight;       // Отдача вправо
    public bool recoilingLeft;        // Отдача влево
    public bool recoilingDrill;       // Отдача от дрели
    public bool recoilFrozen;         // Замороженная отдача
    
    // Заклинания
    public bool casting;              // Каст заклинания
    public bool castRecoiling;        // Отдача от каста
    public bool spellQuake;           // Quake заклинание
    public bool freezeCharge;         // Заморозка заряда
    public bool focusing;             // Фокусировка
    
    // Кулдауны
    public bool preventDash;          // Запрет рывка
    public bool preventBackDash;      // Запрет рывка назад
    public bool dashCooldown;         // Кулдаун рывка
    public bool backDashCooldown;     // Кулдаун рывка назад
    
    // Плавание и окружение
    public bool swimming;             // Плавание
    public bool inAcid;               // В кислоте
    public bool inUpdraft;            // В восходящем потоке
    public bool inWindRegion;         // В зоне ветра
    public bool inFrostRegion;        // В зоне холода
    public bool isFrosted;            // Заморожен
    public bool onConveyor;           // На конвейере
    public bool onConveyorV;          // На вертикальном конвейере
    public bool inConveyorZone;       // В зоне конвейера
    
    // Смерть
    public bool dead;                 // Мёртв
    public bool isFrostDeath;         // Смерть от холода
    public bool hazardDeath;          // Смерть от опасности
    public bool hazardRespawning;     // Возрождение
    
    // Неуязвимость
    public bool invulnerable;         // Неуязвим
    
    // Инструменты
    public bool isToolThrowing;       // Бросок инструмента
    public int toolThrowCount;        // Счётчик бросков
    public int throwingToolVertical;  // Вертикальный бросок
    
    // Другие
    public bool transitioning;        // Переход
    public bool nearBench;            // Рядом со скамейкой
    public bool inWalkZone;           // В зоне ходьбы
    public bool isPaused;             // Пауза
    public bool isMaggoted;           // Заражён личинками
    public bool isBinding;            // Связывание
    public bool needolinPlayingMemory;// Воспоминание Needolin
    public bool isScrewDownAttacking; // Атака отвёрткой
    public bool evading;              // Уклонение
    public bool whipLashing;          // Удар хлыстом
    public bool fakeHurt;             // Поддельный урон
    public bool willHardLand;         // Жёсткая посадка
    public bool touchingNonSlider;    // Касание не-слайдера
    public bool wasOnGround;          // Был на земле
    public bool mantling;             // Взбирание
    public bool mantleRecovery;       // Восстановление после взбирания
    public bool isTouchingSlopeLeft;  // Касание склона слева
    public bool isTouchingSlopeRight; // Касание склона справа
    public bool isInCancelableFSMMove;// В отменяемом FSM движении
    public bool isInCutsceneMovement; // В движении катсцены
    public bool isTriggerEventsPaused;// Триггерные события приостановлены
    
    // Счётчики
    public int downspikeInvulnerabilitySteps;
}
```

### Методы работы с состояниями

```csharp
// Получить состояние по имени
bool value = cState.GetState("jumping");

// Установить состояние по имени
cState.SetState("invulnerable", true);

// Проверить существование состояния
bool exists = HeroControllerStates.CStateExists("jumping");

// Сбросить все состояния
cState.Reset();

// Управление неуязвимостью
cState.AddInvulnerabilitySource(source);
cState.RemoveInvulnerabilitySource(source);
cState.ClearInvulnerabilitySources();
```

## Game States (GameManager)

### GameState enum

```csharp
public enum GameState
{
    INACTIVE,
    MAIN_MENU,
    LOADING,
    ENTERING_LEVEL,
    PLAYING,
    PAUSED,
    EXITING_LEVEL,
    CUTSCENE
}
```

### Проверка состояния игры

```csharp
public bool IsPlaying()
{
    var gm = GameManager.instance;
    return gm.gameState == GameState.PLAYING;
}

public bool IsPaused()
{
    return GameManager.instance.IsGamePaused();
}
```

## UIState

```csharp
public enum UIState
{
    INACTIVE,
    MAIN_MENU,
    LOADING,
    CUTSCENE,
    PLAYING,
    PAUSED
}
```

## FSM (PlayMaker)

Silksong использует PlayMaker для сложных FSM.

### Работа с FSM

```csharp
// Получить FSM
PlayMakerFSM fsm = gameObject.GetComponent<PlayMakerFSM>();

// Отправить событие
fsm.SendEvent("EVENT_NAME");

// Получить/установить переменные FSM
FsmInt intVar = fsm.FsmVariables.GetFsmInt("variableName");
intVar.Value = 100;

FsmBool boolVar = fsm.FsmVariables.GetFsmBool("variableName");
boolVar.Value = true;
```

### FSMUtility

```csharp
public static class FSMUtility
{
    // Отправить событие FSM по имени
    public static void SendEventToGameObject(
        GameObject go, 
        string eventName, 
        bool isFullPath = false
    );
    
    // Получить FSM по имени
    public static PlayMakerFSM LocateFSM(
        GameObject go, 
        string fsmName
    );
    
    // Найти все FSM
    public static PlayMakerFSM[] LocateFSMs(GameObject go);
}
```

## Примеры использования

### Проверка нескольких состояний

```csharp
public bool CanPerformAction()
{
    var hero = HeroController.instance;
    var cs = hero.cState;
    
    return !cs.dead && 
           !cs.transitioning && 
           !cs.isPaused && 
           !cs.casting &&
           cs.onGround;
}
```

### Установка состояния неуязвимости

```csharp
public void SetInvulnerable(bool value)
{
    var hero = HeroController.instance;
    hero.cState.invulnerable = value;
}
```

### Мониторинг состояний

```csharp
public string GetCurrentStateInfo()
{
    var hero = HeroController.instance;
    var cs = hero.cState;
    
    var states = new List<string>();
    
    if (cs.onGround) states.Add("OnGround");
    if (cs.jumping) states.Add("Jumping");
    if (cs.falling) states.Add("Falling");
    if (cs.dashing) states.Add("Dashing");
    if (cs.attacking) states.Add("Attacking");
    if (cs.invulnerable) states.Add("Invulnerable");
    
    return string.Join(", ", states);
}
```

## Transition States

```csharp
public enum HeroTransitionState
{
    WAITING_TO_TRANSITION,
    EXITING_SCENE,
    WAITING_TO_ENTER_LEVEL,
    ENTERING_SCENE,
    DROPPING_DOWN
}
```

## Связанные файлы

- `HeroControllerStates.cs` - Состояния персонажа
- `GameManager.cs` - Состояния игры
- `UIManager.cs` - Состояния UI
- `FSMUtility.cs` - Утилиты для FSM
- `PlayMakerFSM.cs` - PlayMaker FSM
