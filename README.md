# GermanBrainrot

Synced custom creature sounds for Lethal Company. Each creature type plays random clips from its own streamer sound pack folder, synchronized to all players via networked audio streaming.

**All players must have this mod installed.**

## Setup

1. Install [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/).
2. Copy the **entire** `Template/bin/` contents into your mod profile:
   ```
   ...\profiles\<YourProfile>\BepInEx\plugins\GermanBrainrot\
   ```
   This must include `GermanBrainrot.dll`, `Concentus.dll`, `Concentus.Oggfile.dll`, `config/`, and `audio/packs/`.
3. Add `.opus` or `.wav` files (48 kHz mono recommended for opus) into the creature pack folders under `audio/packs/`.

## Sound packs

Default layout (one streamer folder per creature type):

```
BepInEx/plugins/GermanBrainrot/
  GermanBrainrot.dll
  config/creature-profiles.json
  audio/packs/
    streamer_a/    # Hoarding Bug
    streamer_b/    # Tulip Snake
    streamer_c/    # Flower Man (Bracken)
```

Convert audio for best results:

```bash
ffmpeg -i input.wav -c:a libopus -ar 48000 -ac 1 output.opus
```

## Configuration

BepInEx config (`BepInEx/config/BinderDyn.GermanBrainrot.cfg`):

| Setting | Default | Description |
|---------|---------|-------------|
| `PlayAlongsideVanilla` | `true` | When true, vanilla creature sounds play alongside custom audio. When false, vanilla is suppressed. |
| `Enable_hoarding_bug` | `true` | Toggle Hoarding Bug custom sounds |
| `Enable_tulip_snake` | `true` | Toggle Tulip Snake custom sounds |
| `Enable_flower_man` | `true` | Toggle Flower Man custom sounds |

## Adding a new creature

1. Create a folder under `audio/packs/` with your clips.
2. Add a profile block to `config/creature-profiles.json`:

```json
{
  "id": "jester",
  "enemyType": "JesterAI",
  "soundPackFolder": "packs/my_streamer",
  "enabled": true,
  "triggerOnVanillaClips": ["*"]
}
```

3. Restart the game (or reload the lobby). No recompile needed.

Use LCSoundTool (F5 logging) to find exact vanilla clip names if you want specific triggers instead of `"*"`.

## Testing (multi-client sync)

Set `NumberOfClients` to `2` in `Template.csproj` (default) and build with `StartGame=true` to launch two game instances locally. Verify:

1. **Additive mode** (`PlayAlongsideVanilla=true`): vanilla creature sounds and custom pack audio both play; custom audio is synced across clients.
2. **Replace mode** (`PlayAlongsideVanilla=false`): only custom pack audio plays for triggered clips.
3. **Random picks**: each sound trigger picks a random clip on the host; all clients hear the same clip for that trigger.

Use LCSoundTool F5 logging to refine `triggerOnVanillaClips` in `creature-profiles.json` if needed.

## Build

Requires .NET SDK and `netcode-patcher` CLI (`dotnet tool install -g evaisa.netcodepatcher.cli`).

```bash
dotnet build
dotnet test /p:SkipNetcodePatch=true
```

## Credits

Audio sync architecture inspired by [Mirage](https://github.com/qwbarch/mirage).
