using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/StrikerData")]
public class StrikerData : ScriptableObject
{
    [Tooltip("Display name shown in the striker selection UI.")]
    public string strikerName = "Striker";

    [Tooltip("Portrait sprite shown in the selection UI.")]
    public Sprite portrait;

    [Header("Prefab")]
    [Tooltip("The striker character prefab. Must have a StrikerController component.")]
    public GameObject prefab;

    [Header("Positioning (world space)")]
    [Tooltip("Where the striker stands while performing their attack.")]
    public Vector2 attackSpawnPosition = new Vector2(5f, 0f);

    [Tooltip("Offset from attackSpawnPosition where the striker starts before tweening in. " +
             "E.g. (3, 0) = slides in from the right.")]
    public Vector2 entryStartOffset = new Vector2(3f, 0f);

    [Tooltip("Offset from attackSpawnPosition where the striker moves to when exiting. " +
             "Usually the same direction as entry.")]
    public Vector2 exitEndOffset = new Vector2(3f, 0f);

    [Header("Timing (seconds)")]
    [Tooltip("Duration of the entry tween.")]
    public float entryDuration = 0.15f;

    [Tooltip("Delay between arriving at spawn position and starting the attack.")]
    public float preAttackDelay = 0.05f;

    [Tooltip("Duration of the exit tween.")]
    public float exitDuration = 0.2f;

    [Tooltip("Seconds before this striker can be called again after use.")]
    public float cooldown = 8f;

    [Header("Attack")]
    [Tooltip("The attack the striker performs. Uses the same AttackData system as the player.")]
    public AttackData attackData;

    [Tooltip("Should the striker face left (toward the player/enemies) on entry?")]
    public bool faceLeft = true;

    [Header("Tween Easing")]
    public DG.Tweening.Ease entryEase = DG.Tweening.Ease.OutQuad;
    public DG.Tweening.Ease exitEase = DG.Tweening.Ease.InQuad;

    [Header("Flash VFX (optional)")]
    [Tooltip("If true, the sprite flashes in on entry instead of sliding.")]
    public bool flashOnEntry = false;

    [Tooltip("Number of alpha flashes on entry if flashOnEntry is true.")]
    public int flashCount = 3;
}
