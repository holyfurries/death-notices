using System;
using System.Globalization;
using System.Text;

namespace DeathNotices;

internal enum DeathKind { Unknown, Gunshot, Explosion, Melee, SharpWeapon, Vehicle, Impact }
internal enum AttackerKind { Unknown, Player, Police, NPC }
internal readonly record struct DeathCause(DeathKind kind, string attacker = "", bool self_inflicted = false,
    AttackerKind attacker_kind = AttackerKind.Unknown);

internal sealed class DeathTracker
{
    private readonly int[] impact_ids = new int[8];
    private int death_count;
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
        death_count = 0;
        impact_count = 0;
        impact_index = 0;
        pending_cause = default;
        pending_amount = 0;
        pending_seconds = float.NegativeInfinity;
        last_cause = default;
        damage_seconds = float.NegativeInfinity;
        is_dead = !alive;
    }

    public void revive()
    {
        int previous_count = death_count;
        reset(alive: true);
        death_count = previous_count;
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

    public bool try_death(string victim, float now_seconds, DeathCause? current_damage, int variation, out string message)
    {
        message = "";
        if (is_dead || !float.IsFinite(now_seconds)) return false;
        is_dead = true;
        float age = now_seconds - damage_seconds;
        DeathCause cause = current_damage ?? (age >= 0 && age <= 2f ? last_cause : default);
        death_count = Math.Min(death_count + 1, 1000);
        message = format_notice(victim, cause, death_count, variation);
        pending_seconds = float.NegativeInfinity;
        return true;
    }

    public static string format_notice(string victim, DeathCause cause, int death_count, int variation)
    {
        int choice = (variation & int.MaxValue) % 4;
        string name = safe_name(victim);
        if (death_count >= 3 && death_count % 2 == 1 && choice == 0)
            return $"{format(victim, cause)} Keeping this notification system employed.";
        string line;
        if (cause.kind != DeathKind.Unknown && cause.self_inflicted)
            line = choice switch
            {
                0 => "was their own worst enemy.",
                1 => "filed a complaint against themselves.",
                2 => "lost a fight with their own decisions.",
                _ => "should not have been left unsupervised."
            };
        else if (cause.kind == DeathKind.Gunshot && cause.attacker_kind == AttackerKind.Police)
            line = choice switch
            {
                0 => "unsuccessfully disputed the charges.",
                1 => "brought a complaint to a police gunfight.",
                2 => "received the express arrest package.",
                _ => "will not be getting their deposit back from the police."
            };
        else line = (cause.kind, choice) switch
        {
            (DeathKind.Gunshot, 0) => "discovered bullets are not suggestions.",
            (DeathKind.Gunshot, 1) => "brought confidence to a gunfight.",
            (DeathKind.Gunshot, 2) => "forgot to decline incoming ammunition.",
            (DeathKind.Gunshot, _) => "tested the wrong end of a gun.",
            (DeathKind.Explosion, 0) => "became a group project.",
            (DeathKind.Explosion, 1) => "stood inside the recommended blast radius.",
            (DeathKind.Explosion, 2) => "went out with questionable timing.",
            (DeathKind.Explosion, _) => "has been distributed locally.",
            (DeathKind.Melee, 0) => "lost an argument at arm's length.",
            (DeathKind.Melee, 1) => "caught hands instead of a break.",
            (DeathKind.Melee, 2) => "failed the practical boxing exam.",
            (DeathKind.Melee, _) => "should have kept that thought to themselves.",
            (DeathKind.SharpWeapon, 0) => "lost a pointed discussion.",
            (DeathKind.SharpWeapon, 1) => "found the sharp end of the situation.",
            (DeathKind.SharpWeapon, 2) => "was not cut out for this.",
            (DeathKind.SharpWeapon, _) => "ignored a cutting remark.",
            (DeathKind.Vehicle, 0) => "lost the right-of-way dispute.",
            (DeathKind.Vehicle, 1) => "became a speed bump.",
            (DeathKind.Vehicle, 2) => "challenged traffic and finished second.",
            (DeathKind.Vehicle, _) => "forgot cars have the final say.",
            (DeathKind.Impact, 0) => "lost to an inanimate object.",
            (DeathKind.Impact, 1) => "failed a practical physics exam.",
            (DeathKind.Impact, 2) => "was on the receiving end of momentum.",
            (DeathKind.Impact, _) => "should have moved slightly to the left.",
            (_, 0) => "has become an administrative problem.",
            (_, 1) => "has left the group chat.",
            (_, 2) => "has been banned from the alive casino.",
            _ => "bet it all on red."
        };
        string credit = cause.kind != DeathKind.Unknown && !cause.self_inflicted &&
            !string.IsNullOrEmpty(cause.attacker) ? $" Courtesy of {safe_name(cause.attacker)}." : "";
        if (cause.attacker_kind == AttackerKind.Player && !cause.self_inflicted && cause.kind != DeathKind.Unknown && choice == 0)
            line = "discovered friendly fire isn't.";
        return $"{name} {line}{credit}";
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
