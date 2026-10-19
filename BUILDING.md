# Building

Requires the .NET 8 SDK and Python 3. The mod targets .NET 6 and tests run on
.NET 8. Launch Schedule I with MelonLoader once to generate its IL2CPP
assemblies.

Set the MelonLoader path to the directory containing both `net6` and
`Il2CppAssemblies`. These game and loader dependencies stay on your machine.

Install ModSettings 0.2.0 in the profile's `Mods` folder before building. For a
local ModSettings build, set `ModSettingsDll` to its DLL path instead. The
shared DLL is referenced during compilation and shipped separately by its own
mod.

## Windows (PowerShell)

```powershell
$loader = "C:\path\to\profile\MelonLoader"
dotnet build DeathNotices.csproj --configuration Release "-p:MelonLoaderDir=$loader"
dotnet run --project tests/Tests.csproj
python package.py --prepare
```

## Linux / Nix

```sh
MELONLOADER_DIR='/path/to/profile/MelonLoader' ./build.sh
nix-shell -p dotnet-sdk_8 --run 'dotnet run --project tests/Tests.csproj'
```

The DLL is written to `bin/Release/net6.0/`. Packaging produces a Thunderstore
ZIP in `dist/`. Run `python package.py --prepare` after building to copy the DLL
into `package/` and create the ZIP. Commit that DLL alongside its source
changes. The `bin/` and `dist/` directories remain ignored.

## Formatting

Set `MelonLoaderDir` as an environment variable when formatting the main
project:
`MelonLoaderDir=/path/to/profile/MelonLoader dotnet format DeathNotices.csproj`
on Linux, or set `$env:MelonLoaderDir` in PowerShell before running the
formatter. Run `dotnet format tests/Tests.csproj` for tests and
`ruff format package.py` for the packaging script.

Format Markdown with Neovim's configured `mdformat --wrap 80 --number`,
including the GFM, footnote, and frontmatter plugins. Avoid semicolons in prose.

## In-game checks

Standalone checks cover damage attribution, duplicate suppression, revival,
bounded names, notice expiry, and placement. Native event ordering and UI need
game checks:

- Die to police gunfire, melee, a vehicle, and an explosion. Revive and die
  again. Each life must produce one notice with an observed cause or a generic
  fallback.
- Install on four peers. Compare host, client, and victim notices, including
  simultaneous deaths and late joining while another player is already dead.
- Take a nonfatal hit, then die to an unrelated cause. Never blame the stale
  attacker. Check nested damage calls, scene reloads, and disconnect/rejoin.
- Change position and both margins in the main and pause menus. Check live
  placement, saved values, invalid config values, and bottom-up stacking.
- Confirm long names and messages wrap without rich-text injection. The feed
  must remain visible on the death screen, with bounded rows and fading notices.

## GitHub Actions

`Package and publish` runs standalone tests and release-metadata checks on pull
requests. Pushes to main and manual runs also package the committed DLL into a
validated Thunderstore ZIP. Download the `thunderstore-package` artifact from
the run and extract the mod ZIP inside. Artifacts expire after 30 days.

CI packages `package/DeathNotices.dll`. It does not compile the mod or need game
references. Rebuild locally and run `python package.py --prepare` whenever the
mod source changes. CI tests source code separately. Those tests do not prove
the committed DLL matches that source or that multiplayer works.

## Publishing

1. Update the version in the project, package manifest and MelonInfo attribute
   together.
2. Build locally, run tests, then run `python package.py --prepare`.
3. Commit and push the source, metadata and refreshed
   `package/DeathNotices.dll`.
4. Create and push a matching `vX.Y.Z` tag.

**Pushing the version tag publishes to Thunderstore automatically** after checks
and packaging pass. Main-branch pushes and manual runs only create ZIP
artifacts. The tag must match the manifest version.

Publishing uses the existing `TCLI_AUTH_TOKEN` Actions secret and official
`tcli` 0.2.4, uploading the exact ZIP artifact to `holyfurries` / `schedule-i`.
No private reference repository or reference token is needed.

Existing Thunderstore versions cannot be replaced. Bump the version for each
release. If an upload fails, check Thunderstore before rerunning the workflow.
