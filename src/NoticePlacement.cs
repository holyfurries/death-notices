using System;

namespace DeathNotices;

internal enum NoticePosition { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight }

internal readonly record struct NoticePlacement(NoticePosition position = NoticePosition.TopCenter,
    float margin_x = 24f, float margin_y = 72f)
{
    public bool is_top => position is NoticePosition.TopLeft or NoticePosition.TopCenter or NoticePosition.TopRight;

    public float anchor_x => position switch
    {
        NoticePosition.TopLeft or NoticePosition.BottomLeft => 0f,
        NoticePosition.TopRight or NoticePosition.BottomRight => 1f,
        _ => 0.5f
    };

    public float anchor_y => is_top ? 1f : 0f;

    public NoticePlacement validated()
    {
        return this with
        {
            position = Enum.IsDefined(position) ? position : NoticePosition.TopCenter,
            margin_x = float.IsFinite(margin_x) ? Math.Clamp(margin_x, 0f, 600f) : 24f,
            margin_y = float.IsFinite(margin_y) ? Math.Clamp(margin_y, 0f, 900f) : 72f
        };
    }

    public float row_x()
    {
        if (anchor_x == 0f) return margin_x;
        if (anchor_x == 1f) return -margin_x;
        return 0f;
    }

    public float row_y(float stack_offset)
    {
        if (!float.IsFinite(stack_offset) || stack_offset < 0f) throw new ArgumentOutOfRangeException(nameof(stack_offset));
        float distance = margin_y + stack_offset;
        return is_top ? -distance : distance;
    }
}
