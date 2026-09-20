using System;
using Il2CppScheduleOne.UI;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathNotices;

internal static class NoticeFeed
{
    private sealed class Row
    {
        public readonly GameObject root;
        public readonly RectTransform rect;
        public readonly CanvasGroup group;
        public readonly TextMeshProUGUI text;
        public float shown_seconds = float.NegativeInfinity;
        public float height;

        public Row(GameObject root, RectTransform rect, CanvasGroup group, TextMeshProUGUI text)
        {
            this.root = root;
            this.rect = rect;
            this.group = group;
            this.text = text;
        }
    }
    private const float row_width = 640f;
    private const float row_gap = 6f;
    private const float padding_x = 18f;
    private const float padding_y = 9f;
    private const float top_offset = 72f;
    private const float lifetime_seconds = 7f;
    private const float fade_in_seconds = 0.2f;
    private const float fade_out_seconds = 0.6f;
    private static readonly Row?[] rows = new Row?[4];
    private static GameObject? canvas_object;

    public static void reset()
    {
        if (canvas_object != null) UnityEngine.Object.Destroy(canvas_object);
        canvas_object = null;
        Array.Clear(rows, 0, rows.Length);
    }

    public static void show(string title, string message, float now_seconds)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message) || !float.IsFinite(now_seconds)) return;
        if (canvas_object == null) build();
        Row? oldest = null;
        foreach (Row? row in rows)
        {
            if (row != null && (oldest == null || row.shown_seconds < oldest.shown_seconds)) oldest = row;
        }
        if (oldest == null) return;
        string text = $"<b><color=#F2B84B>{title}</color></b>  {message}";
        float text_height = oldest.text.GetPreferredValues(text, row_width - padding_x * 2f, 0f).y;
        if (!float.IsFinite(text_height)) return;
        oldest.text.text = text;
        oldest.height = Math.Clamp(text_height, 20f, 200f) + padding_y * 2f;
        oldest.rect.sizeDelta = new Vector2(row_width, oldest.height);
        oldest.shown_seconds = now_seconds;
        oldest.group.alpha = 0f;
        oldest.root.SetActive(true);
        layout();
    }

    public static void update(float now_seconds)
    {
        if (canvas_object == null || !float.IsFinite(now_seconds)) return;
        bool expired = false;
        foreach (Row? row in rows)
        {
            if (row == null || float.IsNegativeInfinity(row.shown_seconds)) continue;
            float age_seconds = now_seconds - row.shown_seconds;
            if (age_seconds > lifetime_seconds)
            {
                row.shown_seconds = float.NegativeInfinity;
                row.root.SetActive(false);
                expired = true;
                continue;
            }
            float fade_in = age_seconds / fade_in_seconds;
            float fade_out = (lifetime_seconds - age_seconds) / fade_out_seconds;
            row.group.alpha = Math.Clamp(Math.Min(fade_in, fade_out), 0f, 1f);
        }
        if (expired) layout();
    }

    private static void layout()
    {
        foreach (Row? row in rows)
        {
            if (row == null || float.IsNegativeInfinity(row.shown_seconds)) continue;
            float offset = top_offset;
            foreach (Row? other in rows)
            {
                if (other == null || other == row || float.IsNegativeInfinity(other.shown_seconds)) continue;
                if (other.shown_seconds < row.shown_seconds) offset += other.height + row_gap;
            }
            row.rect.anchoredPosition = new Vector2(0f, -offset);
        }
    }

    private static void build()
    {
        canvas_object = new GameObject("DeathNoticesFeed");
        Canvas canvas = canvas_object.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = canvas_object.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        TMP_FontAsset? font = null;
        if (NotificationsManager.InstanceExists && NotificationsManager.Instance.NotificationPrefab != null)
            font = NotificationsManager.Instance.NotificationPrefab.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
        for (int i = 0; i < rows.Length; i++)
        {
            var root = new GameObject("Notice");
            root.transform.SetParent(canvas_object.transform, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.07f, 0.82f);
            background.raycastTarget = false;
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var label = new GameObject("Text");
            label.transform.SetParent(root.transform, false);
            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = 22f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            RectTransform text_rect = text.rectTransform;
            text_rect.anchorMin = Vector2.zero;
            text_rect.anchorMax = Vector2.one;
            text_rect.offsetMin = new Vector2(padding_x, padding_y);
            text_rect.offsetMax = new Vector2(-padding_x, -padding_y);
            root.SetActive(false);
            rows[i] = new Row(root, rect, group, text);
        }
    }
}
