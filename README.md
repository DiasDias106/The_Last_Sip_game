<h1 align="center">🍷 The Last Sip</h1>

<p align="center">
  <i>A 3D action-adventure game built in Unity 6. From a rain-soaked city alley to the depths of a cursed dungeon, fight your way through traps, monsters and bosses, one sip at a time.</i>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.4.0f1-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6"/>
  <img src="https://img.shields.io/badge/URP-17-1a1b27?style=for-the-badge&logo=unity&logoColor=white" alt="URP"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/Input%20System-1a1b27?style=for-the-badge" alt="Input System"/>
  <img src="https://img.shields.io/badge/Languages-PT%20%7C%20EN-F38020?style=for-the-badge" alt="PT | EN"/>
</p>

---

## 📖 About

**The Last Sip** is a third-person action-adventure game developed by a team of three as a university project. The journey starts in a city alley, at night and by day, and descends into a dungeon full of traps, puzzles and enemies. Wine is the core theme: it heals you, it hides truths, and it ties the story together.

> *"Wine hides truths the mind does not want to see."*

## ✨ Features

- ⚔️ **Third-person combat**: light and heavy attacks, defending, rolling, jumping and running
- 👹 **Boss fights**, each with its own controller and attack patterns: Minotaur, Demon, Drakonit, Golem, Lava Golem, Knight, Ogre, and a **Paladin final boss**
- 🏰 **Multiple levels**: city alley (day and night), dungeon spawn, three dungeon levels and a boss room
- 🪤 **Traps & hazards**: arrow, blade and spike traps, collapsing platforms and lava with respawn
- 🧩 **Puzzles**: lever-based puzzle in Level 2, doors, portals and sealed boss entrances
- 🛒 **Merchant / tavern shop**: buy food and wine with the coins you earn along the way (bosses drop big rewards) (up to 5 of each item)
- 🧪 **Healing items**: bread, cheese, stew, roast meat, wines and the legendary *divine ruby drink*
- 💬 **Dialogue system** with audio, periodic and respawn dialogue, plus **intro cinematics**
- 💾 **Save slots with autosave**
- 🎛️ **Fully rebindable controls** and an in-game controls tutorial scene
- 🌍 **Portuguese & English** localisation
- 🎵 Per-scene music, loading screens, fades, a pause menu and scrolling credits

## 🎮 Controls (default)

| Action | Key |
|--------|-----|
| Move | <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> |
| Run | <kbd>Left Shift</kbd> |
| Jump | <kbd>Space</kbd> |
| Roll | <kbd>C</kbd> |
| Attack | <kbd>J</kbd> / Left mouse |
| Heavy attack | <kbd>X</kbd> / Right mouse |
| Defend | <kbd>R</kbd> |
| Interact | <kbd>F</kbd> |
| Pause | <kbd>Esc</kbd> |

All keys can be remapped in the **controls menu**.

## 🗺️ Scenes

| Scene | Description |
|-------|-------------|
| `MainMenuScene` | Main menu, save slots, options and credits |
| `new_game_controls` | Controls tutorial shown when starting a new game |
| `beco_noite` / `beco_dia` | The city alley, by night and by day |
| `masmorra_spawn` | Dungeon entrance and hub |
| `level_1` → `level_3` | Dungeon levels |
| `boss_room` | Final boss arena |

## 🚀 Getting Started

### Requirements
- **Unity 6000.4.0f1** (via [Unity Hub](https://unity.com/download))
- **[Git LFS](https://git-lfs.com/)**, because 3D models (`.glb`) are stored with LFS
- Several GB of free disk space for the project and the generated `Library/` folder

### Running the project
```bash
git lfs install
git clone https://github.com/DiasDias106/The_Last_Sip_game.git
```
1. In **Unity Hub**, choose **Add**, then **Add project from disk**, and select the cloned folder.
2. Wait for the first import to finish (it can take several minutes).
3. Open `Assets/Scenes/MainMenuScene.unity` and press **Play** ▶️.

### Building
**File › Build Profiles**: all game scenes are already registered in the build list, in play order.

## 📁 Project Structure

```
Assets/
├── Scenes/             # Game scenes (menu, alley, dungeon levels, boss room)
├── Scripts/            # Gameplay: player, combat, traps, interaction, shop, dialogue, saving, music
├── Bosses/             # Boss models, animators, prefabs and controllers
├── Enemies/ · NPC/     # Enemies, merchant and NPCs
├── UI/                 # Main menu, pause menu, HUD and loading screens
├── Dialogos/           # Dialogue audio
├── Audio/              # Music and sound effects
├── Prefabs/            # Reusable game objects
└── (asset packs)       # Third-party environments, characters and animations
Packages/               # Unity package manifest
ProjectSettings/        # Unity project configuration
```

## 🛠️ Built With

- **Unity 6** with the **Universal Render Pipeline**
- **C#** gameplay scripts
- **Input System** for rebindable input
- **AI Navigation** (NavMesh) for enemy and boss movement
- **TextMesh Pro** and **Timeline**

## 👥 Team

| | Member |
|---|--------|
| 🧑‍💻 | [Francisco Dias](https://github.com/DiasDias106) |
| 🧑‍💻 | [Mário](https://github.com/Mario-Bicho) |
| 🧑‍💻 | [Ricardo Fernandes](https://github.com/Ricard0Fernandes) |

## 🙏 Third-Party Assets

This project uses third-party assets, including: *FANTASTIC – Dungeon Pack* (Tidal Flask Studios), *RPG Character Mecanim Animation Pack FREE* and *Super Character Controller* (Explosive LLC), *Human Animations FREE* (Kevin Iglesias), MoCap Central animations, *Skeleton* (SazenGames), *Free Scavenger*, and various boss models.

> ⚠️ Third-party assets belong to their respective authors and are subject to their own licenses (e.g. the Unity Asset Store EULA). They are **not** licensed for redistribution through this repository, which should stay **private**.
