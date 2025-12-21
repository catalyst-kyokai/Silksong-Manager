# Документация: Валюты (Currency)

## Обзор

Система валют в Hollow Knight: Silksong включает:

- **Geo** - основная валюта
- **Shell Shards** - осколки раковин
- **CurrencyManager** - менеджер валют
- **CurrencyCounter** - UI счётчик валют

## CurrencyType (enum)

```csharp
public enum CurrencyType
{
    Money = 0,  // Geo
    Shard = 1   // Shell Shards
}
```

## CurrencyManager (CurrencyManager.cs)

Файл: `silksong_source_code/CurrencyManager.cs`

### Основные методы

| Метод | Описание |
|-------|----------|
| `AddGeo(int amount)` | Добавить Geo |
| `TakeGeo(int amount)` | Отнять Geo |
| `AddGeoQuietly(int amount)` | Добавить Geo без UI |
| `AddGeoToCounter(int amount)` | Добавить в счётчик |
| `AddShards(int amount)` | Добавить осколки |
| `TakeShards(int amount)` | Отнять осколки |
| `GetCurrencyAmount(CurrencyType)` | Получить количество |
| `TempStoreCurrency()` | Временно сохранить валюту |
| `RestoreTempStoredCurrency()` | Восстановить сохранённую |
| `ToZero()` | Обнулить все валюты |

### Доступ

```csharp
// Через статические методы
CurrencyManager.AddGeo(100);
CurrencyManager.TakeGeo(50);

// Через PlayerData
PlayerData.instance.geo += 100;
PlayerData.instance.ShellShards += 10;
```

### Очередь валют

CurrencyManager использует очередь для обработки изменений:

```csharp
private sealed class CurrencyQueue
{
    public int amount;
    public bool showCounter;
    
    public void Reset()
    {
        amount = 0;
        showCounter = false;
    }
}
```

## PlayerData - поля валют

```csharp
public class PlayerData
{
    // Geo
    public int geo;              // Текущее количество Geo
    public int TempGeoStore;     // Временное хранилище Geo
    
    // Shell Shards
    public int ShellShards;           // Текущие осколки
    public int TempShellShardStore;   // Временное хранилище осколков
}
```

### Методы PlayerData для валют

```csharp
// Добавить Geo
pd.AddGeo(int amount);

// Отнять Geo
pd.TakeGeo(int amount);

// Добавить осколки
pd.AddShards(int amount);

// Отнять осколки
pd.TakeShards(int amount);
```

## CurrencyCounter (UI)

Файл: `silksong_source_code/CurrencyCounter.cs`

UI элемент для отображения валюты.

### Основные методы

```csharp
public class CurrencyCounter
{
    public static void Add(int amount, CurrencyType type);
    public static void Take(int amount, CurrencyType type);
    public static void ToZero(CurrencyType type);
    public static void RefreshStartCount(CurrencyType type);
}
```

## CurrencyObject

Объекты валюты в игровом мире (выпадающие монеты и т.д.).

```csharp
public class CurrencyObjectBase : MonoBehaviour
{
    public int value;
    public CurrencyType currencyType;
    
    public void Collect();
    public void Fling(Vector2 velocity);
}
```

## Примеры использования

### Добавление валюты

```csharp
public static void AddGeo(int amount)
{
    var pd = PlayerData.instance;
    if (pd == null) return;
    
    pd.geo += amount;
    CurrencyManager.AddGeoToCounter(amount);
}
```

### Получение текущего количества

```csharp
public static int GetGeo()
{
    return CurrencyManager.GetCurrencyAmount(CurrencyType.Money);
}

public static int GetShards()
{
    return CurrencyManager.GetCurrencyAmount(CurrencyType.Shard);
}
```

### Установка точного значения

```csharp
public static void SetGeo(int amount)
{
    var pd = PlayerData.instance;
    if (pd != null)
    {
        pd.geo = Mathf.Max(0, amount);
    }
}
```

### Временное хранение (при смерти)

```csharp
// Сохранить валюту при смерти
CurrencyManager.TempStoreCurrency();

// Восстановить при возврате к тени
CurrencyManager.RestoreTempStoredCurrency();
```

## CurrencyCollectable

Собираемые объекты валюты.

```csharp
public class CurrencyCollectable : MonoBehaviour
{
    public int amount;
    public CurrencyType type;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Логика сбора
        CurrencyManager.AddCurrency(amount, type);
    }
}
```

## CurrencyCounterAppearRegion

Регион, при входе в который появляется счётчик валюты.

```csharp
public class CurrencyCounterAppearRegion : MonoBehaviour
{
    public CurrencyType currencyType;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Показать счётчик при входе
    }
}
```

## Связанные файлы

- `CurrencyManager.cs` - Основной менеджер
- `CurrencyCounter.cs` - UI счётчик
- `CurrencyCounterBase.cs` - Базовый класс счётчика
- `CurrencyCounterTyped.cs` - Типизированный счётчик
- `CurrencyObject.cs` - Объект валюты
- `CurrencyObjectBase.cs` - Базовый класс объекта
- `CurrencyCollectable.cs` - Собираемая валюта
- `CurrencyType.cs` - Enum типов валют
