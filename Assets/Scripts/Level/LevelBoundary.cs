using System.Collections.Generic;
using UnityEngine;

public class LevelBoundary : MonoBehaviour
{
    public static readonly List<LevelBoundary> All = new List<LevelBoundary>();

    [Tooltip("Which axis and side this wall blocks movement on.\n\n" +
             "Left  — blocks the player from moving further left  (pos.x floor).\n" +
             "Right — blocks the player from moving further right (pos.x ceiling).\n" +
             "DepthTop    — blocks upward depth movement  (_groundY floor).\n" +
             "DepthBottom — blocks downward depth movement (_groundY ceiling).")]
    public BoundaryDirection direction = BoundaryDirection.Left;

    [Tooltip("Visual height of the gizmo wall line in Scene view. " +
             "Has no gameplay effect — purely for readability.")]
    [SerializeField] private float gizmoHeight = 6f;

    public float WorldPosition => direction switch
    {
        BoundaryDirection.Left => transform.position.x,
        BoundaryDirection.Right => transform.position.x,
        BoundaryDirection.DepthTop => transform.position.y,
        BoundaryDirection.DepthBottom => transform.position.y,
        _ => transform.position.x
    };

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    /// <param name="posX">Proposed world X position (modified in place).</param>
    /// <param name="groundY">Proposed ground Y/depth position (modified in place).</param>
    /// <param name="velX">Horizontal velocity — zeroed when hitting a wall.</param>
    /// <param name="halfWidth">Half the character's collision width (X boundaries).</param>
    /// <param name="halfDepth">Half the character's collision depth (depth boundaries).</param>
    public static void ApplyAll(ref float posX, ref float groundY,
                                ref float velX,
                                float halfWidth = 0.3f, float halfDepth = 0f)
    {
        foreach (var b in All)
        {
            if (b == null) continue;

            switch (b.direction)
            {
                case BoundaryDirection.Left:
                    {
                        float limit = b.WorldPosition + halfWidth;
                        if (posX < limit)
                        {
                            posX = limit;
                            if (velX < 0f) velX = 0f;
                        }
                        break;
                    }
                case BoundaryDirection.Right:
                    {
                        float limit = b.WorldPosition - halfWidth;
                        if (posX > limit)
                        {
                            posX = limit;
                            if (velX > 0f) velX = 0f;
                        }
                        break;
                    }
                case BoundaryDirection.DepthTop:
                    {
                        float limit = b.WorldPosition + halfDepth;
                        if (groundY < limit) groundY = limit;
                        break;
                    }
                case BoundaryDirection.DepthBottom:
                    {
                        float limit = b.WorldPosition - halfDepth;
                        if (groundY > limit) groundY = limit;
                        break;
                    }
            }
        }
    }

    public static void BlockVelocity(
    float currentX,
    float currentGroundY,
    ref float velX,
    ref float velDepth,
    float halfWidth = 0.3f,
    float halfDepth = 0f)
    {
        foreach (var b in All)
        {
            if (b == null) continue;

            switch (b.direction)
            {
                case BoundaryDirection.Left:
                    if (velX < 0f &&
                        currentX <= b.WorldPosition + halfWidth + 0.001f)
                        velX = 0f;
                    break;

                case BoundaryDirection.Right:
                    if (velX > 0f &&
                        currentX >= b.WorldPosition - halfWidth - 0.001f)
                        velX = 0f;
                    break;

                case BoundaryDirection.DepthTop:
                    if (velDepth < 0f &&
                        currentGroundY <= b.WorldPosition + halfDepth + 0.001f)
                        velDepth = 0f;
                    break;

                case BoundaryDirection.DepthBottom:
                    if (velDepth > 0f &&
                        currentGroundY >= b.WorldPosition - halfDepth - 0.001f)
                        velDepth = 0f;
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Color col = direction switch
        {
            BoundaryDirection.Left => new Color(0.3f, 0.5f, 1f, 1f),
            BoundaryDirection.Right => new Color(1f, 0.3f, 0.3f, 1f),
            BoundaryDirection.DepthTop => new Color(0.3f, 1f, 0.4f, 1f),
            BoundaryDirection.DepthBottom => new Color(0.3f, 1f, 0.4f, 1f),
            _ => Color.white
        };

        Gizmos.color = col;

        bool isXBoundary = direction == BoundaryDirection.Left ||
                           direction == BoundaryDirection.Right;

        if (isXBoundary)
        {
            Vector3 top = new Vector3(transform.position.x, gizmoHeight * 0.5f, 0f);
            Vector3 bot = new Vector3(transform.position.x, -gizmoHeight * 0.5f, 0f);
            Gizmos.DrawLine(top, bot);
            float arrowDir = direction == BoundaryDirection.Left ? 1f : -1f;
            Vector3 mid = transform.position;
            Vector3 tip = mid + Vector3.right * arrowDir * 0.5f;
            Gizmos.DrawLine(mid, tip);
            Gizmos.DrawLine(tip, tip + new Vector3(-arrowDir * 0.2f, 0.15f, 0f));
            Gizmos.DrawLine(tip, tip + new Vector3(-arrowDir * 0.2f, -0.15f, 0f));
            Gizmos.color = new Color(col.r, col.g, col.b, 0.08f);
            float fillWidth = 0.3f;
            float fillX = direction == BoundaryDirection.Left
                ? transform.position.x - fillWidth * 0.5f
                : transform.position.x + fillWidth * 0.5f;
            Gizmos.DrawCube(
                new Vector3(fillX, 0f, 0f),
                new Vector3(fillWidth, gizmoHeight, 0.01f));
        }
        else
        {
            Vector3 left = new Vector3(-10f, transform.position.y, 0f);
            Vector3 right = new Vector3(10f, transform.position.y, 0f);
            Gizmos.DrawLine(left, right);

            float arrowDir = direction == BoundaryDirection.DepthTop ? 1f : -1f;
            Vector3 mid = transform.position;
            Vector3 tip = mid + Vector3.up * arrowDir * 0.5f;
            Gizmos.DrawLine(mid, tip);
            Gizmos.DrawLine(tip, tip + new Vector3(0.15f, -arrowDir * 0.2f, 0f));
            Gizmos.DrawLine(tip, tip + new Vector3(-0.15f, -arrowDir * 0.2f, 0f));
        }

#if UNITY_EDITOR
        Color labelCol = col;
        labelCol.a = 1f;
        var style = new GUIStyle
        {
            normal = { textColor = labelCol },
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };
        string label = direction switch
        {
            BoundaryDirection.Left => $"◀ Wall  x={transform.position.x:F1}",
            BoundaryDirection.Right => $"Wall ▶  x={transform.position.x:F1}",
            BoundaryDirection.DepthTop => $"▲ Depth  y={transform.position.y:F1}",
            BoundaryDirection.DepthBottom => $"Depth ▼  y={transform.position.y:F1}",
            _ => ""
        };
        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoHeight * 0.5f + 0.2f), label, style);
#endif
    }
}

public enum BoundaryDirection
{
    Left,
    Right,
    DepthTop,
    DepthBottom
}
