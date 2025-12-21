# Документация: Сцены (Scenes)

## Обзор

Система сцен в Silksong управляется через:

- **GameManager** - главный менеджер игры
- **SceneData** - данные сцены
- **TransitionPoint** - точки перехода
- **SceneLoad** - загрузка сцен

## GameManager - управление сценами

Файл: `silksong_source_code/GameManager.cs` (~5500 строк)

### Доступ

```csharp
GameManager gm = GameManager.instance;
string currentScene = gm.sceneName;
```

### Информация о загрузке сцены

```csharp
public class SceneLoadInfo
{
    public string SceneName;           // Имя сцены
    public string EntryGateName;       // Имя входного гейта
    public bool PreventCameraFadeOut;  // Предотвратить fade out
    public bool WaitForSceneTransitionCameraFade;
    public float EntryDelay;           // Задержка входа
    public SceneLoadVisualizations Visualization;
    public int TransitionID;
}
```

### SceneLoadVisualizations

```csharp
public enum SceneLoadVisualizations
{
    Default,
    Dream,
    Colosseum,
    GodFinderBoss,
    ContinueFromSave
}
```

### Переход между сценами

```csharp
public void TransitionToScene(string sceneName, string gateName)
{
    var gm = GameManager.instance;
    
    var loadInfo = new GameManager.SceneLoadInfo
    {
        SceneName = sceneName,
        EntryGateName = gateName,
        PreventCameraFadeOut = false,
        WaitForSceneTransitionCameraFade = true,
        EntryDelay = 0f,
        Visualization = GameManager.SceneLoadVisualizations.Default
    };
    
    gm.BeginSceneTransition(loadInfo);
}
```

## SceneData (SceneData.cs)

Файл: `silksong_source_code/SceneData.cs`

### Структура

```csharp
public class SceneData
{
    public static SceneData instance;
    
    // Постоянные данные булевых значений
    public PersistentBoolCollection PersistentBools;
    
    // Постоянные данные целых чисел
    public PersistentIntCollection PersistentInts;
    
    // Данные GeoRock
    private PersistentIntCollection geoRocks;
}
```

### PersistentItemData

```csharp
public class PersistentItemData<T>
{
    public string SceneName;
    public string ID;
    public T Value;
    public PersistentMutatorTypes Mutator;
    public bool IsSemiPersistent;
}
```

### Работа с постоянными данными

```csharp
// Сохранение состояния
public void SaveMyState(PersistentBoolItem item)
{
    var data = new PersistentItemData<bool>
    {
        SceneName = item.sceneName,
        ID = item.id,
        Value = item.activated
    };
    
    SceneData.instance.PersistentBools.SetValue(data);
}

// Получение состояния
public bool FindMyState(string sceneName, string id)
{
    return SceneData.instance.PersistentBools
        .GetValueOrDefault(sceneName, id);
}
```

## TransitionPoint (TransitionPoint.cs)

Точка перехода между сценами.

```csharp
public class TransitionPoint : MonoBehaviour
{
    public string targetScene;        // Целевая сцена
    public string entryPoint;         // Точка входа
    public bool isADoor;              // Это дверь
    public bool dontWalkOutOfDoor;    
    public Vector2 entryOffset;       // Смещение входа
    public bool alwaysEnterLeft;      // Всегда входить слева
    public bool alwaysEnterRight;     // Всегда входить справа
    
    public void BeginTransition();
}
```

## SceneLoad (SceneLoad.cs)

Контроллер загрузки сцены.

```csharp
public class SceneLoad
{
    public string TargetSceneName;
    public bool IsLoadComplete;
    public float LoadProgress;
    
    public IEnumerator LoadScene();
    public void ActivateScene();
    public void UnloadScene();
}
```

## SceneType (enum)

```csharp
public enum SceneType
{
    GAMEPLAY,      // Игровая
    MENU,          // Меню
    CUTSCENE,      // Катсцена
    BOSS,          // Босс
    TRANSITION     // Переход
}
```

## CustomSceneManager (CustomSceneManager.cs)

Файл: `silksong_source_code/CustomSceneManager.cs`

### Основные методы

```csharp
public class CustomSceneManager
{
    public static CustomSceneManager Instance;
    
    // Загрузка сцены
    public void LoadScene(string sceneName);
    public void LoadSceneAdditive(string sceneName);
    
    // Выгрузка сцены
    public void UnloadScene(string sceneName);
    
    // Проверки
    public bool IsSceneLoaded(string sceneName);
    public string GetActiveSceneName();
}
```

## Постоянные объекты (Persistent Items)

### PersistentBoolItem

```csharp
public class PersistentBoolItem : MonoBehaviour
{
    public string sceneName;
    public string id;
    public bool activated;
    
    public void SetMyID();
    public void SaveState();
    public void LoadState();
}
```

### PersistentIntItem

```csharp
public class PersistentIntItem : MonoBehaviour
{
    public string sceneName;
    public string id;
    public int value;
    
    public void SaveState();
    public void LoadState();
}
```

## Примеры использования

### Получение текущей сцены

```csharp
public string GetCurrentScene()
{
    return GameManager.instance?.sceneName ?? "Unknown";
}
```

### Переход на другую сцену

```csharp
public void GoToScene(string sceneName, string gate = "")
{
    var gm = GameManager.instance;
    if (gm == null) return;
    
    var loadInfo = new GameManager.SceneLoadInfo
    {
        SceneName = sceneName,
        EntryGateName = gate,
        Visualization = GameManager.SceneLoadVisualizations.Default
    };
    
    gm.BeginSceneTransition(loadInfo);
}
```

### Перезагрузка текущей сцены

```csharp
public void ReloadScene()
{
    var currentScene = GameManager.instance?.sceneName;
    if (!string.IsNullOrEmpty(currentScene))
    {
        GoToScene(currentScene);
    }
}
```

### Проверка посещения сцены

```csharp
public bool HasVisitedScene(string sceneName)
{
    var pd = PlayerData.instance;
    return pd.GetBool($"visited{sceneName}");
}
```

## Точки респауна

### RespawnMarker

```csharp
public class RespawnMarker : MonoBehaviour
{
    public bool respawnFacingRight;
    public bool stretch;
    
    public Vector3 GetRespawnPosition();
}
```

### HazardRespawnMarker

```csharp
public class HazardRespawnMarker : MonoBehaviour
{
    public string markerName;
    public Vector3 respawnPosition;
}
```

## Связанные файлы

- `GameManager.cs` - Главный менеджер
- `SceneData.cs` - Данные сцен
- `SceneLoad.cs` - Загрузчик сцен
- `CustomSceneManager.cs` - Менеджер сцен
- `TransitionPoint.cs` - Точки перехода
- `PersistentBoolItem.cs` - Постоянные bool объекты
- `PersistentIntItem.cs` - Постоянные int объекты
- `RespawnMarker.cs` - Маркеры респауна
