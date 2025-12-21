# Документация: Карта (Map)

## Обзор

Система карты в Hollow Knight: Silksong включает:

- **GameMap** - главный контроллер карты
- **GameMapScene** - сцена на карте
- **MapPin** - маркеры на карте
- **MapMarkerMenu** - меню маркеров

## Основные классы

### GameMap (GameMap.cs)

Файл: `silksong_source_code/GameMap.cs` (~2000 строк)

Главный контроллер игровой карты.

#### Доступ

```csharp
GameMap gameMap = GameManager.instance.gameMap;
```

#### Структура зон

```csharp
public class ZoneInfo
{
    public string zoneName;
    public Transform root;
    public bool isUnlocked;
    public List<MapCache> maps;
    
    public class MapCache
    {
        public string sceneName;
        public ParentInfo mapParent;
        public GameObject gameObject;
        public GameMapScene gameMapScene;
        public bool hasGameMap;
    }
}
```

#### Основные методы

| Метод | Описание |
|-------|----------|
| `WorldMap()` | Открыть мировую карту |
| `TryOpenQuickMap()` | Попытаться открыть быструю карту |
| `CloseQuickMap()` | Закрыть быструю карту |
| `GetCurrentMapZone()` | Получить текущую зону карты |
| `GetMapZoneFromSceneName(string)` | Получить зону по имени сцены |
| `GetMapPosition(string)` | Получить позицию на карте |
| `SetupMap()` | Настроить карту |
| `EnableUnlockedAreas()` | Включить разблокированные области |
| `DisableAllAreas()` | Отключить все области |
| `UpdateCurrentScene()` | Обновить текущую сцену |

#### Условные позиции

```csharp
public class ConditionalPosition
{
    public Vector3 position;
    public List<string> conditions;
}
```

### GameMapScene (GameMapScene.cs)

Представляет отдельную сцену на карте.

```csharp
public class GameMapScene : MonoBehaviour
{
    public string sceneName;
    public bool isVisited;
    public bool isMapped;
    
    public void UpdateState();
    public bool IsUnlocked();
}
```

### MapPin (MapPin.cs)

Маркер/пин на карте.

```csharp
public class MapPin : MonoBehaviour
{
    public string pinName;
    public MapPinState state;
    public Sprite normalSprite;
    public Sprite highlightedSprite;
    
    public void Show();
    public void Hide();
    public void Highlight();
}
```

## Зоны карты (MapZone enum)

```csharp
public enum MapZone
{
    NONE,
    TOWN,           // Город
    CROSSROADS,     // Перекрёстки
    GREENPATH,      // Зелёный путь
    FUNGAL_WASTES,  // Грибные пустоши
    CLIFFS,         // Утёсы
    // ... и другие зоны
}
```

## Работа с картой

### Получение текущей зоны

```csharp
public MapZone GetCurrentZone()
{
    var gm = GameManager.instance;
    return gm.GetCurrentMapZone();
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

### Добавление маркера

```csharp
public void AddMapPin(Vector3 position, string markerType)
{
    var gameMap = GameManager.instance.gameMap;
    // Логика добавления маркера
}
```

## MapMarkerMenu (MapMarkerMenu.cs)

Меню для управления маркерами на карте.

### Основные методы

| Метод | Описание |
|-------|----------|
| `Open()` | Открыть меню маркеров |
| `Close()` | Закрыть меню |
| `PlaceMarker()` | Установить маркер |
| `RemoveMarker()` | Удалить маркер |

## Связь с PlayerData

Карта хранит данные в PlayerData:

```csharp
// Посещённые сцены
pd.visitedScenes;         // HashSet<string>

// Открытые зоны
pd.mapAllRooms;           // bool
pd.mapDirtmouth;          // Город
pd.mapGreenPath;          // Зелёный путь
// ... и другие зоны

// Маркеры
pd.placedMarkers;         // List<MarkerData>
```

## FastTravelMap

Система быстрого перемещения.

```csharp
public class FastTravelMap : MonoBehaviour
{
    public List<FastTravelPoint> fastTravelPoints;
    
    public void TravelTo(string sceneName);
    public bool CanTravelTo(string sceneName);
}
```

## Примеры использования

### Открыть карту зоны

```csharp
public void UnlockMapZone(MapZone zone)
{
    var pd = PlayerData.instance;
    
    switch (zone)
    {
        case MapZone.GREENPATH:
            pd.mapGreenPath = true;
            break;
        // ... другие зоны
    }
    
    var gameMap = GameManager.instance.gameMap;
    gameMap.SetupMap();
}
```

### Получить позицию на карте

```csharp
public Vector3 GetSceneMapPosition(string sceneName)
{
    var gameMap = GameManager.instance.gameMap;
    return gameMap.GetMapPosition(sceneName);
}
```

## InventoryWideMap

Карта в инвентаре.

```csharp
public class InventoryWideMap : MonoBehaviour
{
    public float zoomLevel;
    public Vector3 currentPosition;
    
    public void Pan(Vector2 direction);
    public void Zoom(float amount);
    public void CenterOnPlayer();
}
```

## Связанные файлы

- `GameMapScene.cs` - Сцена карты
- `MapPin.cs` - Маркеры
- `MapMarkerMenu.cs` - Меню маркеров
- `FastTravelMap.cs` - Быстрое перемещение
- `MapMarkerButton.cs` - Кнопки маркеров
- `InventoryWideMap.cs` - Карта в инвентаре
- `InventoryMapManager.cs` - Менеджер карты инвентаря
