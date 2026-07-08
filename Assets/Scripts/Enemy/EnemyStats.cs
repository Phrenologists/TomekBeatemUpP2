

using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 60;

    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float depthSpeed = 2.0f;
    public float acceleration = 15f;
    public float deceleration = 20f;

    [Header("Attack")]
    [Tooltip("Distance on X at which the enemy starts attacking.")]
    public float attackRangeX = 1.1f;
    [Tooltip("Distance on the depth axis that is also checked.")]
    public float attackRangeDepth = 0.6f;
    [Tooltip("Seconds of telegraph animation before the hitbox activates.")]
    public float windupDuration = 0.35f;
    [Tooltip("Seconds the active hit window stays open.")]
    public float attackDuration = 0.2f;
    [Tooltip("Seconds of recovery after an attack.")]
    public float recoveryDuration = 0.5f;
    [Tooltip("Seconds between two attack attempts.")]
    public float attackCooldown = 1.2f;

    public int attackDamage = 10;
    public Vector2 attackKnockback = new Vector2(4f, 2f);
    public bool attackKnocksDown = false;

    [Header("Hurt")]
    public float hurtDuration = 0.35f;
    public float knockdownDuration = 1.2f;
    public float getUpDuration = 0.6f;

    [Header("Score / Drops")]
    public int scoreValue = 100;

    [Header("Juggle")]
    [Tooltip("Minimum upward velocity applied when an airborne enemy is hit with a " +
         "horizontal attack. Keeps them in the air for combo continuation. " +
         "Higher = floatier juggle, lower = they drop faster between hits.")]
    public float juggleUplift = 3f;
}
