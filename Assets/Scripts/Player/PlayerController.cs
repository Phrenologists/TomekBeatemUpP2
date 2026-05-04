// COMPONENT REQUIREMENTS:
//   - PlayerInputHandler
//   - PlayerStats  (ScriptableObject)
//   - Rigidbody2D  (Gravity Scale = 0)
//   - BoxCollider2D
//   - Animator
//   - SpriteRenderer
//
// ANIMATOR PARAMETERS:
//   stateID(Int) — matches (int)PlayerStateID exactly
//   attackIndex(Int) — 0/1/2 for combo hits, -1 when not attacking, might change it later when we do smth else with the attacks

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("2.5D Lane Bounds (world-space Y)")]
    [Tooltip("Top of the walkable lane (smallest Y value).")]
    [SerializeField] private float groundYMin = -3f;
    [Tooltip("Bottom of the walkable lane (largest Y value).")]
    [SerializeField] private float groundYMax = 3f;


    private static readonly int AnimStateID = Animator.StringToHash("stateID");
    private static readonly int AnimAttackIdx = Animator.StringToHash("attackIndex");

    
    public PlayerStateID CurrentState { get; private set; } = PlayerStateID.Idle;
    public bool IsGrounded => _jumpHeight <= 0f;
    public int CurrentHealth => _currentHealth;

    public float GroundY => _groundY;

    
    private PlayerInputHandler _input;
    private Rigidbody2D _rb;

    
    private float _groundY;       // depth position on the floor plane
    private float _jumpHeight;    // height above the floor  (0 = grounded)
    private float _jumpVelocity;  // vertical speed acting on _jumpHeight

    // Horizontal / depth movement velocities (world units per second)
    private float _velX;
    private float _velDepth;


    private bool _facingRight = true;


    private float _coyoteTimer;
    private bool _wasGrounded;
    private const float CoyoteTime = 0.1f;

    
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashVelX;
    private float _dashVelDepth;

    
    private int _comboIndex;
    private float _attackTimer;
    private bool _comboInputQueued;

    
    private float _hurtTimer;
    private int _currentHealth;

    
    private bool _isInvincible;

    

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        _currentHealth = stats.maxHealth;
        _groundY = transform.position.y;
    }

    private void Update()
    {
        if (_wasGrounded && !IsGrounded)
            _coyoteTimer = CoyoteTime;
        else if (IsGrounded)
            _coyoteTimer = 0f;
        else
            _coyoteTimer -= Time.deltaTime;

        _wasGrounded = IsGrounded;

        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;

        TickState();

        if (_jumpHeight < 0f)
        {
            _jumpHeight = 0f;
            _jumpVelocity = 0f;
        }

        _groundY = Mathf.Clamp(_groundY, groundYMin, groundYMax);

        Vector3 pos = transform.position;
        pos.x += _velX * Time.deltaTime;
        pos.y = _groundY + _jumpHeight;
        transform.position = pos;

        _rb.position = new Vector2(pos.x, pos.y);

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-_groundY * 10f);

        
        UpdateAnimator();

        
        _input.ConsumeFrameInputs();
    }

    

    private void TickState()
    {
        switch (CurrentState)
        {
            case PlayerStateID.Idle: TickIdle(); break;
            case PlayerStateID.Walking: TickWalking(); break;
            case PlayerStateID.Jumping: TickJumping(); break;
            case PlayerStateID.Falling: TickFalling(); break;
            case PlayerStateID.Dashing: TickDashing(); break;
            case PlayerStateID.Attacking: TickAttacking(); break;
            case PlayerStateID.AirAttacking: TickAirAttacking(); break;
            case PlayerStateID.Hurt: TickHurt(); break;
            case PlayerStateID.KnockedDown: TickKnockedDown(); break;
            case PlayerStateID.GetUp: TickGetUp(); break;
            case PlayerStateID.Dead: break;
        }
    }

    
    private void TickIdle()
    {
        ApplyGroundFriction();

        if (TryDash()) return;
        if (TryAttack()) return;
        if (TryJump()) return;

        if (_input.MoveInput.sqrMagnitude > 0.01f)
            TransitionTo(PlayerStateID.Walking);
    }

    private void TickWalking()
    {
        ApplyGroundMovement();

        if (TryDash()) return;
        if (TryAttack()) return;
        if (TryJump()) return;

        if (_input.MoveInput.sqrMagnitude < 0.01f)
            TransitionTo(PlayerStateID.Idle);
    }

    private void TickJumping()
    {
        ApplyGroundMovement();
        TickJumpPhysics();

        if (TryAirAttack()) return;

        if (!_input.JumpHeld && _jumpVelocity > 0f)
            _jumpVelocity = Mathf.MoveTowards(_jumpVelocity, 0f,
                stats.fallGravity * 8f * Time.deltaTime);

        if (_jumpVelocity <= 0f)
            TransitionTo(PlayerStateID.Falling);
    }

    
    private void TickFalling()
    {
        ApplyGroundMovement();
        TickFallPhysics();

        if (TryAirAttack()) return;

        if (IsGrounded)
        {
            TransitionTo(_input.MoveInput.sqrMagnitude > 0.01f
                ? PlayerStateID.Walking
                : PlayerStateID.Idle);
        }
    }

    
    private void TickDashing()
    {
        _dashTimer -= Time.deltaTime;

        // Dash overrides movement with its own fixed velocity
        _velX = _dashVelX;
        _velDepth = _dashVelDepth;
        _groundY += _velDepth * Time.deltaTime;

        // Keep jump physics running if we dashed in the air
        if (!IsGrounded) TickFallPhysics();

        if (_dashTimer <= 0f)
        {
            _velX = 0f;
            _velDepth = 0f;
            TransitionTo(IsGrounded ? PlayerStateID.Idle : PlayerStateID.Falling);
        }
    }

    private void TickAttacking()
    {
        //For now, we lunge the character slightly when attacking, but it kinda feels weird tbh, I'll see if we do smth about it
        float lunge = (_facingRight ? 1f : -1f) * stats.attackMoveSpeed;
        _velX = Mathf.Lerp(_velX, lunge, 10f * Time.deltaTime);

        if (_input.AttackPressed)
            _comboInputQueued = true;

        _attackTimer -= Time.deltaTime;

        //Ok, I just realized this logic is pretty stupid - it's gonna be replaced with a system where startup phase is first
        //then it's active phase, then there's recovery - you will only be able to extend in recovery, maybe the input could be
        //qued before, idk, I did some research and it seems this sort of queing is not done as industry standard. Idk, this will have to be talked about with the team
        if (_attackTimer <= stats.comboLinkWindow && _comboInputQueued)
        {
            _comboIndex = (_comboIndex + 1) % stats.comboLength;
            StartAttack();
            return;
        }

        if (_attackTimer <= 0f)
        {
            _comboIndex = 0;
            _comboInputQueued = false;
            TransitionTo(PlayerStateID.Idle);
        }
    }

    private void TickAirAttacking()
    {
        TickFallPhysics();
        _attackTimer -= Time.deltaTime;

        if (_attackTimer <= 0f || IsGrounded)
            TransitionTo(IsGrounded ? PlayerStateID.Idle : PlayerStateID.Falling);
    }

    private void TickHurt()
    {
        _hurtTimer -= Time.deltaTime;
        if (!IsGrounded) TickFallPhysics();
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);

        if (_hurtTimer <= 0f && IsGrounded)
        {
            _velX = 0f;
            TransitionTo(PlayerStateID.Idle);
        }
    }

    private void TickKnockedDown()
    {
        _hurtTimer -= Time.deltaTime;
        if (_hurtTimer <= 0f)
        {
            _hurtTimer = stats.getUpDuration;
            TransitionTo(PlayerStateID.GetUp);
        }
    }

    private void TickGetUp()
    {
        _hurtTimer -= Time.deltaTime;
        if (_hurtTimer <= 0f)
            TransitionTo(PlayerStateID.Idle);
    }

    private void ApplyGroundMovement()
    {
        Vector2 input = _input.MoveInput;

        // Horizontal velocity
        float targetVX = input.x * stats.walkSpeed;
        _velX = Mathf.MoveTowards(_velX, targetVX,
            (Mathf.Abs(input.x) > 0.01f ? stats.acceleration : stats.deceleration) * Time.deltaTime);

        // Depth velocity
        float targetDepth = input.y * stats.depthSpeed;
        _velDepth = Mathf.MoveTowards(_velDepth, targetDepth,
            (Mathf.Abs(input.y) > 0.01f ? stats.acceleration : stats.deceleration) * Time.deltaTime);
        _groundY += _velDepth * Time.deltaTime;

        // Sprite facing
        if (input.x > 0.01f && !_facingRight) Flip();
        if (input.x < -0.01f && _facingRight) Flip();
    }

    private void ApplyGroundFriction()
    {
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);
        _velDepth = Mathf.MoveTowards(_velDepth, 0f, stats.deceleration * Time.deltaTime);
        _groundY += _velDepth * Time.deltaTime;
    }
     
    //Lighter gravity when ascending, cool trick, I should do this in other projects, lol
    private void TickJumpPhysics()
    {
        float g = stats.jumpHoldGravity * Mathf.Abs(Physics2D.gravity.y);
        _jumpVelocity -= g * Time.deltaTime;
        _jumpHeight += _jumpVelocity * Time.deltaTime;
    }

    //Heavier gravity when falling, might be a bit of an overkill for a game like this
    private void TickFallPhysics()
    {
        float g = stats.fallGravity * Mathf.Abs(Physics2D.gravity.y);
        _jumpVelocity -= g * Time.deltaTime;
        _jumpVelocity = Mathf.Max(_jumpVelocity, -stats.maxFallSpeed);
        _jumpHeight += _jumpVelocity * Time.deltaTime;
    }

    private bool TryJump()
    {
        bool canJump = IsGrounded || _coyoteTimer > 0f;
        if (!_input.JumpPressed || !canJump) return false;

        _jumpVelocity = stats.jumpForce;
        _jumpHeight = 0.01f;//nudge off floor so IsGrounded flips immediately, this might cause some bugs but right now, it kinda works
        _coyoteTimer = 0f;
        TransitionTo(PlayerStateID.Jumping);
        return true;
    }

    private bool TryDash()
    {
        if (!_input.DashPressed || _dashCooldownTimer > 0f) return false;

        Vector2 dir = _input.MoveInput.sqrMagnitude > 0.01f
            ? _input.MoveInput.normalized
            : new Vector2(_facingRight ? 1f : -1f, 0f);

        if (!IsGrounded) dir.y = 0f;// horizontal-only air dash
        if (dir == Vector2.zero) dir = new Vector2(_facingRight ? 1f : -1f, 0f);

        _dashVelX = dir.x * stats.dashSpeed;
        _dashVelDepth = dir.y * stats.dashSpeed;
        _dashTimer = stats.dashDuration;
        _dashCooldownTimer = stats.dashCooldown;

        if (dir.x > 0f && !_facingRight) Flip();
        if (dir.x < 0f && _facingRight) Flip();

        TransitionTo(PlayerStateID.Dashing);
        return true;
    }

    private bool TryAttack()
    {
        if (!_input.AttackPressed) return false;
        _comboIndex = 0;
        _comboInputQueued = false;
        StartAttack();
        return true;
    }

    private bool TryAirAttack()
    {
        if (!_input.AttackPressed) return false;
        _attackTimer = stats.attackDuration;
        TransitionTo(PlayerStateID.AirAttacking);
        return true;
    }

    private void StartAttack()
    {
        _attackTimer = stats.attackDuration;
        _comboInputQueued = false;
        TransitionTo(PlayerStateID.Attacking);
    }

    private void TransitionTo(PlayerStateID next)
    {
        if (next == CurrentState) return;
        OnExitState(CurrentState);
        CurrentState = next;
        OnEnterState(next);
    }

    private void OnEnterState(PlayerStateID state)
    {
        if (state == PlayerStateID.Dashing && stats.dashInvincible)
        {
            SetInvincible(true);
            StartCoroutine(EndDashInvincibility());
        }
    }

    private void OnExitState(PlayerStateID state) { }

    private IEnumerator EndDashInvincibility()
    {
        yield return new WaitForSeconds(stats.dashDuration);
        SetInvincible(false);
    }

    public void TakeDamage(int amount, Vector2 knockback, bool knockdown = false)
    {
        
        if (CurrentState == PlayerStateID.Dead) return;
        if (_isInvincible) return;

        _currentHealth -= amount;

        Debug.Log(CurrentHealth);

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            TransitionTo(PlayerStateID.Dead);
            return;
        }

        _velX = knockback.x;
        _jumpVelocity = knockback.y;
        if (knockback.y > 0f) _jumpHeight = 0.01f;  // lift off floor

        if (knockdown)
        {
            _hurtTimer = stats.knockdownDuration;
            TransitionTo(PlayerStateID.KnockedDown);
        }
        else
        {
            Debug.Log("Got hit");
            _hurtTimer = stats.hurtDuration;
            TransitionTo(PlayerStateID.Hurt);
        }
    }

    private void SetInvincible(bool value) => _isInvincible = value;

    private void UpdateAnimator()
    {
        if (animator == null) return;

        //All state transitions work with one int that corresponds to current enum of the state
        animator.SetInteger(AnimStateID, (int)CurrentState);

        //Debug.Log((int)CurrentState);

        //-1 means "not attacking"
        animator.SetInteger(AnimAttackIdx,
            CurrentState is PlayerStateID.Attacking or PlayerStateID.AirAttacking
                ? _comboIndex : -1);
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    //All debug drawing goes here

    private void OnDrawGizmosSelected()
    {
        float x = transform.position.x;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(x - 6f, groundYMin), new Vector3(x + 6f, groundYMin));
        Gizmos.DrawLine(new Vector3(x - 6f, groundYMax), new Vector3(x + 6f, groundYMax));

        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(x, _groundY), 0.12f);

        if (!IsGrounded)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                new Vector3(x, _groundY),
                new Vector3(x, _groundY + _jumpHeight));
        }
    }
}