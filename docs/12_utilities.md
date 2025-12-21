# Документация: Полезные утилиты и хуки

## Обзор

Эта документация описывает полезные техники для создания модов:

- Утилиты для поиска объектов
- Хуки и патчи (Harmony)
- Запуск корутин
- Работа с ресурсами

## Поиск объектов

### Поиск по типу

```csharp
// Найти все объекты типа
var allEnemies = Object.FindObjectsOfType<HealthManager>();

// Найти один объект типа
var hero = Object.FindObjectOfType<HeroController>();

// Найти неактивные объекты
var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
```

### Поиск по имени

```csharp
// Найти GameObject по имени
var go = GameObject.Find("ObjectName");

// Найти в дочерних
var child = parent.transform.Find("ChildName");

// Найти по тегу
var tagged = GameObject.FindGameObjectWithTag("Enemy");
var allTagged = GameObject.FindGameObjectsWithTag("Enemy");
```

### Кастомный поиск

```csharp
public static T FindComponent<T>(string name) where T : Component
{
    var go = GameObject.Find(name);
    return go?.GetComponent<T>();
}

public static List<T> FindAllWithComponent<T>() where T : Component
{
    return Object.FindObjectsOfType<T>().ToList();
}
```

## Harmony Patches

### Установка Harmony

```csharp
using HarmonyLib;

[BepInPlugin("mod.id", "Mod Name", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    
    void Awake()
    {
        _harmony = new Harmony("mod.id");
        _harmony.PatchAll();
    }
    
    void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
```

### Prefix Patch (до метода)

```csharp
[HarmonyPatch(typeof(HeroController), nameof(HeroController.TakeDamage))]
public static class TakeDamage_Patch
{
    // Prefix выполняется ДО оригинального метода
    // Вернуть false = пропустить оригинальный метод
    static bool Prefix(HeroController __instance, ref int damage)
    {
        // Если включена неуязвимость - отменить урон
        if (PlayerData.instance.isInvincible)
        {
            return false; // Не вызывать оригинал
        }
        
        // Модифицировать урон
        damage = damage / 2;
        
        return true; // Вызвать оригинал
    }
}
```

### Postfix Patch (после метода)

```csharp
[HarmonyPatch(typeof(HeroController), nameof(HeroController.AddHealth))]
public static class AddHealth_Patch
{
    // Postfix выполняется ПОСЛЕ оригинального метода
    static void Postfix(HeroController __instance, int amount)
    {
        Plugin.Log.LogInfo($"Player healed for {amount}");
    }
}
```

### Работа с приватными полями

```csharp
[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Die))]
public static class Die_Patch
{
    static void Postfix(HealthManager __instance)
    {
        // Доступ к приватному полю через Traverse
        var isDead = Traverse.Create(__instance)
            .Field("isDead")
            .GetValue<bool>();
        
        Plugin.Log.LogInfo($"Enemy died: {__instance.name}");
    }
}
```

### Patch с IL-манипуляцией

```csharp
[HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.AddGeo))]
public static class AddGeo_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        
        // Модификация IL кода
        for (int i = 0; i < codes.Count; i++)
        {
            // Логика модификации
        }
        
        return codes;
    }
}
```

## Корутины

### Запуск корутин

```csharp
public class MyBehaviour : MonoBehaviour
{
    void Start()
    {
        // Запуск корутины
        StartCoroutine(MyCoroutine());
        
        // С параметрами
        StartCoroutine(DelayedAction(2f, () => Debug.Log("Done!")));
    }
    
    IEnumerator MyCoroutine()
    {
        Debug.Log("Начало");
        
        // Ждать 1 секунду
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Прошла 1 секунда");
        
        // Ждать конца кадра
        yield return new WaitForEndOfFrame();
        
        // Ждать следующий кадр
        yield return null;
        
        // Ждать до условия
        yield return new WaitUntil(() => PlayerData.instance != null);
        
        Debug.Log("Конец");
    }
    
    IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
```

### Остановка корутин

```csharp
private Coroutine _myCoroutine;

void StartMyCoroutine()
{
    // Остановить если уже запущена
    if (_myCoroutine != null)
    {
        StopCoroutine(_myCoroutine);
    }
    
    _myCoroutine = StartCoroutine(MyCoroutine());
}

void StopMyCoroutine()
{
    if (_myCoroutine != null)
    {
        StopCoroutine(_myCoroutine);
        _myCoroutine = null;
    }
}

// Остановить все корутины
void StopAll()
{
    StopAllCoroutines();
}
```

### Unscaled Time корутины

```csharp
IEnumerator PauseProofCoroutine()
{
    // Работает даже при Time.timeScale = 0
    yield return new WaitForSecondsRealtime(1f);
}
```

## Рефлексия

### Доступ к приватным полям

```csharp
using System.Reflection;

public static T GetPrivateField<T>(object obj, string fieldName)
{
    var field = obj.GetType().GetField(
        fieldName, 
        BindingFlags.NonPublic | BindingFlags.Instance
    );
    
    return (T)field?.GetValue(obj);
}

public static void SetPrivateField<T>(object obj, string fieldName, T value)
{
    var field = obj.GetType().GetField(
        fieldName, 
        BindingFlags.NonPublic | BindingFlags.Instance
    );
    
    field?.SetValue(obj, value);
}
```

### Вызов приватных методов

```csharp
public static object InvokePrivateMethod(
    object obj, 
    string methodName, 
    params object[] args)
{
    var method = obj.GetType().GetMethod(
        methodName, 
        BindingFlags.NonPublic | BindingFlags.Instance
    );
    
    return method?.Invoke(obj, args);
}
```

## Полезные расширения

```csharp
public static class Extensions
{
    // Проверка null для Unity объектов
    public static bool IsNull(this object obj)
    {
        return obj == null || obj.Equals(null);
    }
    
    // Безопасное получение компонента
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }
    
    // Проверка дистанции до героя
    public static float DistanceToHero(this Transform transform)
    {
        var hero = HeroController.instance;
        if (hero == null) return float.MaxValue;
        
        return Vector3.Distance(
            transform.position, 
            hero.transform.position
        );
    }
    
    // Логирование с именем объекта
    public static void Log(this MonoBehaviour mb, string message)
    {
        Debug.Log($"[{mb.GetType().Name}] {message}");
    }
}
```

## Singleton Pattern

```csharp
public class MySingleton : MonoBehaviour
{
    private static MySingleton _instance;
    public static MySingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MySingleton");
                _instance = go.AddComponent<MySingleton>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

## Работа с PlayerPrefs

```csharp
public static class ModConfig
{
    private const string PREFIX = "SilksongManager_";
    
    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(PREFIX + key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public static bool GetBool(string key, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(PREFIX + key, defaultValue ? 1 : 0) == 1;
    }
    
    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(PREFIX + key, value);
        PlayerPrefs.Save();
    }
    
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(PREFIX + key, defaultValue);
    }
}
```

## Связанные файлы

- `Extensions.cs` - Расширения
- `FSMUtility.cs` - Утилиты FSM
- `PlayMakerUtils.cs` - Утилиты PlayMaker
