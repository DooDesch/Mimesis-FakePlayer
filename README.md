# MIMESIS - FakePlayers

> Spawns configurable fake players into your Mimesis session so a solo host can test multiplayer mods without needing real friends to join.

![Version](https://img.shields.io/badge/version-1.1.0-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![Status](https://img.shields.io/badge/status-working-brightgreen)

FakePlayers injects dummy player sessions right after you register as the host, so you can exercise player-count and session logic on your own machine. It pairs naturally with a player-limit mod such as MorePlayers (formerly TooManyPlayers): raise that mod's cap, set how many fakes you want here, and start a session to test the higher limit.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Limitations](#limitations)
- [Compatibility](#compatibility)
- [Building (developers)](#building-developers)
- [Credits / License](#credits--license)

---

## Features

- Injects fake players once, automatically, via a Harmony Postfix on the host registration path (`VWorld.RegistPlayer`) - only after your own host registration succeeds.
- Creates virtual player sessions through the game's `VirtualAcceptSession` + `SessionContext` system (the path used for local/offline play). Each fake gets a unique SteamID, a unique player UID, and a name like `FakePlayer1`, `FakePlayer2`, and so on.
- Lets fake players bypass the default 4-player room cap: a Prefix on `IVroom.CanEnterChannel` returns success for fake-player UIDs regardless of current room count, while real players still hit the normal limit.
- Provides a spawn-point fallback (Prefix/Postfix on `IVroom.GetPlayerStartPoint`) that synthesizes a default spawn at origin when the room has none, so fake-player creation does not fail.
- Patches `VirtualAcceptSession.get_ID` to return a unique session ID per fake session via an internal mapping table, avoiding ID collisions between simultaneous fake sessions.
- Logs extensively across the join pipeline (room entry, queue processing, `SessionContext.CreatePlayer`, enter-room failures) so you can trace whether fakes actually entered the room.
- Cleanly removes fake players and clears the session-ID mapping on teardown.

---

## Requirements

| Component | Version |
|-----------|---------|
| MIMESIS | 0.3.0 (current Steam build) |
| MelonLoader | 0.7.3+ |

---

## Installation

- **Recommended:** install through a Thunderstore mod manager (r2modman / Gale) and let it resolve MelonLoader for you.
- **Manual:**
  1. Download the latest `FakePlayers.dll` from the [releases page](../../releases).
  2. Drop it into `MIMESIS/Mods/`.
  3. Launch the game once to generate the config file at `UserData/MelonPreferences.cfg`.

No extra assets are required - only `FakePlayers.dll`.

---

## Configuration

Stored in `UserData/MelonPreferences.cfg` under the `FakePlayers` category.

| Option | Description | Default | Values/Range |
|--------|-------------|---------|--------------|
| `Enabled` | Enable or disable fake player emulation for testing (display name "Enable Fake Players"). When `false`, all patches early-return and no fake players are created. | `true` | `true` / `false` |
| `FakePlayerCount` | Number of fake players to spawn (display name "Fake Player Count"). Default 3 gives a total of 4 players including yourself (the vanilla cap). Clamped on load: below 0 becomes 0, above 32762 becomes 32762. A value of 0 (or less) creates no fake players. | `3` | `0` - `32762` (clamped on load) |

---

## Usage

No keybinds and no in-game UI - FakePlayers works automatically:

1. Enable the mod and set `FakePlayerCount` in `UserData/MelonPreferences.cfg` (or via a config UI).
2. Host / start a session. Fake players are injected once, right after your host registration.

They appear in the session and count toward the player limit, but they are registered sessions only: they do not move, respond to events, or interact with the world.

To test higher player counts, pair FakePlayers with a player-limit mod (the original companion is named TooManyPlayers; in this ecosystem it ships on Thunderstore as MorePlayers). Raise that mod's max players, set `FakePlayerCount` accordingly (for example `7` for 8 total), then start a session to exercise the higher limit.

This is a host-only testing tool: it injects players on the local host machine, so run it on the machine that is hosting the test session.

---

## Limitations

- Fake players are session-registered only - they will not spawn, move, or interact in-game.
- They do not respond to game events.
- Some multiplayer features may not behave correctly with fake players present.
- Intended for testing player-count and session logic, not for normal play.

---

## Compatibility

Built for Mimesis 0.3.0 / MelonLoader 0.7.3. This is a multiplayer/host testing tool: it must be run on the host machine, since it injects players locally during host registration.

---

## Building (developers)

```
dotnet build -c Release
```

Standalone mod (no MimicAPI). References resolve from `Workspace/lib/game` (game DLLs) and `Workspace/lib/melonloader`. Target framework is `netstandard2.1`. The PostBuild step copies `FakePlayers.dll` into `MIMESIS/Mods/`.

---

## Credits / License

Author: DooDesch. Provided as-is under the MIT License. Contributions are welcome via pull requests.

Repository: <https://github.com/DooDesch/Mimesis-FakePlayers>
