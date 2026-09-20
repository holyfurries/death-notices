using System;
using System.Runtime.CompilerServices;
using CasinoLedger;
using MelonLoader;

namespace DeathNotices;

internal static class CasinoNotices
{
    public static void install()
    {
        if (MelonBase.FindMelon("Casino Ledger", "holyfurries") == null)
        {
            MelonLogger.Msg("Death Notices: Casino Ledger is not installed, casino reports are off.");
            return;
        }
        subscribe();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void subscribe()
    {
        CasinoStats.round_settled += report_loss;
    }

    private static void report_loss(RoundReport report)
    {
        if (!Main.ready || report.round.net >= 0) return;
        Main.queue_notice("Casino report", CasinoReport.format(report.round.player_name, report.lifetime.net));
    }
}
