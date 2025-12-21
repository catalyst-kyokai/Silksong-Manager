# Документация: Предметы (Items)

## Обзор

Система предметов в Silksong включает:

- **CollectableItem** - собираемые предметы
- **ToolItem** - инструменты
- **MateriumItem** - материалы
- **Relics** - реликвии
- **ShopItem** - товары в магазинах

## CollectableItem

Файл: `silksong_source_code/CollectableItem.cs`

### Базовый класс

```csharp
public class CollectableItem : MonoBehaviour
{
    public string itemName;
    public int itemId;
    public Sprite icon;
    public string description;
    
    public virtual void Collect();
    public virtual bool CanCollect();
}
```

### CollectableItemManager

Управляет всеми собираемыми предметами.

```csharp
public class CollectableItemManager
{
    public static CollectableItemManager Instance;
    
    public List<CollectableItem> allItems;
    public List<CollectableItem> collectedItems;
    
    public void RegisterItem(CollectableItem item);
    public void CollectItem(CollectableItem item);
    public bool HasItem(string itemName);
}
```

## ToolItem (Инструменты)

Файл: `silksong_source_code/ToolItem.cs`

### Структура

```csharp
public class ToolItem : ScriptableObject
{
    public string toolName;
    public int toolId;
    public Sprite icon;
    public ToolItemType itemType;
    
    public bool IsUnlocked;
    public int CurrentAmount;
    public int MaxAmount;
    
    public void Use();
    public void Replenish(int amount);
}
```

### ToolItemType

```csharp
public enum ToolItemType
{
    Consumable,     // Одноразовые
    Reusable,       // Многоразовые
    Permanent       // Постоянные
}
```

### ToolItemManager

Файл: `silksong_source_code/ToolItemManager.cs`

```csharp
public class ToolItemManager
{
    public static ToolItemManager UnsafeInstance;
    
    // Получение инструментов
    public IEnumerable<ToolItem> GetAllTools();
    public IEnumerable<ToolItem> GetUnlockedTools();
    public ToolItem GetToolByName(string name);
    
    // Управление
    public void UnlockAllTools();
    public void UnlockAllCrests();
    public void TryReplenishTools(int minAmount, int maxAmount);
    
    // Экипировка
    public void SetEquippedTools(ToolItem[] tools);
    public ToolItem[] GetCurrentEquippedTools();
    public bool IsToolEquipped(ToolItem tool);
}
```

## ToolCrest (Гербы)

```csharp
public class ToolCrest : ScriptableObject
{
    public string crestName;
    public int crestId;
    public Sprite icon;
    
    public bool IsUnlocked;
    public ToolItem[] equippedTools;
}
```

## MateriumItem (Материалы)

Файл: `silksong_source_code/MateriumItem.cs`

```csharp
public class MateriumItem : ScriptableObject
{
    public string itemName;
    public int itemId;
    public Sprite icon;
    public int stackSize;
    public int currentAmount;
}
```

### MateriumItemManager

```csharp
public class MateriumItemManager
{
    public static MateriumItemManager Instance;
    
    public void AddItem(MateriumItem item, int amount);
    public void RemoveItem(MateriumItem item, int amount);
    public int GetItemCount(MateriumItem item);
}
```

## CollectableRelic (Реликвии)

Файл: `silksong_source_code/CollectableRelic.cs`

```csharp
public class CollectableRelic : MonoBehaviour
{
    public string relicName;
    public int relicId;
    public CollectableItemRelicType relicType;
    
    public void Collect();
}
```

## ShopItem (Товары)

Файл: `silksong_source_code/ShopItem.cs`

```csharp
public class ShopItem : MonoBehaviour
{
    public string itemName;
    public int price;
    public CurrencyType currencyType;
    public Sprite icon;
    public string description;
    
    public bool CanAfford();
    public void Purchase();
}
```

### ShopMenuStock

```csharp
public class ShopMenuStock : MonoBehaviour
{
    public List<ShopItem> items;
    
    public void RefreshStock();
    public bool HasStock(ShopItem item);
}
```

## Данные в PlayerData

```csharp
// Предметы
public List<string> collectedItems;

// Инструменты
public ToolItemsData toolItemsData;

// Материалы
public MateriumItemsData materiumItems;

// Реликвии
public CollectableRelicsData relicsData;
```

## Примеры использования

### Получение всех инструментов

```csharp
public List<ToolInfo> GetAllTools()
{
    var manager = ToolItemManager.UnsafeInstance;
    var tools = manager.GetAllTools();
    
    return tools.Select(t => new ToolInfo
    {
        Name = t.toolName,
        IsUnlocked = t.IsUnlocked,
        Amount = t.CurrentAmount
    }).ToList();
}
```

### Разблокировка инструмента

```csharp
public void UnlockTool(string toolName)
{
    var manager = ToolItemManager.UnsafeInstance;
    var tool = manager.GetToolByName(toolName);
    
    if (tool != null)
    {
        manager.ReportToolUnlocked(tool);
    }
}
```

### Пополнение инструментов

```csharp
public void ReplenishAllTools()
{
    var manager = ToolItemManager.UnsafeInstance;
    manager.TryReplenishTools(0, 9999);
}
```

## Инвентарь

### InventoryItemCollectable

```csharp
public class InventoryItemCollectable : MonoBehaviour
{
    public CollectableItem item;
    
    public void Display();
    public void Select();
}
```

### InventoryItemTool

```csharp
public class InventoryItemTool : MonoBehaviour
{
    public ToolItem tool;
    
    public void Equip();
    public void Unequip();
    public void Use();
}
```

## Связанные файлы

- `CollectableItem.cs` - Базовый собираемый предмет
- `CollectableItemManager.cs` - Менеджер предметов
- `ToolItem.cs` - Инструмент
- `ToolItemManager.cs` - Менеджер инструментов
- `ToolCrest.cs` - Герб
- `MateriumItem.cs` - Материал
- `MateriumItemManager.cs` - Менеджер материалов
- `CollectableRelic.cs` - Реликвия
- `ShopItem.cs` - Товар магазина
- `ShopMenuStock.cs` - Ассортимент магазина
- `InventoryItem*.cs` - UI предметов инвентаря
