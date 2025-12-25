# 🕷️ Silksong Manager v1.0.0.2

> **The ultimate debug and utility mod for Hollow Knight: Silksong.**  
> *Manage your game state, debug mechanics, visualize hitboxes, and customize your experience.*

![Version](https://img.shields.io/badge/version-1.0.0-blue) ![License](https://img.shields.io/badge/license-BSD%203--Clause-green) ![Author](https://img.shields.io/badge/author-Catalyst-purple)

---

## ✨ Features

### 🎮 Player Control
- **Invincibility**: Toggle god mode.
- **Infinite Resources**: Infinite Silk, Health, and Jumps.
- **Noclip**: Move freely through walls and terrain.
- **Tools Management**: Unlock and manage all tools and crests instantly.

### 🌍 World & State
- **Save/Load State**: Save your precise position and game state, then load it instantly (great for practice!).
- **Scene Management**: Reload scenes or transition to specific levels.
- **Game Speed**: Slow down or speed up the game for precise testing.
- **Benchmarks**: Monitor FPS and memory usage.

### 👁️ Visuals & Debugging
- **Hitbox Visualizer**: View vivid, color-coded hitboxes for:
    - 🟩 Player
    - 🟥 Enemies
    - 🟨 Attacks
    - 🟦 Terrain & Triggers
    - *And more!* (Toggle individual layers in the menu)
- **Debug Info**: View detailed internal game states.
- **Enemy Control**: Kill all enemies, freeze AI, or damage specific targets.

### ⌨️ Scrollable Keybinds
- **Fully Customizable**: Rebind any mod action to your preferred keyboard key.
- **New UI**: Beautiful, scrollable keybinds menu that matches the game's native aesthetic.
- **Visual Feedback**: Real-time notifications when you trigger mod actions (toggable).

---

## 🚀 Installation


1. **Install BepInEx**: Ensure you have BepInEx 5.x installed in your *Hollow Knight: Silksong* directory.
2. **Download**:
   - Go to the [Releases Setup](https://github.com/catalyst-kyokai/Silksong-Manager/releases) page.
   - Download the latest `SilksongManager.zip` (recommended) or `SilksongManager.dll`.
3. **Install Mod**:
   - **Method A (Archive):** Extract the contents of `SilksongManager.zip` directly into your game's root folder.
   - **Method B (Manual):** Place `SilksongManager.dll` into:
     `...\Hollow Knight Silksong\BepInEx\plugins\SilksongManager\`
4. **Launch**: Start the game. The mod is active!

### 🛡️ Verify Integrity (Optional)
To ensure your download is genuine and uncorrupted, compare the SHA256 hash of your file with the one provided in the Release notes.

**Powershell:**
```powershell
Get-FileHash SilksongManager.dll -Algorithm SHA256
```


---

## 📖 Usage

### Opening the Menu
Press **F1** at any time to open the **Debug Menu**. Navigate through tabs to access all features.

### Default Keybinds
| Key | Action |
| :--- | :--- |
| **Num 5** | Toggle Debug Menu |
| **F5** | Save Position (Save State) |
| **F9** | Load Position (Load State) |
| **N** | Toggle Noclip |
| **I** | Toggle Invincibility |
| **J** | Toggle Infinite Jumps |
| **K** | Kill All Enemies |
| **F** | Freeze Enemies |
| **G** | Add Geo |
| **H** | Add Shell Shards |
| **R** | Respawn |
| **F8** | Reload Scene |

*> **Note**: You can change ALL these keys in the Keybinds menu!*

---

## 🛠️ Credits & Contact

**Author:** Catalyst  
**Email:** catalyst@kyokai.ru  
**Telegram:** [@Catalyst_Kyokai](https://t.me/Catalyst_Kyokai)

Licensed under the **BSD 3-Clause License**. See `LICENSE` for details.
