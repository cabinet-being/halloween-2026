<h1 align="center"> 🎃 halloween-2026 🎃 </h1>

<p align="center">
  <img src="https://img.shields.io/badge/unity-6000.4.7f1-blue" alt="Unity 6000.4.7f1">
</p>

A Unity project for Halloween 2026. Currently at the initial setup stage, with a project structure based on mousestaurant.

## Overview

The repository contains the starter Unity scene, render pipeline settings, input actions, and folders for future game assets and code. The game concept and core loop are still to be defined.

## Features

- Universal Render Pipeline configuration.
- Starter Input System actions.
- Dedicated folders for project assets and documentation.
- No custom gameplay implemented yet.

## Screenshots

TODO: Add screenshots once the first playable prototype is ready.

## Installation

1. Install Unity Editor `6000.4.7f1` through Unity Hub.
2. Add this repository folder as a project and open it.
3. Allow Unity to restore the packages declared in `Packages/manifest.json`.

## Usage

Open `Assets/InternalAssets/Scenes/SCN_Main.unity` and press Play to view the starter scene.

With Unity CLI installed, open the project from the repository root:

```bash
unity open .
```

## Project Structure

```text
halloween-2026/
├── Assets/
│   ├── InternalAssets/
│   │   ├── Input/
│   │   ├── Models/
│   │   │   ├── Materials/
│   │   │   └── Textures/
│   │   ├── PhysicsMaterials/
│   │   ├── Prefabs/
│   │   │   ├── Actors/
│   │   │   ├── App/
│   │   │   ├── Architecture/
│   │   │   ├── Holdable/
│   │   │   ├── Props/
│   │   │   └── UI/
│   │   ├── Scenes/
│   │   │   └── SCN_Main.unity
│   │   ├── Scripts/
│   │   │   ├── App/Editor/
│   │   │   ├── Gameplay/Actor/
│   │   │   ├── Shared/
│   │   │   └── UI/
│   │   └── Sprites/UI/
│   ├── Settings/
│   └── TutorialInfo/
├── Docs/
│   ├── Architecture.md
│   ├── Assets.md
│   ├── CodeStyle.md
│   ├── Gameplay.md
│   └── NamingConvention.md
├── Packages/
├── ProjectSettings/
└── README.md
```

## Configuration

Project settings live in `ProjectSettings/`; package dependencies and their resolved versions live in `Packages/`. Change dependencies through Unity Package Manager.

## Development

- Keep project-owned assets in `Assets/InternalAssets/`.
- Commit Unity `.meta` files together with their assets; preserve them when moving assets.
- Empty asset folders contain `.gitkeep` files so Git preserves the initial structure.
- Generated caches, builds, IDE files, and local agent settings are ignored.
- Follow [code style](Docs/CodeStyle.md) and [naming conventions](Docs/NamingConvention.md).
- Update the relevant document in `Docs/` when adding a system or changing a gameplay flow.

## Roadmap

```mermaid
timeline
    Concept
      : Define game concept and core loop
    Prototype
      : Build first playable scene
      : Implement core interaction
    Presentation
      : Add art, sound, and UI
    Release
      : Playtest and fix issues
      : Prepare a build
```

These are planning placeholders; gameplay and release scope are not yet decided.

## Development Board

```mermaid
kanban
    backlog[Backlog]
      art["Define art and audio direction"]
      build["Choose target platform and build workflow"]
    todo[Todo]
      concept["Define game concept and core loop"]
    inProgress[In Progress]
    testing[Testing]
    done[Done]
      structure["Create base project structure"]
      docs["Outline README and project documentation"]
      git["Initialize Git and Unity ignore rules"]
```

## Contributing

Keep changes focused, test affected scenes in Unity, and update documentation alongside implementation.

## License

TODO: Choose a license. No license file has been added yet.

## Credits

Project structure and documentation layout adapted from mousestaurant. Add asset authors and license details to [Docs/Assets.md](Docs/Assets.md) as assets are introduced.
