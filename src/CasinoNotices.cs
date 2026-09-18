using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Casino;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace DeathNotices;

internal static class CasinoNotices
{
    private const string report_key = "DeathNotices.CasinoLoss";
    private sealed class Round
    {
        public IntPtr table;
        public bool armed;
    }
    private readonly record struct Settlement(Round? round, CasinoGameController? game, float stake, float cash_before);
    private static readonly Round[] rounds = new Round[32];
    private static readonly (string? player, float value, float seconds)[] recent = new (string?, float, float)[16];
    private static readonly CasinoLedger ledger = new();
    private static string ledger_path = "";
    private static bool failed;

    public static void install(HarmonyLib.Harmony harmony)
    {
        for (int i = 0; i < rounds.Length; i++) rounds[i] = new Round();
        foreach (Type type in new[] { typeof(BlackjackGameController), typeof(RTBGameController) })
        {
            harmony.Patch(AccessTools.Method(type, "RpcLogic___AddPlayerToCurrentRound_3323014238"),
                postfix: new HarmonyMethod(typeof(CasinoNotices), nameof(begin)));
            harmony.Patch(AccessTools.Method(type, "RemoveLocalPlayerFromGame"),
                prefix: new HarmonyMethod(typeof(CasinoNotices), nameof(before_settlement)),
                finalizer: new HarmonyMethod(typeof(CasinoNotices), nameof(after_settlement)));
        }
        harmony.Patch(AccessTools.Method(typeof(CasinoGamePlayers), "RpcLogic___ReceivePlayerFloat_2317689966"),
            postfix: new HarmonyMethod(typeof(CasinoNotices), nameof(receive)));
    }

    public static void reset()
    {
        foreach (Round round in rounds) { round.table = IntPtr.Zero; round.armed = false; }
        Array.Clear(recent, 0, recent.Length);
        ledger_path = "";
        ledger.load(0);
        failed = false;
    }

    private static void begin(CasinoGameController __instance, NetworkObject __0)
    {
        if (!Main.ready || failed || Player.Local == null || __0 == null || __0 != Player.Local.NetworkObject) return;
        try
        {
            for (int i = 0; i < rounds.Length; i++)
            {
                if (rounds[i].table != __instance.Pointer && rounds[i].table != IntPtr.Zero) continue;
                rounds[i].table = __instance.Pointer;
                rounds[i].armed = true;
                return;
            }
        }
        catch (Exception error) { pause(error); }
    }

    private static void before_settlement(CasinoGameController __instance, out Settlement __state)
    {
        __state = default;
        if (!Main.ready || failed || !MoneyManager.InstanceExists || Player.Local == null) return;
        try
        {
            for (int i = 0; i < rounds.Length; i++)
            {
                Round round = rounds[i];
                if (round.table != __instance.Pointer || !round.armed) continue;
                round.armed = false;
                __state = new Settlement(round, __instance, __instance.LocalPlayerBet, MoneyManager.Instance.cashBalance);
                return;
            }
        }
        catch (Exception error) { pause(error); }
    }

    private static Exception? after_settlement(Exception? __exception, Settlement __state)
    {
        if (__exception != null || __state.round == null || __state.game == null || Player.Local == null) return __exception;
        try
        {
            load();
            float returned = MoneyManager.Instance.cashBalance - __state.cash_before;
            bool loss = ledger.settle(__state.stake, returned);
            string temporary = ledger_path + ".tmp";
            File.WriteAllText(temporary, ledger.net.ToString("R", CultureInfo.InvariantCulture));
            File.Move(temporary, ledger_path, true);
            MelonLogger.Msg($"Death Notices: Casino round stake={__state.stake:F2}, returned={returned:F2}, tracked_net={ledger.net:F2}.");
            if (loss)
            {
                display(Player.Local, (float)ledger.net);
                __state.game.Players.SendPlayerFloat(Player.Local.NetworkObject, report_key, (float)ledger.net);
            }
        }
        catch (Exception error)
        {
            pause(error);
        }
        return __exception;
    }

    private static void pause(Exception error)
    {
        failed = true;
        MelonLogger.Warning($"Death Notices: Casino tracking paused for this scene: {error.Message}");
    }

    private static void load()
    {
        if (ledger_path.Length > 0) return;
        string identity = Player.Local.PlayerCode;
        if (string.IsNullOrEmpty(identity) || identity.Length > 128) throw new InvalidOperationException("Missing casino player identity.");
        string directory = Path.Combine(MelonEnvironment.UserDataDirectory, "DeathNoticesCasino");
        Directory.CreateDirectory(directory);
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        string path = Path.Combine(directory, key + ".txt");
        if (File.Exists(path))
        {
            if (new FileInfo(path).Length > 128) throw new InvalidDataException("Casino ledger exceeds size limit.");
            ledger.load(double.Parse(File.ReadAllText(path), CultureInfo.InvariantCulture));
        }
        ledger_path = path;
    }

    private static void receive(NetworkConnection __0, NetworkObject __1, string __2, float __3)
    {
        if (!Main.ready || __2 != report_key || __1 == null || !float.IsFinite(__3) || Math.Abs(__3) > 1_000_000_000) return;
        try
        {
            Player player = __1.GetComponent<Player>();
            if (player != null) display(player, __3);
        }
        catch (Exception error) { MelonLogger.Warning($"Death Notices: Casino report ignored: {error.Message}"); }
    }

    private static void display(Player player, float value)
    {
        int slot = -1;
        for (int i = 0; i < recent.Length; i++)
        {
            if (recent[i].player == player.PlayerCode) { slot = i; break; }
            if (slot < 0 && (recent[i].player == null || Time.unscaledTime - recent[i].seconds > 10f)) slot = i;
        }
        if (slot < 0) return;
        var previous = recent[slot];
        if (previous.player == player.PlayerCode && previous.value == value && Time.unscaledTime - previous.seconds < 2f) return;
        recent[slot] = (player.PlayerCode, value, Time.unscaledTime);
        Main.queue_notice("Casino report", CasinoLedger.report(player.PlayerName, value));
    }
}
