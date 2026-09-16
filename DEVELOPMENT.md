# Development

Version 0.1.0, built against local Schedule I 0.4.6f13 IL2CPP interop assemblies.

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
