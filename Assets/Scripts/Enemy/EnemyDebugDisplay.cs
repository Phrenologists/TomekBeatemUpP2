using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyDebugDisplay : MonoBehaviour
{
    public static bool ShowOverlay = true;
    public static bool ShowGizmos = true;
    public static bool ShowHealthBar = true;
    public static bool ShowTokens = true;

    private EnemyController _controller;
    private EnemyAIBrain _brain;
    private EnemyStats _stats;

    private GUIStyle _labelStyle;
    private bool _styleReady;

    private const float OverlayWidth = 160f;
    private const float OverlayHeight = 72f;
    private const float OverlayYOffset = 80f;

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
        _brain = GetComponent<EnemyAIBrain>();
        _stats = _controller.stats;
    }


    private void OnGUI()
    {
        if (!ShowOverlay) return;
        if (_controller == null || _brain == null) return;

        InitStyle();

        Vector3 worldPos = transform.position + Vector3.up * 0.5f;
        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPos)
            : Vector3.zero;

        if (screenPos.z < 0) return;

        float screenY = Screen.height - screenPos.y;

        Rect bgRect = new Rect(
            screenPos.x - OverlayWidth * 0.5f,
            screenY - OverlayYOffset,
            OverlayWidth,
            OverlayHeight);

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        float lineH = 14f;
        float x = bgRect.x + 4f;
        float y = bgRect.y + 3f;

        GUI.color = StateColour(_controller.CurrentState);
        GUI.Label(new Rect(x, y, OverlayWidth, lineH),
            $"State: {_controller.CurrentState}", _labelStyle);
        y += lineH;

        GUI.color = BrainColour(_brain.CurrentBrain);
        GUI.Label(new Rect(x, y, OverlayWidth, lineH),
            $"Brain: {_brain.CurrentBrain}", _labelStyle);
        y += lineH;

        GUI.color = RoleColour(_brain.CurrentRole);
        GUI.Label(new Rect(x, y, OverlayWidth, lineH),
            $"Role:  {_brain.CurrentRole}", _labelStyle);
        y += lineH;

        GUI.color = Color.white;

        if (ShowHealthBar)
        {
            int maxHp = _stats != null ? _stats.maxHealth : 100;
            float hpFrac = maxHp > 0 ? (float)_controller.CurrentHealth / maxHp : 0f;
            DrawMiniBar(new Rect(x, y, OverlayWidth - 8f, 8f), hpFrac, Color.red, "HP");
            y += 11f;
        }

        if (ShowTokens && CombatDirector.Instance != null)
        {
            int tokens = CombatDirector.Instance.GetHeldTokens(_brain);
            GUI.color = tokens > 0 ? Color.yellow : Color.gray;
            GUI.Label(new Rect(x, y, OverlayWidth, lineH),
                $"Tokens held: {tokens}", _labelStyle);
        }

        GUI.color = Color.white;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!ShowGizmos) return;
        if (_controller == null || _brain == null) return;

        Vector3 spherePos = transform.position + Vector3.up * 1.2f;
        Gizmos.color = RoleColour(_brain.CurrentRole);
        Gizmos.DrawSphere(spherePos, 0.12f);

        Vector3 labelPos = transform.position + Vector3.up * 1.5f;
        string label = $"{_controller.CurrentState}\n{_brain.CurrentBrain}\n{_brain.CurrentRole}";
        Handles.Label(labelPos, label, GetSceneStyle(_brain.CurrentRole));

        //Draw line to player, change color based on enemy state
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            bool active = _brain.CurrentBrain == BrainState.Pursuing ||
                          _brain.CurrentBrain == BrainState.Attacking ||
                          _brain.CurrentBrain == BrainState.RequestingAttack;
            Gizmos.color = active
                ? new Color(1f, 0.3f, 0.3f, 0.8f)
                : new Color(1f, 1f, 1f, 0.15f);
            Gizmos.DrawLine(transform.position, player.transform.position);
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 0.8f, 0f));
    }

    private GUIStyle GetSceneStyle(EnemyRole role)
    {
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = RoleColour(role);
        style.fontSize = 10;
        return style;
    }
#endif


    private void InitStyle()
    {
        if (_styleReady) return;
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold
        };
        _styleReady = true;
    }

    private void DrawMiniBar(Rect rect, float fraction, Color fillColor, string label)
    {
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
    }


    private static Color StateColour(EnemyStateID state) => state switch
    {
        EnemyStateID.Idle => Color.white,
        EnemyStateID.Wandering => new Color(0.6f, 0.8f, 1f),
        EnemyStateID.Chasing => new Color(1f, 0.8f, 0.2f),
        EnemyStateID.Windup => new Color(1f, 0.5f, 0f),
        EnemyStateID.Attacking => new Color(1f, 0.2f, 0.2f),
        EnemyStateID.Recovery => new Color(0.8f, 0.4f, 0.8f),
        EnemyStateID.Blocking => new Color(0.3f, 0.8f, 1f),
        EnemyStateID.Taunting => new Color(0.9f, 0.9f, 0.2f),
        EnemyStateID.Hurt => new Color(1f, 0.4f, 0.4f),
        EnemyStateID.KnockedDown => new Color(0.9f, 0.3f, 0.3f),
        EnemyStateID.GetUp => new Color(0.7f, 0.7f, 0.3f),
        EnemyStateID.Dead => Color.gray,
        _ => Color.white
    };

    private static Color BrainColour(BrainState state) => state switch
    {
        BrainState.Wandering => new Color(0.6f, 0.9f, 0.6f),
        BrainState.Pursuing => new Color(1f, 0.8f, 0.3f),
        BrainState.WaitingForToken => new Color(0.8f, 0.8f, 0.4f),
        BrainState.RequestingAttack => new Color(1f, 0.5f, 0.1f),
        BrainState.Attacking => new Color(1f, 0.2f, 0.2f),
        BrainState.Retreating => new Color(0.5f, 0.7f, 1f),
        BrainState.Taunting => new Color(0.9f, 0.9f, 0.2f),
        BrainState.Blocking => new Color(0.3f, 0.8f, 1f),
        _ => Color.white
    };

    private static Color RoleColour(EnemyRole role) => role switch
    {
        EnemyRole.Attacker => new Color(1f, 0.3f, 0.3f),
        EnemyRole.Flanker => new Color(1f, 0.6f, 0.1f),
        EnemyRole.Waiter => new Color(0.5f, 0.8f, 0.5f),
        EnemyRole.Retreating => new Color(0.4f, 0.6f, 1f),
        EnemyRole.Taunting => new Color(0.9f, 0.9f, 0.2f),
        EnemyRole.Blocking => new Color(0.3f, 0.9f, 1f),
        _ => Color.white
    };
}
