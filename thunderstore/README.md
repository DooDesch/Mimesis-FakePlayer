# FakePlayers Mod

Emulates additional players for testing purposes. This is especially useful for testing multiplayer mods like TooManyPlayers when you don't have friends available to join your game.

## Features

- Creates virtual player sessions that count towards the player limit
- Configurable number of fake players (0-32762)
- Works alongside TooManyPlayers mod for testing increased player limits
- Fake players are registered in the game session for testing purposes

## Configuration

- `Enabled`: Enable or disable fake player emulation (default: `true`)
- `FakePlayerCount`: Number of fake players to spawn (default: `3`, minimum: `0`, maximum: `32762`)

## Installation

1. Install via Thunderstore Mod Manager
2. Or manually download and extract to `Mimesis/MelonLoader/Mods`

## Usage with TooManyPlayers

1. Set `TooManyPlayers.MaxPlayers` to your desired maximum (e.g., 8, 16, etc.)
2. Set `FakePlayers.FakePlayerCount` to the number of fake players you want (e.g., 7 for a total of 8 players)
3. Launch the game and start a session

## Limitations

- Fake players are registered in the session but won't actually spawn in-game or move around
- They won't respond to game events or interact with the world
- This mod is primarily for testing player count limits and session management
- Some multiplayer features may not work correctly with fake players

