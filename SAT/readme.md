# Swiss Admin Tools

everything you need for your BattleBit Remastered community server

## Description

This is a fork that adds admin utilities for BattleBit administrators and moderators.
It leverages the [BattleBitAPIRunner](https://github.com/RainOrigami/BattleBitAPIRunner) framework to load and unload modules into the server at runtime.
Running the loader is required for this to work.

## Toolset Commands and Features

Current features are:

### commands

- `!say` sends a message to all players formatted with colors and distinguishable from normal chat. (short form is `@message here`)
- `!clear` clears the chat for all players.
- `!kick <target> <optional reason>` kicks a player from the server.
- `!slay <target>` kills a player.
- `!ban <target> <length in minutes> <optional reason>` bans a player from the server.
- `!gag <target> <length in minutes> <optional reason>` gags a player from the server.
- `!saveloc` saves the current location of the player.
- `!tele <target>` teleports the player to the saved location.
- `!teleto <target>` teleport to the player.
- `!restrict <weapon>` restricts a weapon from being used by the player. Weapon types also allowed with a `#` (e.g. `!restrict #SniperRifle`).
- `!gravity <target> <value>` sets the gravity multiplier for the player (0.1-10).
- `!rcon <command>` executes a rcon command on the server. (must be wrapped in quotes if it contains spaces)
- `!gravity <target> <value>` sets the gravity multiplier for the player (0.1-10).
- `!freeze <target>` (un)freezes the player.
- `!speed <target> <value>` sets the speed multiplier for the player (0.1-10).

### Features

- Custom Welcome messages
- Connecting MOTD
- Customizable map rotation
- Customizable gamemode rotation
- Customizable match settings
- Chat logging to database
- Reports logging to database
- Player stats logging to database

## Targeting for commands

The following target rules apply:

- `@all` targets all players.
- `@me` targets the player who sent the command.
- `@!me` targets all players except the player who sent the command.
- `@usa` targets all players on the USA team.
- `@rus` targets all players on the RUS team.
- `@dead` targets all players who are dead.
- `@alive` targets all players who are alive.
- `@class-here` targets all players of the specified class. (assault, medic, support, engineer, recon, leader)
- `#steam_id64-here` targets the player with the specified SteamID64. (e.g. `#76561197997290818`) (max 1 match)
- `name-here` targets the player with the specified name. (e.g. `nik`) would match Nik, Niko, Nikolas, etc. (max 1
  match)

## Examples

- `!ban nik 60 best reason ever`
- `!ban niko 0` (permanent ban with default reason)
- `!kick @usa`
- `!slay @alive`
- `!kick #76561197997290818 your name is impossible to type`

## Database setup

you need a mysql database running locally (values are currently hardcoded).
Here is the init script: [init.sql](init.sql)

## Configuration

copy the [defaults.example.json](defaults.example.json) file to `defaults.json` and edit it to your liking.

## Restrictions

Restrictions are defined in the [restrictions.md](restrictions.md) file.

## License

I don't care about licenses for this project, do whatever you want with this. If you like it feel free to credit me :)