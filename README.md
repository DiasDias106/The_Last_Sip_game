<h1 align="center">🍷 The Last Sip</h1>

<p align="center">
  <i>A dark-fantasy dungeon game built with Unity 6 and the Universal Render Pipeline.</i>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.4.0f1-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6"/>
  <img src="https://img.shields.io/badge/Render%20Pipeline-URP%2017-1a1b27?style=for-the-badge&logo=unity&logoColor=white" alt="URP"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/status-early%20development-F38020?style=for-the-badge" alt="Status"/>
</p>

---

## 📖 About

**The Last Sip** is a 3D dungeon game in early development. The current build sets up the foundation of the world: a modular stone dungeon (*masmorra*), atmospheric lighting with URP post-processing, and a 4K HDR night-sky skybox.

> 🚧 The project is at the prototype stage: the environment and rendering pipeline are in place, and gameplay systems are next.

## ✨ Current Features

- 🏰 **Modular dungeon spawn area** built from a high-quality modular environment kit
- 🌌 **4K HDRI night sky** used as the scene skybox
- 💡 **URP lighting & post-processing** through a global volume profile
- 🖥️ **PC and mobile render profiles** (separate URP assets and renderers for each)
- 🎮 **New Input System** with a pre-configured action map (`InputSystem_Actions`)

## 🗺️ Roadmap

- [ ] Player controller (movement, camera, interaction)
- [ ] Core gameplay loop
- [ ] Enemies & AI navigation (AI Navigation package already included)
- [ ] UI: main menu, HUD, pause screen
- [ ] Audio & ambience
- [ ] Additional dungeon levels

## 🚀 Getting Started

### Requirements
- **Unity 6000.4.0f1** (or a compatible Unity 6 version), installed via [Unity Hub](https://unity.com/download)
- ~1 GB of free disk space (the first import generates the `Library/` folder)

### Running the project
```bash
git clone https://github.com/DiasDias106/The_Last_Sip_game.git
```
1. Open **Unity Hub**, then **Add**, then **Add project from disk**, and select the cloned folder.
2. Wait for the first import to finish (it can take a few minutes).
3. Open `Assets/Scenes/masmorra_spawn.unity` and press **Play** ▶️.

## 📁 Project Structure

```
Assets/
├── Fantastic Dungeon Pack/   # Third-party modular dungeon environment (models, textures, prefabs)
├── Scenes/
│   └── masmorra_spawn.unity  # Main scene: dungeon spawn area
├── Settings/                 # URP assets, renderers and volume profiles (PC + Mobile)
├── Texturas/Nightsky/        # Night-sky HDRI and skybox material
└── InputSystem_Actions.inputactions
Packages/                     # Unity package manifest
ProjectSettings/              # Unity project configuration
```

## 🛠️ Built With

| Tool | Purpose |
|------|---------|
| [Unity 6](https://unity.com/) | Game engine |
| Universal Render Pipeline | Rendering, lighting and post-processing |
| Input System | Player input handling |
| AI Navigation | NavMesh for future enemy AI |

## 🙏 Credits

- **Environment art:** *FANTASTIC – Dungeon Pack* by [Tidal Flask Studios](https://www.tidalflask.com), from the Unity Asset Store
- **Night sky HDRI:** *NightSkyHDRI014* from [ambientCG](https://ambientcg.com) (CC0)

> ⚠️ Third-party assets in this repository belong to their respective authors and are subject to their own licenses. They are **not** covered by this project's code and may not be redistributed separately.

---

<p align="center">Made by <a href="https://github.com/DiasDias106">Francisco Dias</a></p>
