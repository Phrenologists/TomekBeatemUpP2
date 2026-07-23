using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class HitboxVisualizer : MonoBehaviour
{
    public enum PreviewMode
    {
        SingleAttack,
        AllFromMap,
        AllFromRoster
    }

    [Header("Preview Mode")]
    public PreviewMode mode = PreviewMode.SingleAttack;

    [Header("Data Sources")]
    [Tooltip("Used when mode = SingleAttack.")]
    public AttackData previewAttack;

    [Tooltip("Used when mode = AllFromMap. Shows all non-null attack slots.")]
    public AttackMap attackMap;

    [Tooltip("Used when mode = AllFromRoster. Shows all attacks in the list.")]
    public EnemyAttackRoster enemyRoster;

    [Header("Display Options")]
    [Tooltip("Direction the character faces. Toggle to check both sides.")]
    public bool facingRight = true;

    [Tooltip("Show attack name and frame data as a label in the scene view.")]
    public bool showLabels = true;

    [Tooltip("Show a crosshair at the hitbox offset origin.")]
    public bool showOriginCrosshair = true;

    [Tooltip("Draw a filled rectangle in addition to the wire outline.")]
    public bool showFill = true;

    [Tooltip("Opacity of the filled rectangle (0 = invisible, 1 = solid).")]
    [Range(0f, 1f)]
    public float fillAlpha = 0.25f;

    [Header("Colours  (Single Attack mode)")]
    public Color singleColor = new Color(1f, 0.2f, 0.2f, 1f);

    private static readonly Color[] Palette = new Color[]
    {
        new Color(1f,   0.2f, 0.2f, 1f),   // red
        new Color(0.2f, 0.8f, 1f,   1f),   // cyan
        new Color(0.2f, 1f,   0.3f, 1f),   // green
        new Color(1f,   0.8f, 0.1f, 1f),   // yellow
        new Color(0.8f, 0.2f, 1f,   1f),   // purple
        new Color(1f,   0.5f, 0.1f, 1f),   // orange
    };


    private void OnDrawGizmos()
    {
        switch (mode)
        {
            case PreviewMode.SingleAttack:
                DrawSingle();
                break;
            case PreviewMode.AllFromMap:
                DrawAllFromMap();
                break;
            case PreviewMode.AllFromRoster:
                DrawAllFromRoster();
                break;
        }
    }

    private void DrawSingle()
    {
        if (previewAttack == null) return;
        DrawAttackHitbox(previewAttack, singleColor, 0);
    }

    private void DrawAllFromMap()
    {
        if (attackMap == null) return;

        var slots = new (string label, AttackData data)[]
        {
            ("LightGround",  attackMap.lightAttackGround),
            ("HeavyGround",  attackMap.heavyAttackGround),
            ("LTrigGround",  attackMap.leftTriggerGround),
            ("RTrigGround",  attackMap.rightTriggerGround),
            ("LightAir",     attackMap.lightAttackAir),
            ("HeavyAir",     attackMap.heavyAttackAir),
            ("LTrigAir",     attackMap.leftTriggerAir),
            ("RTrigAir",     attackMap.rightTriggerAir),
        };

        int colorIndex = 0;
        foreach (var (label, data) in slots)
        {
            if (data == null) continue;
            DrawAttackHitbox(data, Palette[colorIndex % Palette.Length], colorIndex, label);
            colorIndex++;
        }
    }

    private void DrawAllFromRoster()
    {
        if (enemyRoster == null) return;

        for (int i = 0; i < enemyRoster.attacks.Count; i++)
        {
            var data = enemyRoster.attacks[i];
            if (data == null) continue;
            DrawEnemyAttackHitbox(data, Palette[i % Palette.Length], i);
        }
    }


    private void DrawAttackHitbox(AttackData data, Color color, int index, string overrideLabel = null)
    {
        float sign = facingRight ? 1f : -1f;
        Vector3 center = new Vector3(
            transform.position.x + data.hitboxOffset.x * sign,
            transform.position.y + data.hitboxOffset.y,
            0f);

        Vector3 size = new Vector3(data.hitboxSize.x, data.hitboxSize.y, 0.01f);

        DrawBox(center, size, color);

        if (showOriginCrosshair)
            DrawCrosshair(transform.position, color, 0.15f);

        if (showLabels)
        {
            string label = overrideLabel ?? data.attackName;
            string frameInfo = $"{label}\n" +
                               $"S:{data.startupFrames} A:{data.activeFrames} R:{data.recoveryFrames}\n" +
                               $"DMG:{data.damage}  KB:({data.knockback.x:F1},{data.knockback.y:F1})";
#if UNITY_EDITOR
            Vector3 labelPos = center + Vector3.up * (data.hitboxSize.y * 0.5f + 0.15f)
                                      + Vector3.right * (index * 0.05f);
            var style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 9;
            style.fontStyle = FontStyle.Bold;
            Handles.Label(labelPos, frameInfo, style);
#endif
        }
    }

    private void DrawEnemyAttackHitbox(EnemyAttackData data, Color color, int index)
    {
        float sign = facingRight ? 1f : -1f;
        Vector3 center = new Vector3(
            transform.position.x + data.hitboxOffset.x * sign,
            transform.position.y + data.hitboxOffset.y,
            0f);

        Vector3 size = new Vector3(data.hitboxSize.x, data.hitboxSize.y, 0.01f);

        DrawBox(center, size, color);

        if (showOriginCrosshair)
            DrawCrosshair(transform.position, color, 0.15f);

        if (showLabels)
        {
            string frameInfo = $"{data.attackName}\n" +
                               $"S:{data.startupFrames} A:{data.activeFrames} R:{data.recoveryFrames}\n" +
                               $"DMG:{data.damage}  Cost:{data.tokenCost}  Gate:{data.intensityGate:F1}";
#if UNITY_EDITOR
            Vector3 labelPos = center + Vector3.up * (data.hitboxSize.y * 0.5f + 0.15f);
            var style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 9;
            style.fontStyle = FontStyle.Bold;
            Handles.Label(labelPos, frameInfo, style);
#endif
        }
    }


    private void DrawBox(Vector3 center, Vector3 size, Color color)
    {
        if (showFill)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, fillAlpha);
            Gizmos.DrawCube(center, size);
        }

        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawCrosshair(Vector3 pos, Color color, float size)
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 0.6f);
        Gizmos.DrawLine(pos + Vector3.left * size, pos + Vector3.right * size);
        Gizmos.DrawLine(pos + Vector3.down * size, pos + Vector3.up * size);
    }
}
