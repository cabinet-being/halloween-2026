# Screens, Settings, and Controls

## Entry Points

- `SCN_Menu` is build scene 0 and the normal entry point.
- `SCN_Main` is the gameplay scene and can also be opened directly in the Editor.
- Settings are a shared overlay, not a separate scene. Opening them during a night preserves the current world and session.

## Screen Flow

```text
Main Menu → Start the night → Gameplay → Dawn results
    ↓                           ↓ Escape/Start ↓
 Settings                      Pause        Retry / Main Menu
    ↓ Back              Resume / Settings / Main Menu
Main Menu                         ↓ Back
                                Pause
```

All player-facing labels are English. Returning to the main menu abandons an unfinished night's score; previously discovered secrets remain saved. Completed-night records use the existing progress store.

`GameplayScreen` freezes `Time.timeScale`, the game session, and non-music audio during pause. It unlocks the cursor and restores normal time/input on resume or scene exit. Escape or gamepad Start pauses or backs out of Settings; B / Circle also backs out of Settings and resumes from Pause. Escape or B / Circle during rebinding cancels the pending binding without closing Settings. Losing application focus pauses a running night. Disconnecting the active gamepad during a running night pauses it and shows a notice; reconnect the controller or use keyboard and mouse to resume.

## UI Authoring

UI Toolkit layouts live in `Assets/InternalAssets/UI/`:

- `Menu.uxml` — main menu and decorative ghost/moon artwork.
- `Gameplay.uxml` — HUD, pause, and dawn results.
- `Settings.uxml` — shared audio, camera, and control settings.
- `Theme.uss` — shared colors, sizing, button states, and layout.
- `RuntimeTheme.tss` / `RuntimePanel.asset` — Unity control theme and resolution scaling.

Open the UXML files in UI Builder to edit them visually, or edit their markup and USS directly. Menu and game controllers bind behavior to named elements; the settings view is reused in both scenes. Focus is visible, and buttons support keyboard and gamepad navigation. In Settings, Up/Down walks all visible controls in document order, including rows outside the scroll viewport; the list scrolls each focused control into view before navigation reaches the fixed footer. Left/Right retains slider adjustment and horizontal navigation. The header and footer do not shrink into the scroll area. Gamepad A / Cross confirms and B / Circle goes back.

## Settings

Settings apply immediately and persist through `SettingsService` in the versioned PlayerPrefs key `Halloween2026.Settings.v1`, independently of game progress.

| Setting | Range | Application |
| --- | --- | --- |
| Master volume | 0–100% | `AudioListener.volume`, affecting all audio. |
| Music volume | 0–100% | Music AudioSource volume, additionally scaled by master volume. |
| Mouse sensitivity | 0.1–4.0× | Camera look speed with a mouse. |
| Gamepad look sensitivity | 0.1–4.0× | Camera look speed with the right stick. |
| Invert gamepad look Y | On/off | Reverses vertical right-stick look. |
| Move and look deadzones | 0.05–0.9 each | Independent left- and right-stick deadzones. |
| Field of view | 45–100° | Gameplay camera's vertical FOV, including while paused. |
| Keyboard/mouse controls | Six rebindable inputs | Forward, backward, left, right, primary, secondary. |
| Gamepad controls | Two rebindable inputs | Primary and secondary actions. |

Settings has Keyboard & mouse and Gamepad tabs. Opening Settings selects the active device; choosing a tab keeps that tab selected until Settings closes. The selected tab does not change the active input device. **Reset this device** restores only that tab's sensitivity, other gamepad look options where applicable, and editable bindings. **Reset sound & FOV** restores the shared audio and field-of-view values. The short `NightLoop.wav` is an original generated placeholder to make volume controls audible, not the final soundtrack. `MusicVolumeApplier` marks its source as music and lets it play through pause; ordinary effects respect listener pause.

## Input Actions

The existing `InputSystem_Actions.inputactions` remains the source of truth for gameplay controls. No generated C# input wrapper is used. Inspector-assigned `InputActionReference` fields follow the approach in mousestaurant; `SettingsService` also reads device activity to identify the active input device.

| Action | Default | Consumer |
| --- | --- | --- |
| Player/Move | WASD / left stick | `GhostController` movement. |
| Player/Look | Mouse delta / right stick | Camera yaw and pitch. |
| Player/Attack | Left Mouse / RT (R2) | Primary-action event. |
| Player/Interact | Right Mouse / LT (L2) | Secondary-action event. |
| System/Pause | Escape / Start (Options) | Pause/back, always available. |
| UI actions | Pointer, keyboard, gamepad navigation and A / Cross confirm | Unity's UI Toolkit event system. |

Primary and secondary inputs currently raise separate `ActionRequested` events and show a brief HUD notice. Possession and other gameplay actions are not implemented here. Movement is a minimal third-person CharacterController on a small blockout street, not the final ghost controller or level.

The last device with deliberate input becomes active: key presses or mouse buttons/movement select keyboard and mouse; gamepad button presses or a stick moved past the intent threshold select that gamepad. Minor stick drift does not switch devices. The HUD and menu prompts follow the active device. Both keyboard/mouse and gamepad HUD hints show movement, look, and pause only; primary/secondary action hints are not permanently displayed. Contextual interaction prompts are not implemented yet. Movement and look use that device's input and sensitivity; gamepad look can also be inverted and each stick has its own deadzone.

Rebinding waits up to ten seconds for an eligible control and stores Input System binding overrides with settings. Duplicate assignments are rejected within each device's editable controls, so keyboard/mouse and gamepad bindings remain independent. Escape is reserved on keyboard. On gamepad, Start, Select, A / Cross, B / Circle, the D-pad, and both sticks are reserved for menus; B / Circle cancels rebinding. Future interactions should subscribe to the action events or use the corresponding authored action references.

Settings still load from the existing `Halloween2026.Settings.v1` key. Saved settings from before gamepad options were added retain their previous values and bindings; the new gamepad options receive defaults.

## Setup and Validation

The scenes are already wired. `Halloween > Create Base Scenes` and `Halloween > Set Up Screens and Controls` provide setup tooling for a fresh scaffold; they preserve existing configured screens.

Tests cover settings validation and legacy settings migration, conflicting controls, the full menu/settings/game/pause flow, movement/look, cancellation of rebinding, live FOV and volume changes, and settings persistence across scene loads, alongside the original foundation tests. Simulated gamepad input additionally checks D-pad navigation and submit, connection/drift filtering, device-specific reset, interactive rebinding and cancellation, small look deadzones, trigger actions, pause, and recovery through keyboard input after disconnection. EditMode screen tests focus Game View so Unity routes UI navigation to the runtime panel. A constrained-height settings regression checks forward/reverse navigation through both tabs, scrolling lower binding rows into view, footer separation, and horizontal slider adjustment. Physical controller feel and platform-specific button labels still need a hardware playtest.
