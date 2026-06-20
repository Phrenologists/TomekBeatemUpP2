using UnityEngine;

public class CombatDirectorDebug : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private ScreenCorner corner = ScreenCorner.TopLeft;
    [SerializeField] private float panelWidth = 220f;
    [SerializeField] private float margin = 10f;
    [SerializeField] private float backgroundAlpha = 0.75f;
    [SerializeField] private bool showRoleBreakdown = true;
    [SerializeField] private bool showPresenceList = true;
    [SerializeField] private bool showTokenTimestamp = true;

    private CombatDirector _director;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _smallStyle;
    private bool _stylesReady;

    private int _attackerCount;
    private int _flankerCount;
    private int _waiterCount;
    private int _otherCount;

    private float _lastGrantTime = -1f;

    private const float LineH = 16f;
    private const float SmallLineH = 13f;
    private const float BarH = 10f;
    private const float Padding = 5f;


    private void Awake()
    {
        _director = GetComponent<CombatDirector>();
        if (_director == null)
            Debug.LogWarning("CombatDirectorDebug: no CombatDirector on this GameObject.", this);

        if (_director != null)
            _director.OnBudgetChanged += _ => _lastGrantTime = Time.time;
    }

    private void OnGUI()
    {
        if (_director == null) return;

        InitStyles();
        CollectRoleCounts();

        float panelHeight = CalculatePanelHeight();
        Rect panel = GetPanelRect(panelWidth, panelHeight);

        GUI.color = new Color(0f, 0f, 0f, backgroundAlpha);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        float x = panel.x + Padding;
        float y = panel.y + Padding;

        GUI.color = new Color(1f, 0.7f, 0.2f);
        GUI.Label(new Rect(x, y, panelWidth, LineH), "COMBAT DIRECTOR", _headerStyle);
        y += LineH + 2f;

        GUI.color = new Color(0.4f, 0.4f, 0.4f);
        DrawHorizontalLine(new Rect(x, y, panelWidth - Padding * 2f, 1f));
        y += 4f;
        GUI.color = Color.white;

        float intensity = _director.Intensity;
        GUI.color = IntensityColour(intensity);
        GUI.Label(new Rect(x, y, panelWidth, LineH),
            $"Intensity  {intensity:F2}", _labelStyle);
        y += LineH;

        DrawBar(new Rect(x, y, panelWidth - Padding * 2f, BarH),
            intensity, IntensityColour(intensity));
        y += BarH + Padding;

        int current = _director.CurrentBudget;
        int max = _director.MaxBudget;
        float budgetFrac = max > 0 ? (float)current / max : 0f;

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, panelWidth, LineH),
            $"Budget  {current} / {max}", _labelStyle);
        y += LineH;

        DrawSegmentedBar(new Rect(x, y, panelWidth - Padding * 2f, BarH), current, max);
        y += BarH + Padding;

        if (showTokenTimestamp)
        {
            float ago = _lastGrantTime < 0f ? -1f : Time.time - _lastGrantTime;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            string agoStr = ago < 0f ? "never" : $"{ago:F1}s ago";
            GUI.Label(new Rect(x, y, panelWidth, SmallLineH),
                $"Last grant: {agoStr}", _smallStyle);
            y += SmallLineH;
        }

        GUI.color = Color.white;
        DrawSectionDivider(ref y, x, panelWidth);

        GUI.Label(new Rect(x, y, panelWidth, LineH),
            $"Enemies active", _labelStyle);
        y += LineH;

        if (showRoleBreakdown)
        {
            DrawRoleRow(ref y, x, "Attacker", _attackerCount, new Color(1f, 0.3f, 0.3f));
            DrawRoleRow(ref y, x, "Flanker", _flankerCount, new Color(1f, 0.6f, 0.1f));
            DrawRoleRow(ref y, x, "Waiter", _waiterCount, new Color(0.5f, 0.8f, 0.5f));
            DrawRoleRow(ref y, x, "Other", _otherCount, new Color(0.6f, 0.6f, 0.6f));
        }

        if (showPresenceList)
        {
            DrawSectionDivider(ref y, x, panelWidth);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, panelWidth, LineH), "Present types", _labelStyle);
            y += LineH;

            foreach (string tag in _director.PresentTypes())
            {
                int count = _director.CountPresent(tag);
                GUI.color = new Color(0.8f, 0.9f, 1f);
                GUI.Label(new Rect(x + 8f, y, panelWidth, SmallLineH),
                    $"• {tag}  ×{count}", _smallStyle);
                y += SmallLineH;
            }

            if (_director.CountPresent("") == 0)
            {
                bool any = false;
                foreach (var _ in _director.PresentTypes()) { any = true; break; }
                if (!any)
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    GUI.Label(new Rect(x + 8f, y, panelWidth, SmallLineH),
                        "(none)", _smallStyle);
                    y += SmallLineH;
                }
            }
        }

        GUI.color = Color.white;
    }


    private float CalculatePanelHeight()
    {
        float h = Padding * 2f;
        h += LineH + 2f + 4f;
        h += LineH + BarH + Padding;
        h += LineH + BarH + Padding;
        if (showTokenTimestamp) h += SmallLineH;
        h += 4f + 2f;
        h += LineH;
        if (showRoleBreakdown) h += SmallLineH * 4f;
        if (showPresenceList)
        {
            h += 4f + 2f + LineH;
            int typeCount = 0;
            foreach (var _ in _director.PresentTypes()) typeCount++;
            h += SmallLineH * Mathf.Max(1, typeCount);
        }
        return h;
    }

    private Rect GetPanelRect(float w, float h)
    {
        return corner switch
        {
            ScreenCorner.TopLeft => new Rect(margin, margin, w, h),
            ScreenCorner.TopRight => new Rect(Screen.width - w - margin, margin, w, h),
            ScreenCorner.BottomLeft => new Rect(margin, Screen.height - h - margin, w, h),
            ScreenCorner.BottomRight => new Rect(Screen.width - w - margin,
                                                  Screen.height - h - margin, w, h),
            _ => new Rect(margin, margin, w, h)
        };
    }

    private void DrawBar(Rect rect, float fraction, Color fillColor)
    {
        GUI.color = new Color(0.15f, 0.15f, 0.15f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x, rect.y,
            rect.width * Mathf.Clamp01(fraction), rect.height),
            Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawSegmentedBar(Rect rect, int current, int max)
    {
        GUI.color = new Color(0.15f, 0.15f, 0.15f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        if (max <= 0) return;

        float segW = (rect.width - (max - 1)) / max;

        for (int i = 0; i < max; i++)
        {
            float segX = rect.x + i * (segW + 1f);
            bool filled = i < current;
            GUI.color = filled
                ? new Color(0.3f, 0.8f, 1f)
                : new Color(0.2f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(segX, rect.y, segW, rect.height),
                Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }

    private void DrawHorizontalLine(Rect rect)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }

    private void DrawSectionDivider(ref float y, float x, float w)
    {
        y += 3f;
        GUI.color = new Color(0.35f, 0.35f, 0.35f);
        DrawHorizontalLine(new Rect(x, y, w - Padding * 2f, 1f));
        y += 4f;
        GUI.color = Color.white;
    }

    private void DrawRoleRow(ref float y, float x, string label, int count, Color color)
    {
        GUI.color = count > 0 ? color : new Color(0.4f, 0.4f, 0.4f);
        GUI.Label(new Rect(x + 8f, y, panelWidth, SmallLineH),
            $"• {label,-12} {count}", _smallStyle);
        y += SmallLineH;
    }


    private void CollectRoleCounts()
    {
        _attackerCount = 0;
        _flankerCount = 0;
        _waiterCount = 0;
        _otherCount = 0;

        var brains = FindObjectsByType<EnemyAIBrain>(FindObjectsSortMode.None);
        foreach (var brain in brains)
        {
            switch (brain.CurrentRole)
            {
                case EnemyRole.Attacker: _attackerCount++; break;
                case EnemyRole.Flanker: _flankerCount++; break;
                case EnemyRole.Waiter: _waiterCount++; break;
                default: _otherCount++; break;
            }
        }
    }

    private static Color IntensityColour(float intensity)
    {
        return Color.Lerp(
            Color.Lerp(new Color(0.3f, 0.9f, 0.3f), new Color(1f, 0.8f, 0.1f), intensity * 2f),
            new Color(1f, 0.2f, 0.2f),
            Mathf.Max(0f, intensity * 2f - 1f));
    }


    private void InitStyles()
    {
        if (_stylesReady) return;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold
        };
        _smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Normal
        };
        _stylesReady = true;
    }
}

public enum ScreenCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
