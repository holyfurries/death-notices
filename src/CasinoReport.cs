using System;
using System.Globalization;

namespace DeathNotices;

internal static class CasinoReport
{
    public static string format(string player, double lifetime_net)
    {
        if (!double.IsFinite(lifetime_net) || Math.Abs(lifetime_net) > 1_000_000_000_000) throw new ArgumentOutOfRangeException(nameof(lifetime_net));
        string name = DeathTracker.safe_name(player);
        string amount = Math.Abs(lifetime_net).ToString("N2", CultureInfo.InvariantCulture);
        if (lifetime_net < -0.005) return $"{name} lost again. Down ${amount} at the casino overall. The house sends its regards.";
        if (lifetime_net > 0.005) return $"{name} lost that round. Still up ${amount} at the casino overall. Annoying.";
        return $"{name} lost that round. Back to breaking even. All that effort for nothing.";
    }
}
