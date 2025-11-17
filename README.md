# Mimesis FakePlayers

FakePlayers is a MelonLoader mod for Mimesis that emulates additional players for testing purposes. This is especially useful for testing multiplayer mods like TooManyPlayers when you don't have friends available to join your game.

## Requirements
- Mimesis (latest Steam build)
- MelonLoader 0.7.1+

## Installation
1. Download the latest `FakePlayers.dll` release.
2. Drop it into `Mimesis/MelonLoader/Mods`.
3. Launch the game once so the config file is generated.

## Configuration
Adjustment values live in `UserData/MelonPreferences.cfg`.

Key options in the `FakePlayers` category:

- `Enabled`: Enable or disable fake player emulation (default: `true`).
- `FakePlayerCount`: Number of fake players to spawn (default: `3`, minimum: `0`, maximum: `32762`).

**Note**: The default of 3 fake players means you'll have a total of 4 players (including yourself), which matches the default game limit. If you're using TooManyPlayers mod, you can increase this value to test with more players.

## How It Works

FakePlayers creates virtual player sessions using the game's `VirtualAcceptSession` system, which is normally used for local/offline play. The mod:

1. Intercepts the host player registration in `VWorld.RegistPlayer`
2. Creates fake `SessionContext` instances with unique Steam IDs and player UIDs
3. Registers these fake players in the game session
4. Adds them to the session manager

The fake players will appear in the game session and count towards the player limit, allowing you to test multiplayer functionality even in single-player mode.

## Usage with TooManyPlayers

This mod is designed to work alongside the TooManyPlayers mod:

1. Set `TooManyPlayers.MaxPlayers` to your desired maximum (e.g., 8, 16, etc.)
2. Set `FakePlayers.FakePlayerCount` to the number of fake players you want (e.g., 7 for a total of 8 players)
3. Launch the game and start a session

The fake players will be registered in the game session, allowing you to test the increased player limits.

## Limitations

- Fake players are registered in the session but won't actually spawn in-game or move around
- They won't respond to game events or interact with the world
- This mod is primarily for testing player count limits and session management
- Some multiplayer features may not work correctly with fake players

## Development
- Core entry: `Core.cs`
- Preferences: `Config/FakePlayersPreferences.cs`
- Manager: `Managers/FakePlayerManager.cs`
- Harmony patches: `Patches/*.cs`

## License
Provided as-is under the MIT License. Contributions welcome via PR.

