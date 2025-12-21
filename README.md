# Silksong Manager

Debug and utility mod for Hollow Knight: Silksong.

## Author
- **Name:** Catalyst
- **Email:** catalyst@kyokai.ru
- **Telegram:** @Catalyst_Kyokai

## Requirements
- BepInEx 5.x or later
- Hollow Knight: Silksong

## Installation
1. Install BepInEx to your game folder
2. Copy `SilksongManager.dll` to `BepInEx/plugins/SilksongManager/`
3. Launch the game

## Features

### Debug Menu (F1)
Press F1 to open the debug menu with multiple tabs:
- **Player** - Health, silk, invincibility, noclip, abilities
- **Currency** - Geo and shards management
- **World** - Scene transitions, position save/load, game speed
- **Tools** - Unlock and manage tools/crests
- **Enemies** - Kill, damage, freeze enemies
- **Debug** - Debug information and states

### Hotkeys
| Key | Action |
|-----|--------|
| F1 | Toggle Debug Menu |
| F2 | Quick Heal |
| F3 | Refill Silk |
| F4 | Toggle Invincibility |
| F5 | Add 1000 Geo |
| F6 | Toggle Infinite Jumps |
| F7 | Toggle Noclip |
| F9 | Save Position |
| F10 | Load Position |

## Building
1. Set the game path in `Directory.Build.props`
2. Run `dotnet build`

## Project Structure
```
silksong_manager/
├── Plugin.cs              # Main plugin entry point
├── PluginInfo.cs          # Plugin metadata
├── PluginConfig.cs        # Configuration settings
├── Player/
│   └── PlayerActions.cs   # Player-related actions
├── Currency/
│   └── CurrencyActions.cs # Currency management
├── World/
│   └── WorldActions.cs    # World/scene actions
├── Tools/
│   └── ToolActions.cs     # Tools and crests
├── Enemies/
│   └── EnemyActions.cs    # Enemy management
└── DebugMenu/
    └── DebugMenuController.cs # Debug UI
```

## License
MIT License
