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
        require(tracker.try_death("Alex", 1.3f, null, out string notice) && notice == "Alex was shot by police.", "Confirmed fatal damage notice");
        require(!tracker.try_death("Alex", 1.4f, null, out _), "Duplicate death suppressed");
        tracker.reset(alive: true);
        require(tracker.try_death("Alex", 2, null, out notice) && notice == "Alex died.", "Revive clears stale attacker and permits next death");
        tracker.reset(alive: false);
        require(!tracker.try_death("Alex", 2, null, out _), "Joining an already-dead player does not replay death");
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
        require(tracker.try_death("Sam", 4.2f, null, out notice) && notice == "Sam died.", "Untyped later damage replaces prior shooter");
        tracker.reset(alive: true);
        tracker.record_damage(police, true, 1);
        require(tracker.try_death("Sam", 5, null, out notice) && notice == "Sam died.", "Old injury is not guessed as cause");
        tracker.reset(alive: true);
        require(tracker.try_death("Sam", 5, new DeathCause(DeathKind.Gunshot, "Riley"), out notice) && notice == "Sam was shot by Riley.", "Death inside damage call has immediate cause");
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
        require(tracker.try_death("Sam", 1.1f, null, out notice) && notice == "Sam died.", "Nonfatal damage cannot explain an unrelated death");
        tracker.reset(alive: true);
        tracker.observe_impact(5, 20, police, 1);
        require(tracker.try_death("Sam", 1.1f, null, out notice) && notice == "Sam died.", "Unconfirmed impact is never used as a fallback");
        tracker.reset(alive: true);
        tracker.observe_impact(6, float.NaN, police, 1);
        require(tracker.consume_impact(20, 1.1f).kind == DeathKind.Unknown, "Invalid damage ignored");
        var other = new DeathTracker();
        other.reset(alive: true);
        require(other.try_death("Other", 5, null, out notice) && notice == "Other died.", "Players have independent attribution");
        var queue = new NoticeQueue();
        for (int i = 0; i < 20; i++) queue.add($"Player {i} died.", 1);
        require(queue.try_take(2, out notice) && notice == "Player 4 died.", "Overflow retains newest sixteen notices");
        require(!queue.try_take(12, out _), "Expired notices discarded");
        queue.add("Alex died.", 20);
        queue.reset();
        require(!queue.try_take(20, out _), "Scene change clears notices");
        Console.WriteLine("Death attribution, duplicate suppression, lifecycle, names, and queue checks passed.");
    }

    private static void require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
