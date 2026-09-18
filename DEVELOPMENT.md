# Development

Version 0.2.0, built against local Schedule I 0.4.6f13 IL2CPP interop assemblies.

## Event handling

Harmony prefixes the native Player ReceiveImpact RPC logic, then matches that impact to
PlayerHealth TakeDamage RPC logic by damage amount (0.01 tolerance) within 1.5 seconds.
The last eight impact IDs per player suppress duplicates. Only one pending impact is kept;
ambiguous overlapping impacts may therefore lose attribution rather than guess.

Damage is confirmed after health decreases or the player dies during the call. A scoped
current cause covers a nested death before the damage postfix; the Harmony finalizer restores
the previous scope even if the game method throws. Only fatal confirmed damage (health at zero or no longer alive) retains an attacker for
a delayed death RPC. Nonfatal or untyped confirmed hits clear that attribution. A fatal
confirmed cause expires after two seconds. Death and revive RPC logic
hooks establish one announcement per life; a 0.25-second alive-state poll is a fallback.
Players already dead when first observed are initialized silently. Sixteen identity-keyed
player slots clear on disconnect or Main scene unload. No player IDs are transmitted by the mod.

Each installed peer observes the native replicated events and displays its own notification.
No custom protocol or host-only broadcast is added. Peers can differ in attribution if their
native impact/damage observations differ. The fallback is a generic death notice.

Attacker resolution uses the supplied impact source: player, police, NPC, or vehicle driver.
Names are sanitized and bounded. Physics impacts from identified vehicles count as run-over
causes. No attacker is inferred from proximity or wanted level. Lethal effects, lightning, and untyped damage have no specific cause label in this version.

A sixteen-entry queue bounds announcements, drops the oldest on overflow, expires entries
after ten seconds, and submits one native notification every half second. Notification
lifetime is six seconds. Errors disable the mod for the scene and log once. Source game
exceptions are returned unchanged. No gameplay methods are suppressed or damage altered.

## Build

```sh
MELONLOADER_DIR='/path/to/profile/MelonLoader' ./build.sh
nix-shell -p dotnet-sdk_8 --run 'dotnet run --project tests/Tests.csproj'
```

Format C# using dotnet format for DeathNotices.csproj (MelonLoaderDir environment variable)
and tests/Tests.csproj. Format package.py with Ruff. ZIP contains only manifest, README,
256x256 icon and Mods/DeathNotices.dll. Do not distribute game assemblies.

## Manual checks still required

- Solo: die to a police gunshot, then revive and die again. One notice per life.
- Co-op: install on all four peers. Have a joining player killed by police and another
  player. Compare notifications and logs on host and all clients, including the victim.
- Kill several players together; each notice must display once without losing attribution
  between players. Check gunshot, melee, vehicle and explosion sources actually resolve.
- Take a nonfatal hit, then die later to another untyped cause: never blame the stale attacker.
- Join while another player is dead: do not replay that death. Test disconnect/rejoin,
  scene reload, and revival immediately after death.
- Check that native notifications display with no icon and while the death screen is open.
  Build/tests do not establish native UI layout, Harmony RPC ordering, or MP replication.

Automated checks cover cause freshness/matching, duplicate suppression, revive/reset,
player isolation, name safety, and bounded queue overflow/expiry. No game runtime is simulated.

## Notice variants

Every death picks one of four phrasings for the observed cause. Police gunshots and self
injuries have their own pools; known other-player attacks can use friendly-fire wording.
Unknown causes never name an attacker. Categories are separate from names, preventing a
player called "police" from selecting police-specific text. Repeat-death commentary can
replace the joke on third and later odd-numbered deaths. Counts cap at 1000, survive
revives, and reset with player identity or scene. Each peer chooses wording independently.
All names and final messages remain bounded. The old timed pigeon notification is removed.

## Casino settlement

Local Blackjack/RTB AddPlayerToCurrentRound RPC logic arms one settlement per table.
RemoveLocalPlayerFromGame captures the stake and current cash; its finalizer records the
cash returned synchronously by a successful call minus the stake. Failed native calls
are never recorded. Up to 32 table identities are tracked per scene. Native payouts are
observed rather than replaced; no money, payout multipliers or odds are changed.

CasinoLedger tracks net, validates finite stakes/returns and bounds totals to one billion.
Each local player keeps a SHA-256-keyed text ledger in UserData/DeathNoticesCasino, across
all saves in that profile. Files are at most 128 bytes and updated with a temporary-file
rename. Malformed data or I/O/API errors pause local casino tracking for the scene, leaving
death notices active. Reports cover only completed Blackjack and RTB rounds observed since
installation; slots, old history and rounds abandoned before settlement are not inferred.

Losses display locally and send the net total through CasinoGamePlayers.SendPlayerFloat
with a fixed DeathNotices.CasinoLoss key. The replicated receive hook displays a Casino
report on installed peers. Values are finite/bounded and names sanitized. Sixteen recent
player/value records suppress the local network echo for two seconds. These are cosmetic
client-reported totals, not an authoritative money ledger or anti-cheat system. Peers do
not persist other players' totals. The shared bounded queue preserves separate notice titles.

Pure tests cover net profit versus returned stake, losses while still ahead, pushes,
persisted-total loading, malformed amounts, queue titles, variant eligibility and bounds.
Native settlement timing and custom casino float replication still require SP/MP testing.
Test a bust, dealer loss, push, normal win and blackjack; RTB failure and cash-out at each
stage; repeat rounds; table exit; simultaneous players; reconnect and profile restart.
Compare Casino round stake/returned/tracked_net logs with the actual wallet changes.
If payouts occur asynchronously in a different game build, this hook requires adjustment.
