# Architecture

## Overview

The project currently contains a Unity starter scene and an asset folder scaffold. The structure follows mousestaurant; gameplay systems have not been implemented.

## Structure

- `Assets/InternalAssets/Scenes` — game scenes.
- `Assets/InternalAssets/Scripts/App` — future startup and scene flow code.
- `Assets/InternalAssets/Scripts/App/Editor` — editor-only tools.
- `Assets/InternalAssets/Scripts/Gameplay/Actor` — future actor behavior.
- `Assets/InternalAssets/Scripts/UI` — UI behavior.
- `Assets/InternalAssets/Scripts/Shared` — shared helpers.
- `Assets/InternalAssets/Prefabs` — actors, application objects, architecture, holdable objects, props, and UI.
- `Assets/InternalAssets/Input` — input actions.
- `Assets/Settings` — render pipeline configuration.

## Scene Bootstrap

TODO: Define the entry point, scene transitions, and runtime system ownership when the first gameplay flow is implemented.

## Boundaries

Keep input, actor behavior, interaction logic, and UI responsibilities separate. Add gameplay-specific modules when needed; restaurant and networking systems from mousestaurant are not part of this scaffold.
