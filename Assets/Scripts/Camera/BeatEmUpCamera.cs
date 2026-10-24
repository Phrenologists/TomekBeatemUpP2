// BeatEmUpCamera.cs
// Beat-em-up style camera controller with two modes:
//
//   Following — camera scrolls rightward when the player approaches the
//               right edge of the screen. Never scrolls left (one-way).
//               Player is bounded by the camera's left and right edges.
//
//   Locked    — camera holds its position during an enemy encounter.
//               Player is still bounded by the locked edges.
//
// SINGLETON:
//   Accessible via BeatEmUpCamera.Instance so PlayerController and
//   EncounterTrigger can read bounds and change mode without a direct
//   inspector reference.
//
// SETUP:
//   Attach to the Camera GameObject.
//   Set cameraY to match your scene's desired camera height.
//   Requires an orthographic camera (standard for 2D/2.5D games).

using UnityEngine;

public class BeatEmUpCamera : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static BeatEmUpCamera Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Scroll Settings")]
    [Tooltip("How close to the right camera edge (in world units) the player " +
             "must get before the camera starts scrolling. Larger = camera starts " +
             "moving earlier.")]
    [SerializeField] private float scrollZoneWidth = 2.5f;

    [Tooltip("How smoothly the camera catches up to its target position. " +
             "Higher = snappier. Very high values make it feel instant.")]
    [SerializeField] private float followSmoothSpeed = 8f;

    [Header("World Bounds")]
    [Tooltip("Leftmost camera X position — camera never scrolls left of this " +
             "(typically the start of the level).")]
    [SerializeField] private float levelMinX = 0f;

    [Tooltip("Rightmost camera X position — camera never scrolls right of this " +
             "(set to the level's right edge). 0 = no limit.")]
    [SerializeField] private float levelMaxX = 0f;

    [Tooltip("Fixed Y position of the camera. Camera never moves vertically.")]
    [SerializeField] private float cameraY = 0f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // ── Public accessors ──────────────────────────────────────────────────────
    public CameraMode Mode { get; private set; } = CameraMode.Following;

    /// <summary>Left world-space edge of the current camera view.</summary>
    public float WorldLeftEdge => transform.position.x - HalfWidth;

    /// <summary>Right world-space edge of the current camera view.</summary>
    public float WorldRightEdge => transform.position.x + HalfWidth;

    /// <summary>Half the visible world width (orthographic size * aspect).</summary>
    public float HalfWidth => _cam != null
        ? _cam.orthographicSize * _cam.aspect
        : 8f;

    // ── Private ───────────────────────────────────────────────────────────────
    private Camera _cam;
    private float _targetX;          // desired camera X, only ever ratchets right
    private float _lockedX;          // camera X when mode was locked

    // ════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cam = GetComponent<Camera>();
        _targetX = transform.position.x;
    }

    private void LateUpdate()
    {
        if (Mode == CameraMode.Locked) return;

        ApplyScrollTargetFromPlayer();
        ClampTargetToBounds();
        SmoothMoveToTarget();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Mode switching — called by EncounterTrigger
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lock the camera at its current position for the duration of an encounter.
    /// The player will be bounded by these edges until UnlockCamera is called.
    /// </summary>
    public void LockCamera()
    {
        _lockedX = transform.position.x;
        Mode = CameraMode.Locked;
    }

    /// <summary>
    /// Release the camera back to Following mode after an encounter ends.
    /// </summary>
    public void UnlockCamera()
    {
        // Sync target to current position so there's no sudden jump
        _targetX = transform.position.x;
        Mode = CameraMode.Following;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Scroll logic
    // ════════════════════════════════════════════════════════════════════════

    private void ApplyScrollTargetFromPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        float playerX = playerObj.transform.position.x;
        float rightEdge = transform.position.x + HalfWidth;
        float scrollTrigger = rightEdge - scrollZoneWidth;

        // When the player enters the scroll zone (within scrollZoneWidth of
        // the right edge), advance the target so the player stays inside it.
        if (playerX > scrollTrigger)
        {
            float desiredCameraX = playerX - (HalfWidth - scrollZoneWidth);
            // One-way ratchet — target only ever advances rightward
            _targetX = Mathf.Max(_targetX, desiredCameraX);
        }
    }

    private void ClampTargetToBounds()
    {
        _targetX = Mathf.Max(_targetX, levelMinX);
        if (levelMaxX > 0f)
            _targetX = Mathf.Min(_targetX, levelMaxX);
    }

    private void SmoothMoveToTarget()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, _targetX, followSmoothSpeed * Time.deltaTime);
        pos.y = cameraY;
        transform.position = pos;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gizmos
    // ════════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Camera view bounds
        float hw = HalfWidth;
        float y = cameraY;

        Gizmos.color = Mode == CameraMode.Locked
            ? new Color(1f, 0.3f, 0.3f, 0.8f)
            : new Color(0.3f, 1f, 0.5f, 0.8f);

        // Left and right edges
        Gizmos.DrawLine(new Vector3(transform.position.x - hw, y - 3f),
                        new Vector3(transform.position.x - hw, y + 3f));
        Gizmos.DrawLine(new Vector3(transform.position.x + hw, y - 3f),
                        new Vector3(transform.position.x + hw, y + 3f));

        // Scroll zone indicator (right edge minus scrollZoneWidth)
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.5f);
        float scrollLine = transform.position.x + hw - scrollZoneWidth;
        Gizmos.DrawLine(new Vector3(scrollLine, y - 2f), new Vector3(scrollLine, y + 2f));

        // Level bounds
        if (levelMaxX > 0f)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawLine(new Vector3(levelMaxX, y - 3f), new Vector3(levelMaxX, y + 3f));
        }
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f);
        Gizmos.DrawLine(new Vector3(levelMinX, y - 3f), new Vector3(levelMinX, y + 3f));
    }
}

/// <summary>Operating modes for BeatEmUpCamera.</summary>
public enum CameraMode
{
    Following,   // scrolls with the player rightward
    Locked       // fixed during an enemy encounter
}
