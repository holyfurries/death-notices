# Death Notices

Death announcements with a questionable bedside manner. Messages vary with the cause,
with extra commentary for repeat visitors.

- **Alex unsuccessfully disputed the charges. Courtesy of police.**
- **Sam discovered friendly fire isn't. Courtesy of Riley.**
- **Jordan has been banned from the alive casino.**

## Casino reports

Lose a round of **Blackjack** or **Ride the Bus** and everyone with the mod gets a report:

- **Alex lost again. Down $1,250.00 at casino cards overall. The house sends its regards.**
- **Sam lost that round. Still up $400.00 at casino cards overall. Annoying.**

Totals are net winnings minus wagers, tracked from this update onward. Returned stakes
and tied rounds do not count as profit. Tracking covers these two card games, not slots.
Each player's total is kept across sessions and saves in their local mod-manager profile,
under `UserData/DeathNoticesCasino`. Old gambling history cannot be recovered. Switching
profiles starts a separate record unless that folder is copied too.

Notices appear at the top centre of the screen, wrap to show the whole message, and last
seven seconds. The mod does not change payouts, odds, or player money.

## Moving the notices

Each player can move the feed in `UserData/MelonPreferences.cfg` after the first launch:

```toml
[DeathNotices]
position = "TopCenter"
margin_x = 24.0
margin_y = 72.0
```

`position` is `TopLeft`, `TopCenter`, `TopRight`, `BottomLeft`, `BottomCenter` or `BottomRight`.
Margins are distances from the screen edge on a 1920x1080 layout; `margin_x` is ignored when
centred. Bottom positions stack upward.

## Installation

Install through **r2modman** or **Thunderstore Mod Manager**.

For manual installation, put `DeathNotices.dll` in your game's `Mods` folder.
Requires **MelonLoader 0.7.3**. **IL2CPP only.**

## Development

Build instructions: [BUILDING.md](https://github.com/holyfurries/death-notices/blob/main/BUILDING.md).

## License

[MIT](https://github.com/holyfurries/death-notices/blob/main/LICENSE).
