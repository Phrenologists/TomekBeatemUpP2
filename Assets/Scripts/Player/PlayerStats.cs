

using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("Movement")]
    public float walkSpeed = 5.0f;
    public float depthSpeed = 3.5f;
    public float acceleration = 20f;
    public float deceleration = 25f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float jumpHoldGravity = 1.5f;
    public float fallGravity = 3.5f;
    public float maxFallSpeed = 20f;

    [Header("Dash")]
    public float dashSpeed = 14f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.5f;
    public bool dashInvincible = true;

    [Header("Attack")]
    public int comboLength = 3;
    public float attackDuration = 0.35f;
    public float comboLinkWindow = 0.2f;
    public float attackMoveSpeed = 1.5f;

    [Header("Health")]
    public int maxHealth = 100;
    public float hurtDuration = 0.4f;
    public float knockdownDuration = 1.2f;
    public float getUpDuration = 0.5f;
}
