# Death Notices

[Holy Furries Studio](https://holyfurries.com) ·
[Thunderstore](https://thunderstore.io/c/schedule-i/p/holyfurries/DeathNotices/)
· [Source](https://github.com/holyfurries/death-notices)

Death announcements with a questionable bedside manner. Messages vary with the
observed cause, with extra commentary for repeat visitors.

- **Alex unsuccessfully disputed the charges. Courtesy of police.**
- **Sam discovered friendly fire isn't. Courtesy of Riley.**

Notices wrap to show the whole message and last seven seconds. Known attackers
are named. Unknown causes get a generic notice.

## Install

**IL2CPP only.** Requires **MelonLoader 0.7.3** and **ModSettings 0.3.1**.

Install through **r2modman** or **Thunderstore Mod Manager**, which installs
ModSettings automatically. For manual installation, copy `DeathNotices.dll` and
`ModSettings.dll` from their release ZIPs into the game's `Mods/` folder.

Install on every player. Each peer observes native death events, so cause
attribution can differ between host and clients.

## Settings

Open **Settings → Mods → Death Notices** to choose a top or bottom position,
side margin, and vertical margin. Changes apply live and are saved in
`UserData/MelonPreferences.cfg`. Centred positions ignore the side margin.
Bottom positions stack upward. Notices narrow to fit the screen, and placement
margins are capped to keep the feed visible. Use **Preview notices** to show
sample messages while adjusting the layout.

For casino profit and loss, use
[Casino Ledger](https://thunderstore.io/c/schedule-i/p/holyfurries/CasinoLedger/).

## Development

See
[BUILDING.md](https://github.com/holyfurries/death-notices/blob/main/BUILDING.md)
for builds, tests, packaging, and in-game checks.

[Code: MIT](https://github.com/holyfurries/death-notices/blob/main/LICENSE).
[Icon: studio asset terms](https://github.com/holyfurries/brand-assets/blob/main/LICENSE).
