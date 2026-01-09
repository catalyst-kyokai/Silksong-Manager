# 🕷️ Silksong Manager

> **The Ultimate Debug & Utility Suite for Hollow Knight: Silksong**  
> *Master your environment. Analyze mechanics. Control time.*

---

## 🌟 Overview

**Silksong Manager** is not just a mod; it is a comprehensive command center for *Hollow Knight: Silksong*. Designed for speedrunners, modders, and deep-dive analysts, it provides unprecedented control over the game engine. From manipulating time scales to dissecting enemy AI states, managing inventory to visualizing collision geometry—if it exists in the game, you can control it.

**Silksong Manager incompatible with "Pantheon Of Pharloom" mod. It also may be incompatible with any other game states changing mods.**

Current Version: **v1.0.0.2**

---

## ⚡ Core Systems

### ⏳ Temporal Manipulation (Speed Control)
Defy the game's internal clock. Silksong Manager completely rewrites the engine's time management logic.
*   **True Global Speed**: Patches `TimeManager` and `GameManager` to ensure your speed settings persist through cutscenes, parries, and boss deaths.
*   **Independent Scaling**: Control player and enemy speeds independently. Practice boss patterns in slow motion while keeping your character at full speed.
*   **Freeze Frame Immunity**: The flow of time obeys *you*, not the game events.

### 💾 State Preservation (SaveStates)
A robust "Time Machine" for your save files.
*   **Deep State Capture**: Snapshots physics, FSM states (enemy logic), health, silk, and position data instantly.
*   **Perfect Restoration**: Restores the world exactly as it was. Fixes physics glitches, resets enemy AI correctly, and refreshes the HUD instantly.
*   **Safety Protocols**: Includes automatic collision re-detection and physics stabilization to prevent soft-locks upon loading.

### 🎒 Inventory Management
Complete mastery over your connection to the song.
*   **Full Arsenal Access**: Add or remove abilities, tools, and items at will.
*   **Crest & Rosary Control**: manipulate your currency and upgrade materials instantly.
*   **Loadout Experimentation**: Test any charm build or tool combination immediately without grinding.

### 👁️ Analysis & Debugging
See the world as the developers do.
*   **Hitbox Visualization**: Render player, enemy, and terrain colliders in real-time. Understand exactly why you took damage or missed a hit.
*   **Noclip & God Mode**: Glide through walls to explore out-of-bounds secrets. Toggle invincibility for stress-free testing.
*   **Console & Logging**: Detailed output for tracking game events and debugging mod conflicts.

---

## 🎮 Controls & Interface

Access the **Debug Menu** at any time by pressing **F1**.

### ⌨️ Default Hotkeys

| Category | Action | Keybind |
| :--- | :--- | :--- |
| **General** | Toggle Menu | `F1` |
| | Toggle Console | `F2` |
| | Reload Scene | `Ctrl + R` |
| **Movement** | Toggle Noclip | `N` |
| | Save Position | `Z` |
| | Load Position | `X` |
| **Combat** | Toggle Invincibility | `I` |
| | Instant Full Health | `H` |
| | Kill All Enemies | `K` |
| | Infinite Silk | `U` |
| **Time** | Slow Down | `[` |
| | Speed Up | `]` |
| | Reset Speed | `\` |
| **SaveState** | Quick Save State | `F5` |
| | Quick Load State | `F6` |
| **Debug** | Show Hitboxes | `Ctrl + B` |

> *Note: All keybinds are fully remappable via the **Keybinds** tab in the Debug Menu.*

---

## �️ Installation

1.  **Prerequisites**: Ensure you have a standard **BepInEx** modding setup for *Hollow Knight: Silksong*.
2.  **Download**: Get the latest `SilksongManager.dll` or `BepInEx.zip` from the [Releases](https://github.com/catalyst-kyokai/Silksong-Manager/releases) page.
3.  **Install**: Drop the `.dll` file into your `Silksong/BepInEx/plugins/` folder (or unpack BepInEx.zip into your game folder).
4.  **Launch**: Start the game. The mod terminal should appear, confirming successful injection.

---

## 🧩 Advanced Features

### Physics Stabilization
The SaveState system uses a sophisticated "dummy scene" technique to ensure physics engines are reset correctly between loads, preventing the common "floating enemy" or "broken collision" bugs found in simpler mods.

### FSM Injection
We don't just restore variables; we inject directly into PlayMaker FSMs. This ensures that complex enemy behaviors (like the Bone Bottom crawlers or Boss phase transitions) resume naturally from where they left off.

### Seamless HUD
The HUD refresh system mimics the game's native UI update triggers, ensuring that your health bar and silk spool always reflect reality, even after complex state manipulations.

---

<div align="center">

**Created by Catalyst**  
*Code is Art. Control is Absolute.*

[Report Issues](https://github.com/catalyst-kyokai/Silksong-Manager/issues) • [Source Code](https://github.com/catalyst-kyokai/Silksong-Manager)

</div>
