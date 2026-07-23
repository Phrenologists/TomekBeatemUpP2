#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackData))]
public class AttackDataEditor : Editor
{
    private bool _previewFacingRight = true;
    private bool _showPreview = true;

    private const float PreviewWidth = 300f;
    private const float PreviewHeight = 200f;

    private const float Scale = 50f;
    private static readonly Vector2 Origin = new Vector2(PreviewWidth * 0.5f, PreviewHeight * 0.65f);

    private static readonly Rect CharacterBody = new Rect(-0.25f, 0f, 0.5f, 1.6f);


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Hitbox Preview", EditorStyles.boldLabel);

        _showPreview = EditorGUILayout.Foldout(_showPreview, "Show Preview", true);
        if (!_showPreview) return;

        AttackData data = (AttackData)target;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Facing Direction", GUILayout.Width(120));
        if (GUILayout.Toggle(_previewFacingRight, "Right →", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            _previewFacingRight = true;
        if (GUILayout.Toggle(!_previewFacingRight, "← Left", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            _previewFacingRight = false;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        Rect canvasRect = GUILayoutUtility.GetRect(PreviewWidth, PreviewHeight);
        DrawPreviewCanvas(canvasRect, data);

        EditorGUILayout.Space(4);
        DrawFrameDataSummary(data);
    }


    private void DrawPreviewCanvas(Rect canvas, AttackData data)
    {
        if (Event.current.type != EventType.Repaint) return;

        EditorGUI.DrawRect(canvas, new Color(0.12f, 0.12f, 0.12f));

        DrawGrid(canvas);

        DrawWorldRect(canvas, CharacterBody, new Color(0.3f, 0.3f, 0.35f, 0.8f), false);

        DrawFacingArrow(canvas);

        float sign = _previewFacingRight ? 1f : -1f;
        Rect hitbox = new Rect(
            data.hitboxOffset.x * sign - data.hitboxSize.x * 0.5f,
            data.hitboxOffset.y - data.hitboxSize.y * 0.5f,
            data.hitboxSize.x,
            data.hitboxSize.y);

        DrawWorldRect(canvas, hitbox, new Color(1f, 0.2f, 0.2f, 0.35f), true);
        DrawWorldRectOutline(canvas, hitbox, new Color(1f, 0.2f, 0.2f, 1f));

        DrawCrosshair(canvas, Vector2.zero, new Color(0.5f, 0.5f, 0.5f, 0.6f), 6f);

        Vector2 hitboxCenter = new Vector2(data.hitboxOffset.x * sign, data.hitboxOffset.y);
        DrawCrosshair(canvas, hitboxCenter, new Color(1f, 0.4f, 0.4f, 0.9f), 4f);

        DrawBorder(canvas, new Color(0.4f, 0.4f, 0.4f));

        DrawHitboxDimensions(canvas, hitbox, data);
    }


    private void DrawWorldRect(Rect canvas, Rect worldRect, Color color, bool filled)
    {
        Rect pixelRect = WorldToPixel(canvas, worldRect);
        if (filled)
            EditorGUI.DrawRect(pixelRect, color);
    }

    private void DrawWorldRectOutline(Rect canvas, Rect worldRect, Color color)
    {
        Rect r = WorldToPixel(canvas, worldRect);
        Handles.color = color;
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[]
            {
                new Vector3(r.xMin, r.yMin),
                new Vector3(r.xMax, r.yMin),
                new Vector3(r.xMax, r.yMax),
                new Vector3(r.xMin, r.yMax)
            },
            Color.clear,
            color);
    }

    private void DrawCrosshair(Rect canvas, Vector2 worldPos, Color color, float pixelRadius)
    {
        Vector2 px = WorldToPixel(canvas, worldPos);
        Handles.color = color;
        Handles.DrawLine(new Vector3(px.x - pixelRadius, px.y), new Vector3(px.x + pixelRadius, px.y));
        Handles.DrawLine(new Vector3(px.x, px.y - pixelRadius), new Vector3(px.x, px.y + pixelRadius));
    }

    private void DrawFacingArrow(Rect canvas)
    {
        float arrowX = _previewFacingRight ? 0.5f : -0.5f;
        Vector2 start = WorldToPixel(canvas, new Vector2(0f, 1.8f));
        Vector2 end = WorldToPixel(canvas, new Vector2(arrowX, 1.8f));

        Handles.color = new Color(0.7f, 0.9f, 1f, 0.8f);
        Handles.DrawLine(start, end);
        Vector2 dir = (end - start).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * 4f;
        Handles.DrawLine(end, end - dir * 8f + perp);
        Handles.DrawLine(end, end - dir * 8f - perp);
    }

    private void DrawGrid(Rect canvas)
    {
        Handles.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        for (float x = -3f; x <= 3f; x += 0.5f)
        {
            Vector2 top = WorldToPixel(canvas, new Vector2(x, 2.5f));
            Vector2 bot = WorldToPixel(canvas, new Vector2(x, -1f));
            Handles.DrawLine(top, bot);
        }
        for (float y = -1f; y <= 2.5f; y += 0.5f)
        {
            Vector2 left = WorldToPixel(canvas, new Vector2(-3f, y));
            Vector2 right = WorldToPixel(canvas, new Vector2(3f, y));
            Handles.DrawLine(left, right);
        }
    }

    private void DrawBorder(Rect canvas, Color color)
    {
        Handles.color = color;
        Handles.DrawLine(new Vector3(canvas.xMin, canvas.yMin), new Vector3(canvas.xMax, canvas.yMin));
        Handles.DrawLine(new Vector3(canvas.xMax, canvas.yMin), new Vector3(canvas.xMax, canvas.yMax));
        Handles.DrawLine(new Vector3(canvas.xMax, canvas.yMax), new Vector3(canvas.xMin, canvas.yMax));
        Handles.DrawLine(new Vector3(canvas.xMin, canvas.yMax), new Vector3(canvas.xMin, canvas.yMin));
    }

    private void DrawHitboxDimensions(Rect canvas, Rect worldRect, AttackData data)
    {
        Rect r = WorldToPixel(canvas, worldRect);
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.8f, 0.8f) },
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter
        };
        string dims = $"{data.hitboxSize.x:F2} × {data.hitboxSize.y:F2}";
        GUI.Label(r, dims, style);
    }

    private Rect WorldToPixel(Rect canvas, Rect worldRect)
    {
        float px = canvas.x + Origin.x + worldRect.x * Scale;
        float py = canvas.y + Origin.y - (worldRect.y + worldRect.height) * Scale;
        float pw = worldRect.width * Scale;
        float ph = worldRect.height * Scale;
        return new Rect(px, py, pw, ph);
    }

    private Vector2 WorldToPixel(Rect canvas, Vector2 worldPos)
    {
        return new Vector2(
            canvas.x + Origin.x + worldPos.x * Scale,
            canvas.y + Origin.y - worldPos.y * Scale);
    }
    private void DrawFrameDataSummary(AttackData data)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        float total = data.TotalDuration * 1000f;

        Rect barRect = GUILayoutUtility.GetRect(0, 16f, GUILayout.ExpandWidth(true));
        float barW = barRect.width;
        float startW = (data.StartupDuration / data.TotalDuration) * barW;
        float activeW = (data.ActiveDuration / data.TotalDuration) * barW;
        float recovW = (data.RecoveryDuration / data.TotalDuration) * barW;

        EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, startW, barRect.height), new Color(1f, 0.8f, 0.1f, 0.8f));
        EditorGUI.DrawRect(new Rect(barRect.x + startW, barRect.y, activeW, barRect.height), new Color(1f, 0.2f, 0.2f, 0.8f));
        EditorGUI.DrawRect(new Rect(barRect.x + startW + activeW, barRect.y, recovW, barRect.height), new Color(0.3f, 0.7f, 1f, 0.8f));

        var centeredMini = new GUIStyle(EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Startup  {data.startupFrames}f  ({data.StartupDuration * 1000f:F0}ms)",
            centeredMini, GUILayout.ExpandWidth(true));
        GUILayout.Label($"Active  {data.activeFrames}f  ({data.ActiveDuration * 1000f:F0}ms)",
            centeredMini, GUILayout.ExpandWidth(true));
        GUILayout.Label($"Recovery  {data.recoveryFrames}f  ({data.RecoveryDuration * 1000f:F0}ms)",
            centeredMini, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Total: {data.TotalDuration * 1000f:F0} ms   @ {data.targetFPS:F0} fps",
            centeredMini);

        EditorGUILayout.EndVertical();
    }
}
#endif
