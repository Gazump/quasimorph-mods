# Loot Tracker

Published on the Steam Workshop as **Have I got this?**

Marks items you don't currently have, so it's obvious whether something in a loot container is
worth the backpack space.

| Marker | Meaning |
|---|---|
| Yellow outline and asterisk | never owned |
| White outline and asterisk | owned before, none right now |
| nothing | you have one |

"Owned" covers ship cargo (all seven tabs, the fridge and the recycler), the shuttle, and every
mercenary on the roster including equipped gear. Markers therefore only ever appear on loot,
corpses, the floor and station stock.

## How it works

One Harmony postfix on `ItemSlot.Initialize`, the single method every item icon in the game is
drawn through. The game itself uses the same spot to flag datadisks you have already unlocked.

Ownership is a `HashSet<string>` of item ids rebuilt at most once per frame, and only on frames
where a slot is actually drawn. Per slot the cost is a hash lookup and a `GetComponent`. There are
no per-frame hooks and nothing allocates in the hot path.

Equipped gear comes from `Mercenary.CreatureData.Inventory` on the roster rather than
`Creatures.Player`, which only exists inside a mission. Reading just the live player leaves
everything equipped looking unowned while on the ship.

## Storage

The registry of everything ever owned lives in
`%USERPROFILE%\AppData\LocalLow\Magnum Scriptum Ltd\Quasimorph\LootTracker\run_<id>.json`,
written whenever the game saves.

The save itself carries only a run id. The game has no identifier for a playthrough
(`SavedGameMetadata` has just `Slot`, `Played`, `LastSaved` and `DifficultyPresetId`) and starting
a new game does not clear the old slot, so one is minted and stored on the save root as
`LootTrackerRunId` via postfixes on `ComponentsLayout.SerializeGlobalComponents` and
`DeserializeGlobalComponents`. A new game mints a new id, which is what stops a reused slot
inheriting an old registry.

That extra key is safe to leave in a save. `LoadFromJSON.LoadFieldsAndProperties` looks members up
by name and never enumerates the JSON, so a key it doesn't know about is never read: **a save
written with this mod enabled loads normally without it.** Saving again with the mod off drops the
key.

Registering a component instead would break saves. `DeserializeGlobalComponents` resolves types
with `typeof(DungeonBuilder).Assembly.GetType(...)`, which only searches Assembly-CSharp, so a mod
type yields null and `Dictionary<Type, JSONNode>.Add(null, …)` throws.

Both save patches are wrapped in try/catch. They run inside the game's save and load path, so a
failure degrades to everything showing yellow rather than to a broken save.

### Limits

- Loading an older save does not roll the registry back, so items found in a discarded hour still
  show white.
- Quitting without saving loses whatever was learned since the last save.
- Deleting a save leaves its `run_<id>.json` behind. They are a few KB each.

## config.json

Written on first run to `…\Quasimorph\LootTracker\config.json`, beside the saves rather than in the
mod folder, since Steam replaces the mod folder on every workshop update. Changes need a restart.

| Key | Default | Notes |
|---|---|---|
| `Enabled` | `true` | |
| `ShowBox` | `true` | outline around the slot |
| `BoxThickness` | `1` | pixels |
| `ShowGlyph` | `true` | the asterisk |
| `ShowTechLevel` | `false` | swaps the asterisk for the item's tech level |
| `FallbackGlyph` | `"*"` | |
| `NeverOwnedColor` | `"#FFD24A"` | |
| `OwnedBeforeColor` | `"#FFFFFF"` | |
| `Corner` | `"TopRight"` | the stack count sits bottom-right, so avoid that one |
| `OffsetX` / `OffsetY` | `1` / `1` | positive X is right, positive Y is up |
| `FontSize` | `12` | slots are 24px, or 50px for two-wide items |
| `LogTechLevelHistogram` | `false` | dumps the TechLevel spread to `Player.log` |

### Tech level

Quasimorph has no rarity stat. `ItemRecord.TechLevel` is the nearest thing and is a real mechanic:
factions have a `CurrentTechLevel`, missions a `MinTechLevel`, and enemy gear spawns are gated by
it. Across all 1374 item records it runs 1-10 with a special tier at 100:

```
1: 477   2: 99   3: 152   4: 171   5: 100
6: 67    7: 67   8: 78    9: 59    10: 65    100: 39
```

A 24px slot only fits one character, so 1-9 render as digits and the other two use their Roman
forms, `X` and `C`.

## Publishing

Workshop item id `3783896795`.

```powershell
.\tools\stage-publish.ps1 -Mod LootTracker -ItemId 3783896795
```

That prints the command to paste into the in-game developer console (backtick to open). On a first
publish both commands are needed: `mod_createworkshopitem` only sets the title and content, while
the thumbnail and the manifest's `SteamTags` are sent by `mod_updateworkshopitem`, whose third
argument must be `true` for the preview image.

Steam gives no useful feedback here. The game submits an empty change note, so the change note
count never moves, and the client caches item details for a long time. Check the DLL that actually
got published:

```powershell
ilspycmd -o out -r "<game>\Quasimorph_Data\Managed" "<steam>\steamapps\workshop\content\2059170\3783896795\LootTracker.dll"
```

The in-game display name comes from the Steam item title, not `UniqueModName` — `SteamWrapper`
reads `pDetails.m_rgchTitle` and passes it through as `UserMod.Title`. Local copies have no Steam
title and fall back to showing `LootTracker`.
