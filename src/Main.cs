using System;
using HarmonyLib;
using Il2CppFishNet;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Vehicles;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(DeathNotices.Main), "Death Notices", "0.5.0", "holyfurries")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace DeathNotices;

public sealed class Main : MelonMod
{
    private sealed class PlayerState
    {
        public Player? player;
        public readonly DeathTracker tracker = new();
        public DeathCause? current_damage;
    }
    private readonly record struct DamageEvent(PlayerState? state, float health_before, bool alive_before,
        DeathCause cause, DeathCause? previous_damage);
    private readonly record struct DeathEvent(PlayerState? state, bool alive_before);
    private static readonly PlayerState[] players = new PlayerState[16];
    private static readonly NoticeQueue notices = new();
    private static MelonPreferences_Entry<NoticePosition> notice_position = null!;
    private static MelonPreferences_Entry<float> notice_margin_x = null!;
    private static MelonPreferences_Entry<float> notice_margin_y = null!;
    private static bool running;
    private static bool failed;
    private static bool feed_failed;
    private float next_tick_seconds;
    private float next_notice_seconds;
    private bool? logged_host;
    internal static bool ready => running && !failed && LoadManager.InstanceExists && LoadManager.Instance.IsGameLoaded;

    public override void OnInitializeMelon()
    {
        for (int i = 0; i < players.Length; i++) players[i] = new PlayerState();
        MelonPreferences_Category preferences = MelonPreferences.CreateCategory("DeathNotices");
        notice_position = preferences.CreateEntry("position", NoticePosition.TopCenter, "Notice position",
            "TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter or BottomRight");
        notice_margin_x = preferences.CreateEntry("margin_x", 24f, "Distance from the left or right screen edge (1920x1080 units)");
        notice_margin_y = preferences.CreateEntry("margin_y", 72f, "Distance from the top or bottom screen edge (1920x1080 units)");
        ModSettings.Settings.dropdown("Death Notices", "Notice position", notice_position);
        ModSettings.Settings.slider("Death Notices", "Side margin", notice_margin_x, minimum: 0f, maximum: 600f, whole_numbers: true);
        ModSettings.Settings.slider("Death Notices", "Top or bottom margin", notice_margin_y, minimum: 0f, maximum: 900f, whole_numbers: true);
        notice_position.OnEntryValueChanged.Subscribe((_, _) => place_notices());
        notice_margin_x.OnEntryValueChanged.Subscribe((_, _) => place_notices());
        notice_margin_y.OnEntryValueChanged.Subscribe((_, _) => place_notices());
        place_notices();
        HarmonyInstance.Patch(AccessTools.Method(typeof(Player), "RpcLogic___ReceiveImpact_427288424"),
            prefix: new HarmonyMethod(typeof(Main), nameof(observe_impact)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(PlayerHealth), "RpcLogic___TakeDamage_3505310624"),
            prefix: new HarmonyMethod(typeof(Main), nameof(before_damage)),
            postfix: new HarmonyMethod(typeof(Main), nameof(after_damage)),
            finalizer: new HarmonyMethod(typeof(Main), nameof(finish_damage)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(PlayerHealth), "RpcLogic___Die_2166136261"),
            prefix: new HarmonyMethod(typeof(Main), nameof(before_death)),
            postfix: new HarmonyMethod(typeof(Main), nameof(after_death)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(PlayerHealth), "RpcLogic___Revive_3848837105"),
            postfix: new HarmonyMethod(typeof(Main), nameof(after_revive)));
    }

    public override void OnPreferencesSaved() => place_notices();

    public override void OnPreferencesLoaded() => place_notices();

    private static void place_notices()
    {
        if (notice_position == null) return;
        NoticeFeed.place(new NoticePlacement(notice_position.Value, notice_margin_x.Value, notice_margin_y.Value));
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (sceneName != "Main") return;
        clear();
        running = true;
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main") return;
        running = false;
        clear();
    }

    private void clear()
    {
        foreach (PlayerState state in players)
        {
            state.player = null;
            state.current_damage = null;
            state.tracker.reset(alive: true);
        }
        notices.reset();
        NoticeFeed.reset();
        failed = false;
        feed_failed = false;
        logged_host = null;
        next_tick_seconds = 0;
        next_notice_seconds = 0;
    }

    public override void OnUpdate()
    {
        if (!ready) return;
        try
        {
            if (!feed_failed) NoticeFeed.update(Time.unscaledTime);
            if (Time.unscaledTime < next_tick_seconds) return;
            next_tick_seconds = Time.unscaledTime + 0.25f;
            bool host = InstanceFinder.IsServer;
            if (logged_host != host)
            {
                LoggerInstance.Msg($"Death Notices: {(host ? "Host" : "Client")} listening to native death events; each installed peer displays its own notices.");
                logged_host = host;
            }
            int count = Math.Min(Player.PlayerList.Count, players.Length);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerState state = players[i];
                bool present = false;
                for (int j = 0; j < count; j++)
                    if (Player.PlayerList[j] != null && Player.PlayerList[j] == state.player) present = true;
                if (present) continue;
                state.player = null;
                state.current_damage = null;
                state.tracker.reset(alive: true);
            }
            for (int i = 0; i < count; i++)
            {
                Player player = Player.PlayerList[i];
                if (player == null || player.Health == null) continue;
                PlayerState? state = get_player(player);
                if (state == null) continue;
                if (player.Health.IsAlive)
                {
                    if (state.tracker.is_dead) state.tracker.revive();
                }
                else announce(state);
            }
            if (Time.unscaledTime < next_notice_seconds) return;
            if (!notices.try_take(Time.unscaledTime, out string message, out string title)) return;
            show_notice(title, message);
            next_notice_seconds = Time.unscaledTime + 0.5f;
        }
        catch (Exception error) { disable(error); }
    }

    private static void show_notice(string title, string message)
    {
        if (!feed_failed)
        {
            try
            {
                NoticeFeed.show(title, message, Time.unscaledTime);
                return;
            }
            catch (Exception error)
            {
                feed_failed = true;
                NoticeFeed.reset();
                MelonLogger.Warning($"Death Notices: Overlay feed failed; using native notifications for this scene: {error}");
            }
        }
        if (NotificationsManager.InstanceExists) NotificationsManager.Instance.SendNotification(title, message, null, 6f, false);
    }

    private static PlayerState? get_player(Player player)
    {
        PlayerState? empty = null;
        for (int i = 0; i < players.Length; i++)
        {
            PlayerState state = players[i];
            if (state.player != null && state.player == player) return state;
            if (state.player == null && empty == null) empty = state;
        }
        if (empty == null || player.Health == null) return null;
        empty.player = player;
        empty.current_damage = null;
        empty.tracker.reset(player.Health.IsAlive);
        return empty;
    }

    private static void observe_impact(Player __instance, Impact __0)
    {
        if (!ready || __instance == null || __0 == null || !float.IsFinite(__0.ImpactDamage) || __0.ImpactDamage <= 0) return;
        try
        {
            PlayerState? state = get_player(__instance);
            if (state == null) return;
            DeathKind kind = __0.ImpactType switch
            {
                EImpactType.Bullet => DeathKind.Gunshot,
                EImpactType.Explosion => DeathKind.Explosion,
                EImpactType.Punch or EImpactType.BluntMetal => DeathKind.Melee,
                EImpactType.SharpMetal => DeathKind.SharpWeapon,
                EImpactType.PhysicsProp => DeathKind.Impact,
                _ => DeathKind.Unknown
            };
            string attacker = "";
            bool self_inflicted = false;
            AttackerKind attacker_kind = AttackerKind.Unknown;
            var source = __0.ImpactSource;
            if (source != null)
            {
                Player source_player = source.GetComponentInParent<Player>();
                PoliceOfficer police = source.GetComponentInParent<PoliceOfficer>();
                LandVehicle vehicle = source.GetComponentInParent<LandVehicle>();
                NPC npc = source.GetComponentInParent<NPC>();
                if (vehicle != null && kind == DeathKind.Impact)
                {
                    kind = DeathKind.Vehicle;
                    source_player = vehicle.DriverPlayer;
                }
                if (source_player != null)
                {
                    attacker_kind = AttackerKind.Player;
                    self_inflicted = source_player == __instance;
                    attacker = DeathTracker.safe_name(source_player.PlayerName);
                }
                else if (police != null)
                {
                    attacker_kind = AttackerKind.Police;
                    attacker = "police";
                }
                else if (npc != null)
                {
                    attacker_kind = AttackerKind.NPC;
                    attacker = DeathTracker.safe_name(npc.FullName);
                }
            }
            state.tracker.observe_impact(__0.ImpactID, __0.ImpactDamage, new DeathCause(kind, attacker, self_inflicted, attacker_kind), Time.unscaledTime);
        }
        catch (Exception error) { disable(error); }
    }

    private static void before_damage(PlayerHealth __instance, float __0, out DamageEvent __state)
    {
        __state = default;
        if (!ready || __instance.Player == null || !float.IsFinite(__0) || __0 <= 0) return;
        try
        {
            PlayerState? state = get_player(__instance.Player);
            if (state == null) return;
            DeathCause cause = state.tracker.consume_impact(__0, Time.unscaledTime);
            __state = new DamageEvent(state, __instance.CurrentHealth, __instance.IsAlive, cause, state.current_damage);
            state.current_damage = cause;
        }
        catch (Exception error) { disable(error); }
    }

    private static void after_damage(PlayerHealth __instance, DamageEvent __state)
    {
        if (__state.state == null) return;
        try
        {
            if (__state.alive_before && (__instance.CurrentHealth < __state.health_before || !__instance.IsAlive))
                __state.state.tracker.record_damage(__state.cause, __instance.CurrentHealth <= 0 || !__instance.IsAlive, Time.unscaledTime);
        }
        catch (Exception error) { disable(error); }
    }

    private static Exception? finish_damage(Exception? __exception, DamageEvent __state)
    {
        if (__state.state != null) __state.state.current_damage = __state.previous_damage;
        return __exception;
    }

    private static void before_death(PlayerHealth __instance, out DeathEvent __state)
    {
        __state = default;
        if (!ready || __instance.Player == null) return;
        try { __state = new DeathEvent(get_player(__instance.Player), __instance.IsAlive); }
        catch (Exception error) { disable(error); }
    }

    private static void after_death(PlayerHealth __instance, DeathEvent __state)
    {
        if (__state.state == null || !__state.alive_before || __instance.IsAlive) return;
        try { announce(__state.state); }
        catch (Exception error) { disable(error); }
    }

    private static void after_revive(PlayerHealth __instance)
    {
        if (!ready || __instance.Player == null || !__instance.IsAlive) return;
        try { get_player(__instance.Player)?.tracker.revive(); }
        catch (Exception error) { disable(error); }
    }

    private static void announce(PlayerState state)
    {
        if (state.player == null || !state.tracker.try_death(state.player.PlayerName, Time.unscaledTime, state.current_damage, System.Random.Shared.Next(8), out string message)) return;
        queue_notice("Death notice", message);
        MelonLogger.Msg($"Death Notices: {message}");
    }

    internal static void queue_notice(string title, string message)
    {
        notices.add(message, Time.unscaledTime, title);
    }

    private static void disable(Exception error)
    {
        if (failed) return;
        failed = true;
        MelonLogger.Error($"Death Notices: Disabled for this scene after a game API failure: {error}");
    }
}
