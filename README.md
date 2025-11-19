# MIMESIS - FakePlayers

A MelonLoader mod for Mimesis that emulates additional players for testing purposes. This is especially useful for testing multiplayer mods like TooManyPlayers when you don't have friends available to join your game.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.1+-green)
![Status](https://img.shields.io/badge/status-working-brightgreen)

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [How It Works](#how-it-works)
- [Usage with TooManyPlayers](#usage-with-toomanyplayers)
- [Limitations](#limitations)
- [Development](#development)
- [License](#license)

---

## Requirements

| Component | Version |
|-----------|---------|
| **Mimesis** | Latest Steam build |
| **MelonLoader** | 0.7.1 or higher |

---

## Installation

1. Download the latest `FakePlayers.dll` release from the [releases page](../../releases)
2. Place the file into your Mimesis mods directory:
   ```
   Mimesis/MelonLoader/Mods/FakePlayers.dll
   ```
3. Launch the game once to generate the configuration file

> **Note:** The configuration file will be created automatically on first launch at `UserData/MelonPreferences.cfg`

---

## Configuration

Configuration values are stored in `UserData/MelonPreferences.cfg` under the `FakePlayers` category.

### Available Options

| Option | Description | Default | Range |
|--------|-------------|---------|-------|
| `Enabled` | Enable or disable fake player emulation | `true` | `true` / `false` |
| `FakePlayerCount` | Number of fake players to spawn | `3` | `0` - `32762` |

> **Note:** The default of 3 fake players means you'll have a total of 4 players (including yourself), which matches the default game limit. If you're using TooManyPlayers mod, you can increase this value to test with more players.

---

## How It Works

FakePlayers creates virtual player sessions using the game's `VirtualAcceptSession` system, which is normally used for local/offline play.

### Process Flow

1. **Intercepts** the host player registration in `VWorld.RegistPlayer`
2. **Creates** fake `SessionContext` instances with unique Steam IDs and player UIDs
3. **Registers** these fake players in the game session
4. **Adds** them to the session manager

The fake players will appear in the game session and count towards the player limit, allowing you to test multiplayer functionality even in single-player mode.

---

## Usage with TooManyPlayers

This mod is designed to work alongside the TooManyPlayers mod for comprehensive testing:

### Setup Steps

1. Set `TooManyPlayers.MaxPlayers` to your desired maximum (e.g., `8`, `16`, etc.)
2. Set `FakePlayers.FakePlayerCount` to the number of fake players you want (e.g., `7` for a total of 8 players)
3. Launch the game and start a session

The fake players will be registered in the game session, allowing you to test the increased player limits without needing additional real players.

---

## Limitations

- Fake players are registered in the session but won't actually spawn in-game or move around
- They won't respond to game events or interact with the world
- This mod is primarily for testing player count limits and session management
- Some multiplayer features may not work correctly with fake players

---

## Development

### Project Structure

```
FakePlayers/
├── Core.cs                          # Main entry point
├── Config/
│   └── FakePlayersPreferences.cs    # Configuration management
├── Managers/
│   └── FakePlayerManager.cs         # Fake player management logic
└── Patches/
    └── VirtualAcceptSessionPatches.cs # Harmony patches
```

### Key Files

- **`Core.cs`** - Core entry point and mod initialization
- **`Config/FakePlayersPreferences.cs`** - Preference management and configuration
- **`Managers/FakePlayerManager.cs`** - Core logic for creating and managing fake players
- **`Patches/*.cs`** - Harmony patches for intercepting player registration

---

## License

This project is provided as-is under the **MIT License**. Contributions are welcome via pull requests.

---
