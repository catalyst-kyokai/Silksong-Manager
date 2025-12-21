# Документация: UI и Создание Меню

## Обзор

Эта документация описывает как создавать собственный UI в модах для Silksong:

- **Unity IMGUI** - простой вариант для отладочных меню
- **Unity UI (uGUI)** - полноценный UI как в игре
- **Стилизация** - кастомизация внешнего вида

## Метод 1: Unity IMGUI (OnGUI)

Самый простой способ создания Debug UI.

### Базовая структура

```csharp
public class DebugUI : MonoBehaviour
{
    private bool _isVisible = false;
    private Rect _windowRect = new Rect(20, 20, 300, 400);
    
    void OnGUI()
    {
        if (!_isVisible) return;
        
        _windowRect = GUI.Window(
            12345, // Уникальный ID окна
            _windowRect,
            DrawWindow,
            "Название окна"
        );
    }
    
    void DrawWindow(int windowId)
    {
        // Контент окна
        GUILayout.Label("Текст метки");
        
        if (GUILayout.Button("Кнопка"))
        {
            // Действие при нажатии
        }
        
        // Разрешить перетаскивание
        GUI.DragWindow();
    }
    
    public void Toggle()
    {
        _isVisible = !_isVisible;
    }
}
```

### Элементы IMGUI

```csharp
void DrawWindow(int windowId)
{
    // Метка
    GUILayout.Label("Текст");
    
    // Кнопка
    if (GUILayout.Button("Нажми меня"))
    {
        Debug.Log("Кнопка нажата!");
    }
    
    // Текстовое поле
    string input = GUILayout.TextField(inputText);
    
    // Слайдер
    float value = GUILayout.HorizontalSlider(currentValue, 0f, 100f);
    
    // Чекбокс
    bool isChecked = GUILayout.Toggle(isEnabled, "Включить");
    
    // Выпадающий список
    if (GUILayout.Button(options[selectedIndex]))
    {
        showDropdown = !showDropdown;
    }
    
    // Горизонтальная группа
    GUILayout.BeginHorizontal();
    GUILayout.Button("Кнопка 1");
    GUILayout.Button("Кнопка 2");
    GUILayout.EndHorizontal();
    
    // Вертикальная группа
    GUILayout.BeginVertical("box");
    GUILayout.Label("В рамке");
    GUILayout.EndVertical();
    
    // Пространство
    GUILayout.Space(10);
    GUILayout.FlexibleSpace();
    
    // Скролл
    scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
    // Контент
    GUILayout.EndScrollView();
}
```

### Стилизация IMGUI

```csharp
private GUIStyle _buttonStyle;
private GUIStyle _labelStyle;
private bool _stylesInitialized = false;

void InitStyles()
{
    if (_stylesInitialized) return;
    
    // Кастомная кнопка
    _buttonStyle = new GUIStyle(GUI.skin.button)
    {
        fontSize = 14,
        fontStyle = FontStyle.Bold,
        padding = new RectOffset(10, 10, 5, 5)
    };
    _buttonStyle.normal.textColor = Color.white;
    _buttonStyle.hover.textColor = Color.yellow;
    
    // Кастомная метка
    _labelStyle = new GUIStyle(GUI.skin.label)
    {
        fontSize = 12,
        wordWrap = true,
        alignment = TextAnchor.MiddleLeft
    };
    
    _stylesInitialized = true;
}

void OnGUI()
{
    InitStyles();
    
    if (GUILayout.Button("Стилизованная кнопка", _buttonStyle))
    {
        // Действие
    }
}
```

### Табы в IMGUI

```csharp
private int _selectedTab = 0;
private string[] _tabNames = { "Tab 1", "Tab 2", "Tab 3" };

void DrawWindow(int windowId)
{
    // Рисуем табы
    GUILayout.BeginHorizontal();
    for (int i = 0; i < _tabNames.Length; i++)
    {
        if (GUILayout.Toggle(_selectedTab == i, _tabNames[i], "Button"))
        {
            _selectedTab = i;
        }
    }
    GUILayout.EndHorizontal();
    
    GUILayout.Space(10);
    
    // Контент таба
    switch (_selectedTab)
    {
        case 0: DrawTab1(); break;
        case 1: DrawTab2(); break;
        case 2: DrawTab3(); break;
    }
}
```

## Метод 2: Unity UI (uGUI)

Более сложный, но более красивый способ.

### Создание Canvas

```csharp
public GameObject CreateUICanvas()
{
    // Создаём Canvas
    var canvasGO = new GameObject("ModCanvas");
    var canvas = canvasGO.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 100; // Поверх игрового UI
    
    // Canvas Scaler для адаптивности
    var scaler = canvasGO.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);
    
    // Graphic Raycaster для интерактивности
    canvasGO.AddComponent<GraphicRaycaster>();
    
    // Не уничтожать при смене сцен
    Object.DontDestroyOnLoad(canvasGO);
    
    return canvasGO;
}
```

### Создание Panel

```csharp
public GameObject CreatePanel(Transform parent, Vector2 size)
{
    var panelGO = new GameObject("Panel");
    panelGO.transform.SetParent(parent, false);
    
    var rect = panelGO.AddComponent<RectTransform>();
    rect.sizeDelta = size;
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    
    var image = panelGO.AddComponent<Image>();
    image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    
    return panelGO;
}
```

### Создание кнопки

```csharp
public Button CreateButton(Transform parent, string text, UnityAction onClick)
{
    var buttonGO = new GameObject("Button");
    buttonGO.transform.SetParent(parent, false);
    
    var rect = buttonGO.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(200, 40);
    
    var image = buttonGO.AddComponent<Image>();
    image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    var button = buttonGO.AddComponent<Button>();
    button.targetGraphic = image;
    button.onClick.AddListener(onClick);
    
    // Создаём текст
    var textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform, false);
    
    var textRect = textGO.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.offsetMin = Vector2.zero;
    textRect.offsetMax = Vector2.zero;
    
    var textComponent = textGO.AddComponent<Text>();
    textComponent.text = text;
    textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    textComponent.alignment = TextAnchor.MiddleCenter;
    textComponent.color = Color.white;
    
    return button;
}
```

### Создание текста

```csharp
public Text CreateText(Transform parent, string content)
{
    var textGO = new GameObject("Text");
    textGO.transform.SetParent(parent, false);
    
    var rect = textGO.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(200, 30);
    
    var text = textGO.AddComponent<Text>();
    text.text = content;
    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    text.fontSize = 14;
    text.color = Color.white;
    text.alignment = TextAnchor.MiddleLeft;
    
    return text;
}
```

### Создание слайдера

```csharp
public Slider CreateSlider(Transform parent, float min, float max, float current)
{
    var sliderGO = new GameObject("Slider");
    sliderGO.transform.SetParent(parent, false);
    
    var rect = sliderGO.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(200, 20);
    
    var slider = sliderGO.AddComponent<Slider>();
    slider.minValue = min;
    slider.maxValue = max;
    slider.value = current;
    
    // Background
    var bgGO = new GameObject("Background");
    bgGO.transform.SetParent(sliderGO.transform, false);
    var bgRect = bgGO.AddComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.offsetMin = Vector2.zero;
    bgRect.offsetMax = Vector2.zero;
    var bgImage = bgGO.AddComponent<Image>();
    bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    // Fill Area
    var fillAreaGO = new GameObject("Fill Area");
    fillAreaGO.transform.SetParent(sliderGO.transform, false);
    var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
    fillAreaRect.anchorMin = Vector2.zero;
    fillAreaRect.anchorMax = Vector2.one;
    fillAreaRect.offsetMin = new Vector2(5, 0);
    fillAreaRect.offsetMax = new Vector2(-5, 0);
    
    // Fill
    var fillGO = new GameObject("Fill");
    fillGO.transform.SetParent(fillAreaGO.transform, false);
    var fillRect = fillGO.AddComponent<RectTransform>();
    fillRect.sizeDelta = new Vector2(10, 0);
    var fillImage = fillGO.AddComponent<Image>();
    fillImage.color = Color.green;
    slider.fillRect = fillRect;
    
    // Handle Slide Area
    var handleAreaGO = new GameObject("Handle Slide Area");
    handleAreaGO.transform.SetParent(sliderGO.transform, false);
    var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
    handleAreaRect.anchorMin = Vector2.zero;
    handleAreaRect.anchorMax = Vector2.one;
    handleAreaRect.offsetMin = new Vector2(10, 0);
    handleAreaRect.offsetMax = new Vector2(-10, 0);
    
    // Handle
    var handleGO = new GameObject("Handle");
    handleGO.transform.SetParent(handleAreaGO.transform, false);
    var handleRect = handleGO.AddComponent<RectTransform>();
    handleRect.sizeDelta = new Vector2(20, 0);
    var handleImage = handleGO.AddComponent<Image>();
    handleImage.color = Color.white;
    slider.handleRect = handleRect;
    
    return slider;
}
```

## Layout Groups

```csharp
// Вертикальный лэйаут
public void AddVerticalLayout(GameObject go)
{
    var layout = go.AddComponent<VerticalLayoutGroup>();
    layout.spacing = 10;
    layout.padding = new RectOffset(10, 10, 10, 10);
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;
}

// Горизонтальный лэйаут
public void AddHorizontalLayout(GameObject go)
{
    var layout = go.AddComponent<HorizontalLayoutGroup>();
    layout.spacing = 10;
    layout.padding = new RectOffset(10, 10, 10, 10);
    layout.childForceExpandWidth = false;
    layout.childForceExpandHeight = true;
}

// Grid лэйаут
public void AddGridLayout(GameObject go, int columns)
{
    var layout = go.AddComponent<GridLayoutGroup>();
    layout.cellSize = new Vector2(100, 40);
    layout.spacing = new Vector2(10, 10);
    layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    layout.constraintCount = columns;
}
```

## Скрытие/Показ UI

```csharp
public class ToggleableUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void Show()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }
    
    public void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
    
    public void Toggle()
    {
        if (_canvasGroup.alpha > 0)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
}
```

## Анимация UI

```csharp
public IEnumerator FadeIn(CanvasGroup group, float duration)
{
    float elapsed = 0f;
    group.alpha = 0f;
    
    while (elapsed < duration)
    {
        group.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
        elapsed += Time.unscaledDeltaTime;
        yield return null;
    }
    
    group.alpha = 1f;
}

public IEnumerator FadeOut(CanvasGroup group, float duration)
{
    float elapsed = 0f;
    float startAlpha = group.alpha;
    
    while (elapsed < duration)
    {
        group.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
        elapsed += Time.unscaledDeltaTime;
        yield return null;
    }
    
    group.alpha = 0f;
}
```

## Связанные файлы

- `UIManager.cs` - Менеджер UI игры
- `MenuScreen.cs` - Экраны меню
- `MenuButtonList.cs` - Кнопки меню
- `DialogueBox.cs` - Диалоговые окна
