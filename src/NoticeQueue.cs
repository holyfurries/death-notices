using System;

namespace DeathNotices;

internal sealed class NoticeQueue
{
    private readonly (string message, float expires_seconds, string title)[] entries = new (string, float, string)[16];
    private int read_index;
    private int count;

    public void reset()
    {
        Array.Clear(entries, 0, entries.Length);
        read_index = 0;
        count = 0;
    }

    public void add(string message, float now_seconds, string title)
    {
        if (string.IsNullOrEmpty(message) || message.Length > 192 || string.IsNullOrEmpty(title) || title.Length > 32 || !float.IsFinite(now_seconds)) return;
        if (count == entries.Length)
        {
            read_index = (read_index + 1) % entries.Length;
            count--;
        }
        entries[(read_index + count) % entries.Length] = (message, now_seconds + 10f, title);
        count++;
    }

    public bool try_take(float now_seconds, out string message, out string title)
    {
        message = "";
        title = "";
        if (!float.IsFinite(now_seconds)) return false;
        for (int attempt = 0; attempt < entries.Length && count > 0; attempt++)
        {
            var entry = entries[read_index];
            entries[read_index] = default;
            read_index = (read_index + 1) % entries.Length;
            count--;
            if (now_seconds > entry.expires_seconds) continue;
            message = entry.message;
            title = entry.title;
            return true;
        }
        return false;
    }
}
