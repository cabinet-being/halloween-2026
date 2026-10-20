# Architecture

## Scope

The foundation provides a timed night, scoring, persistent records and secret discoveries, environmental presentation, and a development panel. Player control, possession, NPC behavior, candy collection, and the final HUD are not implemented.

## Composition Root

`GameBootstrapper` owns one `GameSession`, one loaded `PlayerProgress`, and a `ProgressStore`. It loads progress in `Awake`, optionally starts the night in `Start`, and advances it with scaled `Time.deltaTime`. No global singleton is required: scene components receive explicit references to the bootstrapper.

Restart reloads the active scene so future world state resets along with the session. The scene must be included in Build Settings. Do not create multiple bootstrappers in one scene.

## Session and Scoring

`GameSession` is plain C# with states `Ready`, `Running`, `Paused`, and `Completed`. Its normalized `Progress` runs from 0 to 1, and dawn completes the night exactly once. A session cannot start again; create another session or reload the scene.

- `game.Award(ScoreSource.Candy, points)` records an award and its category (`Candy`, `Scare`, `Prank`, `Trick`).
- `game.Session.Changed` exposes clock, score, and state updates; `Completed` signals dawn.
- Awards are accepted only while running. Invalid values and integer overflow are rejected.
- `game.SetPaused(true)` pauses the session clock and rejects rewards. It does not change global `Time.timeScale`; future movement, AI, and interaction systems must obey the session state or add a shared world-pause layer.
- Mechanics own reward eligibility, cooldowns, and one-use rules. The score service does not infer whether two awards describe the same scare or trick.

## Time and Environment

`NightSettings` is a ScriptableObject containing night duration (default 480 seconds), start/end clock hours, sky/ambient/light gradients, moon intensity curve, and the moon arc. `HourAt` handles crossing midnight.

`NightEnvironment` reads session progress and updates ambient/fog colors, camera background, directional moon light, and an optional visible moon mesh. The moon stays at a fixed distance from the camera while traversing its arc. A runtime copy of the skybox is tinted when its shader supports `_SkyTint` or `_Tint`; shared material assets are not modified. The configured foundation scene uses a solid-color background for predictable sky gradients.

This is a single dusk-to-dawn session, not an endless day/night loop. Change settings to tune the arc and colors; add a separate world-clock policy if a future mode needs continuous days.

## Persistence and Gallery

`ProgressStore` writes versioned JSON to `Application.persistentDataPath/progress.json`. Data includes the best score, last completed score, completed-night count, and discovered secret IDs. A lower score never replaces the best score.

- Completed nights save their results once. An unfinished run is not restored or added to the record.
- `game.Discover(id)` accepts only registered IDs during a running session, ignores duplicates, and saves immediately.
- `SecretDefinition` assets hold stable IDs, titles, descriptions, and optional artwork. `game.Secrets` and `game.IsDiscovered(id)` are the API for a future gallery UI. The sample catalog contains `first-secret`.
- Keep released IDs stable. Removed catalog entries remain in the save so reintroducing an entry restores its discovery.
- Writes use a temporary file and replacement; a valid previous save is retained as `.bak`. A damaged primary can recover from the backup.
- If neither copy can load, or the primary belongs to an unsupported version, the game preserves existing files and disables saving for that session. Errors are logged and exposed through `SaveError`.
- Failed writes remain pending and retry at subsequent save points, application pause, destruction, or quit. Restart is blocked while a pending write fails, so it does not silently discard that progress. Persistence is local, with no cloud sync or migrations yet.

## Scene and Development Tools

`Halloween > Set Up Foundation Scene` opens `SCN_Main` and adds the configured foundation once. It creates the default data assets, a moon visual, and the development panel. The menu asks Unity to save unsaved scene edits before switching scenes.

`SessionDebugPanel` provides score buttons, pause/resume, time advance, a sample discovery, and restart. Its rewards are testing values, not gameplay balance. Remove or disable this component for the final HUD.

## Verification

EditMode tests cover completion, pause, score overflow, midnight conversion, persistence round trips, duplicate discoveries, backup recovery, and preservation of unreadable/future saves. Run them with Unity Test Runner using the `Halloween2026.Tests` assembly.
