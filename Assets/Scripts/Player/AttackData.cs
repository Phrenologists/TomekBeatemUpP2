//HOW THIS WORKS:
//Frame data means how long each phase lasts in frames. The actual speed is dependant on how many frames will the game run on,
//so you need to set that amount of frames as the targetFPS in this script to get an accurate preview

using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/AttackData")]
public class AttackData : ScriptableObject
{
    [Tooltip("Human-readable name shown in debug overlays.")]
    public string attackName = "New Attack";

    [Header("Frame Data  (frames at targetFPS)")]
    [Tooltip("Frames before the hitbox activates. Character commits to the move here.")]
    [Min(1)] public int startupFrames = 5;

    [Tooltip("Frames the hitbox is active. More = easier to land.")]
    [Min(1)] public int activeFrames = 3;

    [Tooltip("Frames after the hitbox closes. Combo input window is open during this phase.")]
    [Min(1)] public int recoveryFrames = 12;

    [Tooltip("Framerate used to convert frame counts to seconds. Match your game's target FPS.")]
    public float targetFPS = 60f;

    public float StartupDuration => startupFrames / targetFPS;
    public float ActiveDuration => activeFrames / targetFPS;
    public float RecoveryDuration => recoveryFrames / targetFPS;
    public float TotalDuration => StartupDuration + ActiveDuration + RecoveryDuration;

    // Hitbox size, when I learn more about making unity tools I might make it so that you don't need to set the offset without the preview
    [Header("Hitbox  (local space, relative to character root)")]
    [Tooltip("Centre of the hitbox relative to the character. X is flipped automatically when facing left.")]
    public Vector2 hitboxOffset = new Vector2(0.6f, 0f);

    [Tooltip("Full width and height of the hitbox.")]
    public Vector2 hitboxSize = new Vector2(0.8f, 0.6f);

    // Attack stats
    [Header("Hit Properties")]
    public int damage = 15;

    [Tooltip("Knockback applied in world space. X is flipped automatically when facing left.")]
    public Vector2 knockback = new Vector2(3f, 1.5f);

    public bool causesKnockdown = false;

    [Tooltip("How long the hit opponent is frozen on the hit-stop frame (seconds). 0 = disabled.")]
    [Range(0f, 0.15f)]
    public float hitStopDuration = 0.05f;

    [Header("Animation")]
    [Tooltip("Animator trigger name to play this attack. Must exist as a Trigger parameter in the Animator.")]
    public string animTriggerName = "";

    [Tooltip("Movement applied during Active phase (forward lunge). Positive = forward.")]
    public float activeLungeSpeed = 1.5f;

    [Header("Combo Routing")]
    [Tooltip("Fixed follow-up: always chains to this move regardless of input. Takes priority over branches.")]
    public AttackData fixedFollowUp = null;

    [Tooltip("Branching follow-ups: chains to the first entry whose inputAction matches what was pressed.")]
    public BranchEntry[] branchFollowUps = new BranchEntry[0];

    [Header("Energy")]
    [Tooltip("Energy required to use this attack. 0 = free. Deducted when the attack starts.")]
    [Min(0)] public int energyCost = 0;

    [Header("Per-Phase Movement  (units/sec, X = forward/back, Y = depth)")]

    public Vector2 startupMovement = Vector2.zero;

    public Vector2 activeMovement = new Vector2(1.5f, 0f);

    public Vector2 recoveryMovement = Vector2.zero;

    public Vector2 GetMovementForPhase(AttackPhase phase)
    {
        return phase switch
        {
            AttackPhase.Startup => startupMovement,
            AttackPhase.Active => activeMovement,
            AttackPhase.Recovery => recoveryMovement,
            _ => Vector2.zero
        };
    }

    public AttackData GetFollowUp(string inputActionName)
    {
        if (fixedFollowUp != null)
            return fixedFollowUp;

        foreach (var branch in branchFollowUps)
        {
            if (branch.inputAction == inputActionName && branch.followUp != null)
                return branch.followUp;
        }

        return null;
    }
}

[System.Serializable]
public class BranchEntry
{
    [Tooltip("Input action name that triggers this branch (e.g. 'LightAttack').")]
    public string inputAction;
    [Tooltip("The attack to chain into.")]
    public AttackData followUp;
}
