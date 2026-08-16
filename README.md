# Quasimorph Mods

C# mods for [Quasimorph](https://store.steampowered.com/app/2059170/Quasimorph/) (Steam AppID
`2059170`, Unity 2022.3, built against game version 1.0.1).

A Quasimorph mod is a plain .NET class library compiled against the game's `Assembly-CSharp.dll`.
The game loads it by reflection and calls any `public static` method tagged `[Hook(...)]`. No Unity
Editor is involved, and the .NET SDK plus any editor is enough.

## Mods

| Mod | Description |
|---|---|
| [CombatPsychology](src/CombatPsychology) | Combat stress, breakdowns, fortitude, battle highs and treatments (sedatives, alcohol, smoking). |
| [LootTracker](src/LootTracker) | Marks loot you don't already own, so duplicates are obvious at a glance. On the Workshop as *Have I got this?* |
| [SampleMod](src/SampleMod) | Minimal template. Copy it to start a new mod. |

## Building

Requires the [.NET SDK](https://dotnet.microsoft.com/download) and a Quasimorph install.

```
dotnet build QuasimorphMods.sln
```

The build probes the usual Steam library locations for the game. If yours is somewhere else,
create `Directory.Build.user.props` (untracked):

```xml
<Project>
  <PropertyGroup>
    <QuasimorphDir>D:\Games\Quasimorph</QuasimorphDir>
  </PropertyGroup>
</Project>
```

Every build copies the assembly, `modmanifest.json` and `thumbnail.png` into
`%USERPROFILE%\AppData\LocalLow\Magnum Scriptum Ltd\Quasimorph\LocalUserPresets\<UniqueModName>\`,
which the game scans on startup. Pass `/p:QmDeploy=false` to skip that.

Mods are only loaded during bootstrap, so a game restart is needed after every rebuild.

## Layout

```
Directory.Build.props     shared paths, game references, deploy target
src/<Mod>/                one project per mod
tools/decompile.ps1       regenerates reference/ after a game update
tools/stage-publish.ps1   builds and stages a mod for a workshop upload
reference/                decompiled game source (untracked)
```

## Reference sources

`tools/decompile.ps1` decompiles `Assembly-CSharp.dll` into `reference/` with
[ILSpy](https://github.com/icsharpcode/ILSpy), which makes the game's classes, hooks and config
records searchable from the editor. That output is the game's code, not ours, so it is gitignored
and must stay that way.

```
dotnet tool install -g ilspycmd --version 9.0.0.7889
.\tools\decompile.ps1
```

## Notes on the mod API

- Hooks are `public static` methods with `[Hook(ModHookType.X)]`. Most take a single `IModContext`,
  which exposes `State` (the service locator) and `ModContentPath`.
- `ResourcesLoad` is different: `public static UnityEngine.Object Hook(string path)`, called from
  `CustomResources.Load` to override an asset, returning null to fall through.
- `BeforeSaveLoaded`, `BeforeDungeonLoaded` and `BeforeSpaceLoaded` are invoked by spreading a
  `string[]` of save JSON as the method's arguments, so their signature depends on the save. The
  `After*Loaded` hooks are the usable ones.
- Exceptions inside a hook are caught and logged as `Unexpected error in hook <type> for mod <name>`,
  so failures are silent unless you read `Player.log`.
- `0Harmony.dll` ships with the game and is already referenced, so runtime patching needs no extra
  dependency.
- Never ship `Assembly-CSharp.dll`, `UnityEngine*.dll` or `0Harmony.dll` alongside a mod. All the
  references here are `Private=false` so they are not copied.

## Starting a new mod

1. Copy `src/SampleMod` to `src/<NewName>`.
2. Rename the `.csproj` and update `AssemblyName`, `RootNamespace` and `UniqueModName`.
3. Match `UniqueModName` and `Assemblies` in `modmanifest.json`.
4. `dotnet sln QuasimorphMods.sln add src/<NewName>/<NewName>.csproj`
