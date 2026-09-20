# Death Notices

Death announcements with a questionable bedside manner. Messages vary with the cause,
with extra commentary for repeat visitors.

- **Alex unsuccessfully disputed the charges. Courtesy of police.**
- **Sam discovered friendly fire isn't. Courtesy of Riley.**
- **Jordan has been banned from the alive casino.**

## Casino reports

With [Casino Ledger](https://thunderstore.io/c/schedule-i/p/holyfurries/CasinoLedger/)
installed, losing a round of **Blackjack**, **Ride the Bus** or **Slots** gets everyone a report:

- **Alex lost again. Down $1,250.00 at the casino overall. The house sends its regards.**
- **Sam lost that round. Still up $400.00 at the casino overall. Annoying.**

Casino Ledger is optional and is not installed automatically; without it Death Notices only
announces deaths. Casino Ledger does the tracking and keeps the totals; Death Notices only comments on them.
Casino Ledger imports the card totals Death Notices 0.2 kept under
`UserData/DeathNoticesCasino` the first time it loads a save.

Notices appear at the top centre of the screen, wrap to show the whole message, and last
seven seconds.

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
