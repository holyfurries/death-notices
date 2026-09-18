using System;
using System.Globalization;

namespace DeathNotices;

internal sealed class CasinoLedger
{
    public double net { get; private set; }

    public void load(double value)
    {
        if (!double.IsFinite(value) || Math.Abs(value) > 1_000_000_000) throw new ArgumentOutOfRangeException(nameof(value));
        net = value;
    }

    public bool settle(float stake, float returned)
    {
        if (!float.IsFinite(stake) || !float.IsFinite(returned) || stake <= 0 || stake > 1_000_000 ||
            returned < 0 || returned > 10_000_000) throw new ArgumentOutOfRangeException(nameof(stake));
        double change = Math.Round((double)returned - stake, 2);
        load(net + change);
        return change < 0;
    }

    public static string report(string player, double total)
    {
        if (!double.IsFinite(total) || Math.Abs(total) > 1_000_000_000) throw new ArgumentOutOfRangeException(nameof(total));
        string name = DeathTracker.safe_name(player);
        string amount = Math.Abs(total).ToString("N2", CultureInfo.InvariantCulture);
        if (total < -0.005) return $"{name} lost again. Down ${amount} at casino cards overall. The house sends its regards.";
        if (total > 0.005) return $"{name} lost that round. Still up ${amount} at casino cards overall. Annoying.";
        return $"{name} lost that round. Back to breaking even. All that effort for nothing.";
    }
}
