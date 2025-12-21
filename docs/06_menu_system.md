# Документация: Система Меню (Menu System)

## Обзор

Система меню в Silksong построена на:

- **UIManager** - главный менеджер UI
- **MenuScreen** - базовый экран меню
- **MenuButtonList** - списки кнопок
- **MenuSetting** - настройки меню

## UIManager (UIManager.cs)

Файл: `silksong_source_code/UIManager.cs` (~2500 строк)

### Доступ

```csharp
UIManager ui = UIManager.instance;
```

### Состояния UI

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

### Основные методы навигации

| Метод | Описание |
|-------|----------|
| `SetState(UIState state)` | Установить состояние UI |
| `UIGoToMainMenu()` | Перейти в главное меню |
| `UIGoToPauseMenu()` | Открыть меню паузы |
| `UIClosePauseMenu()` | Закрыть меню паузы |
| `UIGoToOptionsMenu()` | Открыть настройки |
| `UIGoToProfileMenu()` | Открыть выбор профиля |
| `UIGoToVideoMenu()` | Настройки видео |
| `UIGoToAudioMenu()` | Настройки аудио |
| `UIGoToControllerMenu()` | Настройки контроллера |
| `UIGoToKeyboardMenu()` | Настройки клавиатуры |
| `UIStartNewGame()` | Начать новую игру |
| `UIContinueGame()` | Продолжить игру |
| `UIGoBack()` | Назад |

### Методы экрана

```csharp
// Fade эффекты
public void FadeScreenIn(float time);
public void FadeScreenOut(float time);
public void BlankScreen();
public void SetScreenBlankerAlpha(float alpha);

// Canvas Groups
public void ShowCanvasGroup(CanvasGroup group);
public void HideCanvasGroup(CanvasGroup group);
public void FadeInCanvasGroup(CanvasGroup group);
public void FadeOutCanvasGroup(CanvasGroup group);
```

## MenuScreen (MenuScreen.cs)

Базовый класс экрана меню.

```csharp
public class MenuScreen : MonoBehaviour
{
    public string screenName;
    public MenuButtonList buttonList;
    public CanvasGroup canvasGroup;
    
    public virtual void Show();
    public virtual void Hide();
    public virtual void OnEnter();
    public virtual void OnExit();
}
```

## MenuButtonList (MenuButtonList.cs)

Список кнопок меню с навигацией.

```csharp
public class MenuButtonList : MonoBehaviour
{
    public List<MenuButton> buttons;
    public int selectedIndex;
    public bool isVertical;
    
    public void AddButton(MenuButton button);
    public void RemoveButton(MenuButton button);
    public void SelectButton(int index);
    public void SelectNext();
    public void SelectPrevious();
    public void Activate();
}
```

## MenuSetting (MenuSetting.cs)

Настройка в меню (слайдер, переключатель и т.д.).

```csharp
public class MenuSetting : MonoBehaviour
{
    public string settingName;
    public SettingType type;
    public float value;
    public float minValue;
    public float maxValue;
    
    public void ApplySetting();
    public void ResetToDefault();
}

public enum SettingType
{
    Toggle,
    Slider,
    Dropdown,
    Button
}
```

## MainMenuState (enum)

```csharp
public enum MainMenuState
{
    LOGO,
    MAIN_MENU,
    OPTIONS,
    PROFILE_SELECT,
    LOADING,
    CREDITS
}
```

## Создание кнопки меню

### MenuButton (UnityEngine.UI)

```csharp
public class MenuButton : Button
{
    public string buttonName;
    public UnityEvent onSubmit;
    public UnityEvent onCancel;
    public UnityEvent onHighlight;
    
    public void Submit();
    public void Cancel();
    public void Highlight();
}
```

### Создание кнопки программно

```csharp
public GameObject CreateMenuButton(string text, UnityAction onClick)
{
    // Создаём объект кнопки
    var buttonGO = new GameObject("MenuButton");
    
    // Добавляем компоненты
    var rectTransform = buttonGO.AddComponent<RectTransform>();
    var button = buttonGO.AddComponent<Button>();
    var image = buttonGO.AddComponent<Image>();
    
    // Настраиваем
    button.onClick.AddListener(onClick);
    
    // Добавляем текст
    var textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform);
    var textComponent = textGO.AddComponent<Text>();
    textComponent.text = text;
    
    return buttonGO;
}
```

## MenuStyles (MenuStyles.cs)

Стили оформления меню.

```csharp
public class MenuStyles : MonoBehaviour
{
    public List<MenuStyle> styles;
    public int currentStyleIndex;
    
    public void ApplyStyle(int index);
    public MenuStyle GetCurrentStyle();
}

public class MenuStyle
{
    public string styleName;
    public Color backgroundColor;
    public Color textColor;
    public Color highlightColor;
    public Font font;
}
```

## Пауза и инвентарь

### Открытие меню паузы

```csharp
public void OpenPauseMenu()
{
    var ui = UIManager.instance;
    var gm = GameManager.instance;
    
    if (gm != null && !gm.IsGamePaused())
    {
        ui.UIGoToPauseMenu();
    }
}
```

### Закрытие меню паузы

```csharp
public void ClosePauseMenu()
{
    var ui = UIManager.instance;
    ui.UIClosePauseMenu();
}
```

## DialogueBox (DialogueBox.cs)

Диалоговое окно.

```csharp
public class DialogueBox : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public Image portrait;
    
    public void Show(string text, Sprite portrait);
    public void Hide();
    public void ShowNextLine();
    public bool IsComplete();
}
```

## DialogueYesNoBox

Диалог с выбором Да/Нет.

```csharp
public class DialogueYesNoBox : MonoBehaviour
{
    public UnityEvent onYes;
    public UnityEvent onNo;
    
    public void Show(string question);
    public void Hide();
}
```

## Примеры использования

### Показать сообщение

```csharp
public void ShowMessage(string text)
{
    var dialogue = FindObjectOfType<DialogueBox>();
    if (dialogue != null)
    {
        dialogue.Show(text, null);
    }
}
```

### Создание простого меню

```csharp
public class CustomMenu : MonoBehaviour
{
    private bool isVisible = false;
    private Rect windowRect = new Rect(100, 100, 300, 400);
    
    void OnGUI()
    {
        if (!isVisible) return;
        
        windowRect = GUI.Window(0, windowRect, DrawWindow, "Custom Menu");
    }
    
    void DrawWindow(int windowId)
    {
        if (GUILayout.Button("Option 1"))
        {
            // Действие 1
        }
        
        if (GUILayout.Button("Option 2"))
        {
            // Действие 2
        }
        
        if (GUILayout.Button("Close"))
        {
            isVisible = false;
        }
        
        GUI.DragWindow();
    }
    
    public void Toggle()
    {
        isVisible = !isVisible;
    }
}
```

## Связанные файлы

- `UIManager.cs` - Главный менеджер UI
- `MenuScreen.cs` - Экран меню
- `MenuButtonList.cs` - Список кнопок
- `MenuSetting.cs` - Настройки
- `MenuStyles.cs` - Стили меню
- `DialogueBox.cs` - Диалоговое окно
- `DialogueYesNoBox.cs` - Диалог Да/Нет
- `GameMenuOptions.cs` - Опции игрового меню
- `MainMenuOptions.cs` - Опции главного меню
