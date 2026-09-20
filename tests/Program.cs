using System;
using DeathNotices;

internal static class Program
{
    private static void Main()
    {
        var tracker = new DeathTracker();
        tracker.reset(alive: true);
        var police = new DeathCause(DeathKind.Gunshot, "police");
        tracker.observe_impact(1, 20, police, 1);
        DeathCause shot = tracker.consume_impact(20, 1.2f);
        require(shot == police, "Matching damage retains police attribution");
        tracker.record_damage(shot, true, 1.2f);
        require(tracker.try_death("Alex", 1.3f, null, 1, out string notice) && notice == "Alex brought confidence to a gunfight. Courtesy of police.", "Confirmed fatal damage notice");
        require(!tracker.try_death("Alex", 1.4f, null, 1, out _), "Duplicate death suppressed");
        tracker.reset(alive: true);
        require(tracker.try_death("Alex", 2, null, 1, out notice) && notice == "Alex has left the group chat.", "Revive clears stale attacker and permits next death");
        tracker.reset(alive: false);
        require(!tracker.try_death("Alex", 2, null, 1, out _), "Joining an already-dead player does not replay death");
        tracker.reset(alive: true);
        tracker.observe_impact(2, 20, police, 1);
        require(tracker.consume_impact(10, 1.1f).kind == DeathKind.Unknown, "Different damage amount cannot inherit attribution");
        require(tracker.consume_impact(20, 1.2f).kind == DeathKind.Unknown, "Unmatched damage consumes old candidate");
        tracker.observe_impact(2, 20, police, 1.3f);
        require(tracker.consume_impact(20, 1.4f).kind == DeathKind.Unknown, "Duplicate impact cannot be reused");
        tracker.observe_impact(3, 20, police, 2);
        require(tracker.consume_impact(20, 4).kind == DeathKind.Unknown, "Expired impact ignored");
        tracker.record_damage(police, true, 4);
        tracker.record_damage(default, true, 4.1f);
        require(tracker.try_death("Sam", 4.2f, null, 1, out notice) && notice == "Sam has left the group chat.", "Untyped later damage replaces prior shooter");
        tracker.reset(alive: true);
        tracker.record_damage(police, true, 1);
        require(tracker.try_death("Sam", 5, null, 1, out notice) && notice == "Sam has left the group chat.", "Old injury is not guessed as cause");
        tracker.reset(alive: true);
        require(tracker.try_death("Sam", 5, new DeathCause(DeathKind.Gunshot, "Riley"), 1, out notice) && notice == "Sam brought confidence to a gunfight. Courtesy of Riley.", "Death inside damage call has immediate cause");
        require(DeathTracker.format("Alex", new DeathCause(DeathKind.Explosion)) == "Alex was killed in an explosion.", "Unknown explosion attacker omitted");
        require(DeathTracker.format("Alex", new DeathCause(DeathKind.Vehicle, "Riley")) == "Alex was run over by Riley.", "Vehicle driver notice");
        require(DeathTracker.format("Alex", new DeathCause(DeathKind.Gunshot, "Alex", true)) == "Alex died from a self-inflicted injury.", "Self attribution");
        require(!DeathTracker.safe_name("<size=999>Alex</size>\n\u202e").Contains('<'), "Names cannot inject rich text");
        require(DeathTracker.safe_name(new string('a', 1000)).Length == 48, "Names bounded");
        require(DeathTracker.safe_name(null) == "Someone", "Missing name fallback");
        string unicode = DeathTracker.safe_name(new string('a', 47) + "😀");
        require(unicode.Length == 47, "Truncation does not split surrogate pairs");
        tracker.reset(alive: true);
        tracker.record_damage(police, false, 1);
        require(tracker.try_death("Sam", 1.1f, null, 1, out notice) && notice == "Sam has left the group chat.", "Nonfatal damage cannot explain an unrelated death");
        tracker.reset(alive: true);
        tracker.observe_impact(5, 20, police, 1);
        require(tracker.try_death("Sam", 1.1f, null, 1, out notice) && notice == "Sam has left the group chat.", "Unconfirmed impact is never used as a fallback");
        tracker.reset(alive: true);
        tracker.observe_impact(6, float.NaN, police, 1);
        require(tracker.consume_impact(20, 1.1f).kind == DeathKind.Unknown, "Invalid damage ignored");
        var other = new DeathTracker();
        other.reset(alive: true);
        require(other.try_death("Other", 5, null, 1, out notice) && notice == "Other has left the group chat.", "Players have independent attribution");
        var typed_police = new DeathCause(DeathKind.Gunshot, "police", false, AttackerKind.Police);
        require(DeathTracker.format_notice("Alex", typed_police, 1, 0).Contains("disputed the charges"), "Police gunshot joke");
        require(!DeathTracker.format_notice("Alex", new DeathCause(DeathKind.Melee, "police", false, AttackerKind.Police), 1, 0)
            .Contains("charges"), "Police gunshot joke never applies to melee");
        var player_police = new DeathCause(DeathKind.Gunshot, "police", false, AttackerKind.Player);
        string friendly = DeathTracker.format_notice("Alex", player_police, 1, 0);
        require(friendly.Contains("friendly fire") && !friendly.Contains("disputed"), "Names cannot spoof attacker type");
        require(friendly.Contains("Courtesy of police"), "Friendly-fire variant retains attacker");
        require(DeathTracker.format_notice("Alex", default, 1, 0).Contains("administrative problem"), "Unknown cause joke avoids invented cause");
        require(!DeathTracker.format_notice("Alex", player_police with { self_inflicted = true }, 1, 0).Contains("Friendly fire"),
            "Self damage is not blamed on a teammate");
        for (int roll = 0; roll < 4; roll++)
            for (int previous = 0; previous < roll; previous++)
                require(DeathTracker.format_notice("Alex", typed_police, 1, roll) !=
                    DeathTracker.format_notice("Alex", typed_police, 1, previous), "Police variants are distinct");
        tracker.reset(alive: true);
        for (int death = 1; death <= 3; death++)
        {
            require(tracker.try_death("Alex", death, typed_police, 0, out notice), "Each revived life gets one notice");
            require(notice.Contains("employed") == (death == 3), "Repeated-death joke survives revives");
            require(!tracker.try_death("Alex", death, typed_police, 0, out _), "Duplicate does not advance death count");
            tracker.revive();
        }
        tracker.reset(alive: true);
        require(tracker.try_death("Alex", 4, typed_police, 0, out notice) && !notice.Contains("employed"),
            "Scene or player replacement clears joke history");
        foreach (DeathKind kind in Enum.GetValues<DeathKind>())
        {
            string bounded = DeathTracker.format_notice(new string('v', 100),
                new DeathCause(kind, new string('a', 100), false, AttackerKind.Player), 3, 0);
            require(bounded.Length <= 192, "Jokes with maximum names fit the notice queue");
        }
        var ledger = new CasinoLedger();
        require(ledger.settle(100, 0) && ledger.net == -100, "Losing wager counts once");
        require(!ledger.settle(100, 200) && ledger.net == 0, "Returned stake is not counted as profit");
        require(!ledger.settle(100, 100) && ledger.net == 0, "Push neither wins nor loses money");
        require(!ledger.settle(100, 250) && ledger.net == 150, "Blackjack profit excludes original stake");
        require(ledger.settle(20, 0) && ledger.net == 130, "Loss can leave player ahead overall");
        require(CasinoLedger.report("Alex", ledger.net).Contains("Still up $130.00"), "Winning total gets different roast");
        require(CasinoLedger.report("Alex", -1234).Contains("Down $1,234.00"), "Net loss report");
        ledger.load(-20);
        require(ledger.net == -20, "Saved net restores across sessions");
        bool rejected = false;
        try { ledger.settle(float.NaN, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        require(rejected && ledger.net == -20, "Invalid result cannot corrupt totals");
        var queue = new NoticeQueue();
        for (int i = 0; i < 20; i++) queue.add($"Player {i} died.", 1, "Death notice");
        require(queue.try_take(2, out notice, out _) && notice == "Player 4 died.", "Overflow retains newest sixteen notices");
        require(!queue.try_take(12, out _, out _), "Expired notices discarded");
        queue.add("Alex died.", 20, "Death notice");
        queue.reset();
        require(!queue.try_take(20, out _, out _), "Scene change clears notices");
        queue.add("Alex is down $10.", 30, "Casino report");
        require(queue.try_take(30, out notice, out string title) && title == "Casino report", "Casino reports never use death title");
        foreach (DeathKind kind in Enum.GetValues<DeathKind>())
            for (int variant = 0; variant < 4; variant++)
                require(DeathTracker.format_notice(new string('v', 100),
                    new DeathCause(kind, new string('a', 100), false, AttackerKind.Player), 1, variant).Length <= 192,
                    "All joke variants fit with maximum names");
        var top_center = new NoticePlacement(NoticePosition.TopCenter);
        require(top_center.anchor_x == 0.5f && top_center.anchor_y == 1f && top_center.row_x() == 0f, "Top centre anchors to the middle of the top edge");
        require(top_center.row_y(0f) == -72f && top_center.row_y(40f) == -112f, "Top placements stack downward from the margin");
        var bottom_right = new NoticePlacement(NoticePosition.BottomRight, margin_x: 30f, margin_y: 100f);
        require(bottom_right.anchor_x == 1f && bottom_right.anchor_y == 0f && bottom_right.row_x() == -30f, "Right placements inset from the right edge");
        require(bottom_right.row_y(40f) == 140f, "Bottom placements stack upward from the margin");
        NoticePlacement repaired = new NoticePlacement((NoticePosition)99, float.NaN, -5f).validated();
        require(repaired.position == NoticePosition.TopCenter && repaired.margin_x == 24f && repaired.margin_y == 0f, "Invalid placement preferences are repaired");
        Console.WriteLine("Death attribution, duplicate suppression, lifecycle, names, and queue checks passed.");
    }

    private static void require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
