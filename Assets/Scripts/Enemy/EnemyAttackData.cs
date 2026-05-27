// Unique fields for enemy attacks:
//   - tokenCost      : how much of the director's budget this attack consumes
//   - breaksBlock    : whether this attack bypasses or staggers a blocking player
//   - minimumRange   : enemy won't use this attack if too close to the player
//   - maximumRange   : enemy won't use this attack if too far from the player
//   - requiresRole   : attack is only available when the enemy holds a specific role
//   - intensityGate  : minimum director intensity (0-1) before this attack is unlocked
//
// ATTACK ROSTER:
//   Each EnemyController holds an EnemyAttackRoster asset (list of these)
//   rather than a single attack. EnemyAIBrain picks from the roster based on
//   current range, token budget, intensity, and role.
//

using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/EnemyAttackData")]
public class EnemyAttackData : ScriptableObject
{
    [Tooltip("Human-readable name for debugging and designer reference.")]
    public string attackName = "Enemy Attack";

    [Header("Frame Data  (frames @ targetFPS)")]
    [Min(1)] public int startupFrames = 8;
    [Min(1)] public int activeFrames = 4;
    [Min(1)] public int recoveryFrames = 16;
    public float targetFPS = 60f;

    public float StartupDuration => startupFrames / targetFPS;
    public float ActiveDuration => activeFrames / targetFPS;
    public float RecoveryDuration => recoveryFrames / targetFPS;
    public float TotalDuration => StartupDuration + ActiveDuration + RecoveryDuration;

    [Header("Hitbox")]
    [Tooltip("Centre relative to enemy root. X flipped automatically when facing left.")]
    public Vector2 hitboxOffset = new Vector2(0.7f, 0f);
    public Vector2 hitboxSize = new Vector2(0.9f, 0.7f);

    [Header("Hit Properties")]
    public int damage = 12;
    public Vector2 knockback = new Vector2(3f, 2f);
    public bool causesKnockdown = false;

    [Header("Director Budget")]
    [Tooltip("How many points this attack costs from the director's token budget. " +
             "Quick jab = 1, heavy combo = 2, grab or charge = 3+.")]
    [Min(1)] public int tokenCost = 1;

    [Tooltip("Minimum director intensity (0-1) required before this attack is selectable. " +
             "Set to 0 to always allow. Use higher values to gate powerful attacks " +
             "behind encounter progression.")]
    [Range(0f, 1f)] public float intensityGate = 0f;

    [Header("Block Interaction")]
    [Tooltip("If true, this attack bypasses the player's block entirely.")]
    public bool breaksBlock = false;

    [Tooltip("If true, this attack staggers the block (player is briefly stunned " +
             "even if blocking). Overridden by breaksBlock.")]
    public bool staggersBlock = false;

    [Header("Range Constraints")]
    [Tooltip("Minimum X distance to the player required to use this attack. " +
             "Prevents melee attacks firing at point-blank when a lunge is baked in.")]
    public float minimumRangeX = 0f;

    [Tooltip("Maximum X distance at which this attack can be used.")]
    public float maximumRangeX = 2f;

    [Tooltip("Maximum depth (Y axis) distance at which this attack can be used.")]
    public float maximumRangeDepth = 0.8f;

    [Header("Role Gate")]
    [Tooltip("If set, this attack is only available when the enemy holds the specified role. " +
             "Leave as None to allow from any role.")]
    public EnemyRole requiredRole = EnemyRole.None;

    [Header("Animation")]
    [Tooltip("Animator trigger name for this attack.")]
    public string animTriggerName = "";

    [Header("Per-Phase Movement  (units/sec, X = forward, Y = depth)")]
    public Vector2 startupMovement = Vector2.zero;
    public Vector2 activeMovement = new Vector2(2f, 0f);
    public Vector2 recoveryMovement = Vector2.zero;

    public Vector2 GetMovementForPhase(EnemyAttackPhase phase) => phase switch
    {
        EnemyAttackPhase.Startup => startupMovement,
        EnemyAttackPhase.Active => activeMovement,
        EnemyAttackPhase.Recovery => recoveryMovement,
        _ => Vector2.zero
    };


    public bool IsUsable(
        float distanceX,
        float distanceDepth,
        int availableBudget,
        float currentIntensity,
        EnemyRole currentRole)
    {
        if (tokenCost > availableBudget) return false;
        if (currentIntensity < intensityGate) return false;
        if (distanceX < minimumRangeX) return false;
        if (distanceX > maximumRangeX) return false;
        if (distanceDepth > maximumRangeDepth) return false;
        if (requiredRole != EnemyRole.None && requiredRole != currentRole) return false;

        return true;
    }
}


public enum EnemyAttackPhase
{
    Startup,
    Active,
    Recovery
}

public enum EnemyRole
{
    None = 0,   // no role assigned / any role allowed
    Attacker = 1,   // has an attack token, actively pressuring the player
    Flanker = 2,   // positioning to attack from a different angle
    Waiter = 3,   // holding position, no token
    Retreating = 4,  // post-attack, briefly moving away
    Taunting = 5,   // playing taunt animation
    Blocking = 6,   // in block stance
}
