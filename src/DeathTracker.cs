using System;
using System.Globalization;
using System.Text;

namespace DeathNotices;

internal enum DeathKind { Unknown, Gunshot, Explosion, Melee, SharpWeapon, Vehicle, Impact }
internal readonly record struct DeathCause(DeathKind kind, string attacker = "", bool self_inflicted = false);

internal sealed class DeathTracker
{
    private readonly int[] impact_ids = new int[8];
    private int impact_count;
    private int impact_index;
    private DeathCause pending_cause;
    private float pending_amount;
    private float pending_seconds = float.NegativeInfinity;
    private DeathCause last_cause;
    private float damage_seconds = float.NegativeInfinity;
    public bool is_dead { get; private set; }

    public void reset(bool alive)
    {
        impact_count = 0;
        impact_index = 0;
        pending_cause = default;
        pending_amount = 0;
        pending_seconds = float.NegativeInfinity;
        last_cause = default;
        damage_seconds = float.NegativeInfinity;
        is_dead = !alive;
    }

    public void observe_impact(int impact_id, float amount, DeathCause cause, float now_seconds)
    {
        if (is_dead || !float.IsFinite(amount) || amount <= 0 || !float.IsFinite(now_seconds)) return;
        for (int i = 0; i < impact_count; i++) if (impact_ids[i] == impact_id) return;
        impact_ids[impact_index] = impact_id;
        impact_index = (impact_index + 1) % impact_ids.Length;
        impact_count = Math.Min(impact_ids.Length, impact_count + 1);
        pending_cause = cause;
        pending_amount = amount;
        pending_seconds = now_seconds;
    }

    public DeathCause consume_impact(float amount, float now_seconds)
    {
        float age = now_seconds - pending_seconds;
        bool match = !is_dead && float.IsFinite(amount) && amount > 0 && float.IsFinite(age) &&
            age >= 0 && age <= 1.5f && Math.Abs(amount - pending_amount) <= 0.01f;
        pending_seconds = float.NegativeInfinity;
        return match ? pending_cause : default;
    }

    public void record_damage(DeathCause cause, bool fatal, float now_seconds)
    {
        if (is_dead || !float.IsFinite(now_seconds)) return;
        last_cause = fatal ? cause : default;
        damage_seconds = now_seconds;
    }

    public bool try_death(string victim, float now_seconds, DeathCause? current_damage, out string message)
    {
        message = "";
        if (is_dead || !float.IsFinite(now_seconds)) return false;
        is_dead = true;
        float age = now_seconds - damage_seconds;
        DeathCause cause = current_damage ?? (age >= 0 && age <= 2f ? last_cause : default);
        message = format(victim, cause);
        pending_seconds = float.NegativeInfinity;
        return true;
    }

    public static string format(string victim, DeathCause cause)
    {
        string name = safe_name(victim);
        if (cause.kind == DeathKind.Unknown) return $"{name} died.";
        if (cause.self_inflicted) return $"{name} died from a self-inflicted injury.";
        string attacker = string.IsNullOrEmpty(cause.attacker) ? "" : safe_name(cause.attacker);
        string action = cause.kind switch
        {
            DeathKind.Gunshot => "was shot",
            DeathKind.Explosion => "was killed in an explosion",
            DeathKind.Melee => "was beaten to death",
            DeathKind.SharpWeapon => "was killed with a sharp weapon",
            DeathKind.Vehicle => "was run over",
            _ => "was killed by an impact"
        };
        if (attacker.Length == 0) return $"{name} {action}.";
        if (cause.kind == DeathKind.Explosion) return $"{name} {action} caused by {attacker}.";
        if (cause.kind == DeathKind.Impact) return $"{name} was killed by {attacker}.";
        return $"{name} {action} by {attacker}.";
    }

    public static string safe_name(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "Someone";
        var result = new StringBuilder(48);
        int count = Math.Min(value.Length, 128);
        for (int i = 0; i < count && result.Length < 48; i++)
        {
            char character = value[i];
            if (character is '<' or '>' || char.IsControl(character) ||
                char.GetUnicodeCategory(character) == UnicodeCategory.Format) continue;
            if (char.IsHighSurrogate(character))
            {
                if (i + 1 >= count || !char.IsLowSurrogate(value[i + 1]) || result.Length > 46) continue;
                result.Append(character).Append(value[++i]);
            }
            else if (!char.IsLowSurrogate(character)) result.Append(character);
        }
        string name = result.ToString().Trim();
        return name.Length == 0 ? "Someone" : name;
    }
}
