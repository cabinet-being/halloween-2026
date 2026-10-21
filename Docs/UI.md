# Screens, Settings, and Controls

## Entry Points

- `SCN_Menu` is build scene 0 and the normal entry point.
- `SCN_Main` is the gameplay scene and can also be opened directly in the Editor.
- Settings are a shared overlay, not a separate scene. Opening them during a night preserves the current world and session.

## Screen Flow

```text
Main Menu → Start the night → Gameplay → Dawn results
    ↓                           ↓ Escape       ↓
 Settings                      Pause        Retry / Main Menu
    ↓ Back              Resume / Settings / Main Menu
Main Menu                         ↓ Back
                                Pause
```

All player-facing labels are English. Returning to the main menu abandons an unfinished night's score; previously discovered secrets remain saved. Completed-night records use the existing progress store.

`GameplayScreen` freezes `Time.timeScale`, the game session, and non-music audio during pause. It unlocks the cursor and restores normal time/input on resume or scene exit. Escape inside Settings returns to Pause, and another Escape resumes. Escape during rebinding cancels the pending binding without closing Settings. Losing application focus pauses a running night.

## UI Authoring

UI Toolkit layouts live in `Assets/InternalAssets/UI/`:

- `Menu.uxml` — main menu and decorative ghost/moon artwork.
- `Gameplay.uxml` — HUD, pause, and dawn results.
- `Settings.uxml` — shared audio, camera, and control settings.
- `Theme.uss` — shared colors, sizing, button states, and layout.
- `RuntimeTheme.tss` / `RuntimePanel.asset` — Unity control theme and resolution scaling.

Open the UXML files in UI Builder to edit them visually, or edit their markup and USS directly. Menu and game controllers bind behavior to named elements; the settings view is reused in both scenes. Keyboard focus is visible, and buttons support keyboard navigation.

## Settings

Settings apply immediately and persist through `SettingsService` in the versioned PlayerPrefs key `Halloween2026.Settings.v1`, independently of game progress.

| Setting | Range | Application |
| --- | --- | --- |
| Master volume | 0–100% | `AudioListener.volume`, affecting all audio. |
| Music volume | 0–100% | Music AudioSource volume, additionally scaled by master volume. |
| Mouse sensitivity | 0.1–4.0× | Camera look speed. |
| Field of view | 45–100° | Gameplay camera's vertical FOV, including while paused. |
| Controls | Six rebindable inputs | Forward, backward, left, right, primary, secondary. |

Restore defaults resets both sliders and bindings. The short `NightLoop.wav` is an original generated placeholder to make volume controls audible, not the final soundtrack. `MusicVolumeApplier` marks its source as music and lets it play through pause; ordinary effects respect listener pause.

## Input Actions

The existing `InputSystem_Actions.inputactions` remains the source of truth. No generated C# input wrapper or hardcoded keyboard polling is used. Inspector-assigned `InputActionReference` fields follow the approach in mousestaurant.

| Action | Default | Consumer |
| --- | --- | --- |
| Player/Move | WASD | `GhostController` movement. |
| Player/Look | Mouse delta | Camera yaw and pitch. |
| Player/Attack | Left Mouse | Primary-action event. |
| Player/Interact | Right Mouse | Secondary-action event. |
| System/Pause | Escape | Pause/back, always available. |
| UI actions | Pointer, keyboard navigation | Unity's UI Toolkit event system. |

Primary and secondary inputs currently raise separate `ActionRequested` events and show a brief HUD notice. Possession and other gameplay actions are not implemented here. Movement is a minimal third-person CharacterController on a small blockout street, not the final ghost controller or level.

Rebinding waits for a key/button, rejects duplicate assignments among the six editable controls, reserves Escape, and times out after ten seconds. Changes use Input System binding overrides stored with settings. Future interactions should subscribe to the action events or use the corresponding authored action references.

## Setup and Validation

The scenes are already wired. `Halloween > Create Base Scenes` and `Halloween > Set Up Screens and Controls` provide setup tooling for a fresh scaffold; they preserve existing configured screens.

Tests cover settings validation, conflicting controls, the full menu/settings/game/pause flow, movement/look, cancellation of rebinding, live FOV and volume changes, and settings persistence across scene loads, alongside the original foundation tests.
