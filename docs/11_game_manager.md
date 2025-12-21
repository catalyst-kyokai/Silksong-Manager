# Документация: GameManager и основные системы

## Обзор

GameManager - центральный класс, управляющий всей игрой:

- Состояние игры
- Загрузка/выгрузка сцен
- Сохранение/загрузка
- Пауза
- События игры

## GameManager (GameManager.cs)

Файл: `silksong_source_code/GameManager.cs` (~5500 строк)

### Доступ

```csharp
GameManager gm = GameManager.instance;
```

### Основные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `sceneName` | string | Текущая сцена |
| `gameState` | GameState | Состояние игры |
| `playerData` | PlayerData | Данные игрока |
| `sceneData` | SceneData | Данные сцен |
| `gameMap` | GameMap | Карта игры |
| `inputHandler` | InputHandler | Обработчик ввода |
| `hero_ctrl` | HeroController | Контроллер героя |
| `gameCams` | GameCameras | Камеры игры |
| `currencyManager` | CurrencyManager | Менеджер валют |

### Методы управления сценами

```csharp
// Загрузка сцены
public void BeginSceneTransition(SceneLoadInfo info);
public void ChangeToScene(string sceneName, string gateName, float delay);
public void TransitionScene(TransitionInfo info);

// Выгрузка сцены
public void UnloadScene(string sceneName);
public void ReportUnload();

// Проверка сцены
public string GetEntryGateName();
public bool IsInLevel();
public bool IsMenuScene();
```

### Методы управления игрой

```csharp
// Пауза
public bool IsGamePaused();
public void SetGamePaused(bool paused);

// Сохранение
public void SaveGame();
public void SaveGameSilent();
public void LoadGame(int slot);

// Смерть и респаун
public void PlayerDead(float delay);
public void PlayerDeadFromHazard(HazardType hazardType);
public void ReadyForRespawn(bool isRespawning);
public void HazardRespawn();

// Выход
public void ReturnToMainMenu();
public void QuitGame();
```

### События GameManager

```csharp
// События
public event Action OnBeginLevelLoad;
public event Action OnFinishLevelLoad;
public event Action OnEnterMenu;
public event Action OnExitMenu;
public event Action OnPause;
public event Action OnUnpause;
public event Action OnPlayerDead;

// Подписка на события
void Start()
{
    GameManager.instance.OnPause += OnGamePaused;
    GameManager.instance.OnUnpause += OnGameUnpaused;
}

void OnDestroy()
{
    GameManager.instance.OnPause -= OnGamePaused;
    GameManager.instance.OnUnpause -= OnGameUnpaused;
}

void OnGamePaused()
{
    Debug.Log("Игра на паузе");
}

void OnGameUnpaused()
{
    Debug.Log("Игра продолжена");
}
```

## GameSettings (GameSettings.cs)

Настройки игры.

```csharp
public class GameSettings
{
    public static GameSettings instance;
    
    // Графика
    public int resolutionIndex;
    public bool fullscreen;
    public int vSyncCount;
    public float brightness;
    
    // Звук
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    
    // Управление
    public bool vibrationEnabled;
    public float cameraSensitivity;
    
    // Геймплей
    public int difficulty;
    public string language;
}
```

## InputHandler (InputHandler.cs)

Обработчик ввода.

```csharp
public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance;
    
    // Текущий ввод
    public float moveX;
    public float moveY;
    public bool jump;
    public bool attack;
    public bool dash;
    public bool cast;
    public bool pause;
    public bool inventory;
    public bool quickMap;
    
    // Методы проверки
    public bool IsPressed(HeroActionButton button);
    public bool IsHeld(HeroActionButton button);
    public bool WasReleased(HeroActionButton button);
    
    // Блокировка ввода
    public void EnableInput();
    public void DisableInput();
    public bool IsInputEnabled();
}
```

### HeroActionButton (enum)

```csharp
public enum HeroActionButton
{
    JUMP,
    ATTACK,
    DASH,
    SUPER_DASH,
    CAST,
    TAUNT,
    QUICK_MAP,
    INVENTORY,
    MENU_SUBMIT,
    MENU_CANCEL,
    DREAM_NAIL,
    UP,
    DOWN,
    LEFT,
    RIGHT,
    QUICK_CAST,
    MENU_PANE_LEFT,
    MENU_PANE_RIGHT,
    MENU_EXTRA,
    MENU_SUPER,
    INVENTORY_MAP,
    INVENTORY_JOURNAL,
    INVENTORY_TOOLS,
    INVENTORY_QUESTS
}
```

## GameCameras (GameCameras.cs)

Система камер.

```csharp
public class GameCameras : MonoBehaviour
{
    public static GameCameras instance;
    
    public Camera mainCamera;
    public Camera hudCamera;
    
    public CameraController cameraController;
    public CameraTarget cameraTarget;
}
```

### CameraController

```csharp
public class CameraController : MonoBehaviour
{
    public Vector3 targetPosition;
    public float dampTime;
    
    public void SnapTo(Vector3 position);
    public void FocusOn(Transform target);
    public void Shake(CameraShakeProfile profile);
}
```

## AudioManager (AudioManager.cs)

Управление звуком.

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    // Громкости
    public float masterVolume;
    public float musicVolume;
    public float soundVolume;
    
    // Воспроизведение
    public void PlaySound(AudioClip clip);
    public void PlayMusic(AudioClip clip);
    public void StopMusic();
    
    // Снэпшоты
    public void ApplySnapshot(string snapshotName);
}
```

## TimeManager

Управление временем.

```csharp
public class TimeManager
{
    public static float timeScale
    {
        get => Time.timeScale;
        set => Time.timeScale = Mathf.Clamp(value, 0f, 10f);
    }
    
    public static void Pause()
    {
        Time.timeScale = 0f;
    }
    
    public static void Resume()
    {
        Time.timeScale = 1f;
    }
    
    public static void SlowMotion(float scale = 0.5f)
    {
        Time.timeScale = scale;
    }
}
```

## Примеры использования

### Переход между сценами

```csharp
public void GoToScene(string sceneName)
{
    var gm = GameManager.instance;
    
    var loadInfo = new GameManager.SceneLoadInfo
    {
        SceneName = sceneName,
        EntryGateName = "",
        Visualization = GameManager.SceneLoadVisualizations.Default
    };
    
    gm.BeginSceneTransition(loadInfo);
}
```

### Быстрое сохранение

```csharp
public void QuickSave()
{
    var gm = GameManager.instance;
    gm.SaveGameSilent();
}
```

### Пауза игры

```csharp
public void TogglePause()
{
    var gm = GameManager.instance;
    
    if (gm.IsGamePaused())
    {
        // Снять паузу
        Time.timeScale = 1f;
        UIManager.instance.UIClosePauseMenu();
    }
    else
    {
        // Поставить на паузу
        Time.timeScale = 0f;
        UIManager.instance.UIGoToPauseMenu();
    }
}
```

### Получение текущего состояния

```csharp
public GameInfo GetGameInfo()
{
    var gm = GameManager.instance;
    var pd = PlayerData.instance;
    
    return new GameInfo
    {
        SceneName = gm.sceneName,
        IsPaused = gm.IsGamePaused(),
        GameTime = pd.playTime,
        Health = pd.health,
        Geo = pd.geo
    };
}
```

## Связанные файлы

- `GameManager.cs` - Главный менеджер
- `GameSettings.cs` - Настройки игры
- `InputHandler.cs` - Ввод
- `AudioManager.cs` - Звук
- `GameCameras.cs` - Камеры
- `SaveGameData.cs` - Данные сохранения
