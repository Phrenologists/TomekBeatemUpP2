// COMPONENTs REQUIRED:
//   - EnemyStats        (ScriptableObject)
//   - EnemyAIBrain      (sibling component)
//   - Rigidbody2D       (Gravity Scale = 0)
//   - CapsuleCollider2D or BoxCollider2D
//   - Animator
//   - SpriteRenderer

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAIBrain))]
public class EnemyController : MonoBehaviour, IDamageable
{
    
    [Header("References")]
    //zamienic na private serialized jak juz debug nie bedzie potrzebny
    public EnemyStats stats;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AttackHitbox attackHitbox;

    [Header("2.5D Lane Bounds (world-space Y)")]
    [SerializeField] private float groundYMin = -3f;
    [SerializeField] private float groundYMax = 3f;

    [Header("Block Settings")]
    [Tooltip("Fraction of damage taken while blocking (0 = immune, 0.5 = half damage).")]
    [SerializeField] private float blockDamageMultiplier = 0.25f;

    private static readonly int AnimStateID = Animator.StringToHash("enemyStateID");

    public EnemyStateID CurrentState { get; private set; } = EnemyStateID.Idle;
    public bool IsGrounded => _jumpHeight <= 0f;
    public bool IsAlive => _currentHealth > 0;
    public int CurrentHealth => _currentHealth;
    public float GroundY => _groundY;

    public bool IsAttacking =>
        CurrentState == EnemyStateID.Windup ||
        CurrentState == EnemyStateID.Attacking ||
        CurrentState == EnemyStateID.Recovery;


    public bool IsInReactiveState() =>
        CurrentState == EnemyStateID.Hurt ||
        CurrentState == EnemyStateID.KnockedDown ||
        CurrentState == EnemyStateID.GetUp ||
        CurrentState == EnemyStateID.Dead;

    private Rigidbody2D _rb;
    private EnemyAIBrain _brain;

    private float _groundY;
    private float _jumpHeight;
    private float _jumpVelocity;
    private float _velX;
    private float _velDepth;

    private bool _facingRight = true;

    private Vector2 _moveCommand;

    private float _stateTimer;

    private EnemyAttackData _activeAttack;
    private EnemyAttackPhase _attackPhase;

    private int _currentHealth;

    private Transform _playerTransform;

    private bool hasBeenHit = false ;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _brain = GetComponent<EnemyAIBrain>();

        _currentHealth = stats.maxHealth;
        _groundY = transform.position.y;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;

        if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
    }

    private void Update()
    {
        TickState();

        if (_jumpHeight < 0f) { _jumpHeight = 0f; _jumpVelocity = 0f; }
        _groundY = Mathf.Clamp(_groundY, groundYMin, groundYMax);

        Vector3 pos = transform.position;
        pos.x += _velX * Time.deltaTime;
        pos.y = _groundY + _jumpHeight;
        transform.position = pos;
        _rb.position = new Vector2(pos.x, pos.y);

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-_groundY * 10f);

        UpdateAnimator();
    }

    public void SetMoveCommand(Vector2 direction)
    {
        _moveCommand = direction;
    }

    public void BeginAttack(EnemyAttackData attack)
    {
        if (attack == null) return;
        _activeAttack = attack;
        TransitionTo(EnemyStateID.Windup);
    }

    public void BeginBlock() => TransitionTo(EnemyStateID.Blocking);

    public void EndBlock()
    {
        if (CurrentState == EnemyStateID.Blocking)
            TransitionTo(EnemyStateID.Wandering);
    }

    public void BeginTaunt() => TransitionTo(EnemyStateID.Taunting);

    public void EndTaunt()
    {
        if (CurrentState == EnemyStateID.Taunting)
            TransitionTo(EnemyStateID.Wandering);
    }



    private void TickState()
    {
        switch (CurrentState)
        {
            case EnemyStateID.Idle: TickIdle(); break;
            case EnemyStateID.Wandering: TickMoving(); break;
            case EnemyStateID.Chasing: TickMoving(); break;
            case EnemyStateID.Windup: TickWindup(); break;
            case EnemyStateID.Attacking: TickAttacking(); break;
            case EnemyStateID.Recovery: TickRecovery(); break;
            case EnemyStateID.Blocking: TickBlocking(); break;
            case EnemyStateID.Taunting: TickTaunting(); break;
            case EnemyStateID.Hurt: TickHurt(); break;
            case EnemyStateID.KnockedDown: TickKnockedDown(); break;
            case EnemyStateID.GetUp: TickGetUp(); break;
            case EnemyStateID.Dead: break;
        }
    }

    private void TickIdle()
    {
        ApplyFriction();
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            TransitionTo(EnemyStateID.Wandering);
    }

    private void TickMoving()
    {
        float targetVX = _moveCommand.x * stats.walkSpeed;
        float targetVDepth = _moveCommand.y * stats.depthSpeed;

        _velX = Mathf.MoveTowards(_velX, targetVX, stats.acceleration * Time.deltaTime);
        _velDepth = Mathf.MoveTowards(_velDepth, targetVDepth, stats.acceleration * Time.deltaTime);
        _groundY += _velDepth * Time.deltaTime;

        EnemyStateID desired = _moveCommand.sqrMagnitude > 0.01f
            ? (_brain.CurrentRole == EnemyRole.Attacker || _brain.CurrentRole == EnemyRole.Flanker
                ? EnemyStateID.Chasing
                : EnemyStateID.Wandering)
            : EnemyStateID.Wandering;

        if (desired != CurrentState) TransitionTo(desired);

        FacePlayer();
    }

    private void TickWindup()
    {
        ApplyFriction();
        FacePlayer();
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0f)
            TransitionTo(EnemyStateID.Attacking);
    }

    private void TickAttacking()
    {
        if (_activeAttack != null)
        {
            Vector2 move = _activeAttack.GetMovementForPhase(EnemyAttackPhase.Active);
            float fwd = _facingRight ? 1f : -1f;
            _velX = Mathf.Lerp(_velX, move.x * fwd, 10f * Time.deltaTime);
        }

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            if (attackHitbox != null) attackHitbox.DeactivateHitbox();
            TransitionTo(EnemyStateID.Recovery);
        }
    }

    private void TickRecovery()
    {
        ApplyFriction();
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0f)
        {
            _activeAttack = null;
            TransitionTo(EnemyStateID.Wandering);
        }
    }

    private void TickBlocking()
    {
        ApplyFriction();
        FacePlayer();
    }

    private void TickTaunting()
    {
        ApplyFriction();
        FacePlayer();
    }

    private void TickHurt()
    {
        if (!IsGrounded) TickFallPhysics();
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f && IsGrounded)
        {
            _velX = 0f;
            TransitionTo(EnemyStateID.Wandering);
        }
    }

    private void TickKnockedDown()
    {
        if (!IsGrounded) TickFallPhysics();
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f && IsGrounded)
        {
            _stateTimer = stats.getUpDuration;
            TransitionTo(EnemyStateID.GetUp);
        }
    }

    private void TickGetUp()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            TransitionTo(EnemyStateID.Wandering);
    }

    private void TransitionTo(EnemyStateID next)
    {
        if (next == CurrentState) return;
        OnExitState(CurrentState, next);
        CurrentState = next;
        OnEnterState(next);
    }

    private void OnEnterState(EnemyStateID state)
    {
        switch (state)
        {
            case EnemyStateID.Idle:
                _stateTimer = Random.Range(0.1f, 0.3f);
                break;

            case EnemyStateID.Windup:
                _stateTimer = _activeAttack != null
                    ? _activeAttack.StartupDuration
                    : stats.windupDuration;
                break;

            case EnemyStateID.Attacking:
                _stateTimer = _activeAttack != null
                    ? _activeAttack.ActiveDuration
                    : stats.attackDuration;

                if (attackHitbox != null && _activeAttack != null)
                {
                    attackHitbox.damage = _activeAttack.damage;
                    attackHitbox.causesKnockdown = _activeAttack.causesKnockdown;

                    Vector2 kb = _activeAttack.knockback;
                    kb.x = _facingRight ? Mathf.Abs(kb.x) : -Mathf.Abs(kb.x);
                    attackHitbox.knockback = kb;

                    Vector2 offset = _activeAttack.hitboxOffset;
                    if (!_facingRight) offset.x = -offset.x;
                    attackHitbox.transform.localPosition = new Vector3(offset.x, offset.y, 0f);

                    var col = attackHitbox.GetComponent<BoxCollider2D>();
                    if (col != null) col.size = _activeAttack.hitboxSize;

                    attackHitbox.ActivateHitbox();
                }

                if (animator != null && _activeAttack != null &&
                    !string.IsNullOrEmpty(_activeAttack.animTriggerName))
                    animator.SetTrigger(Animator.StringToHash(_activeAttack.animTriggerName));
                break;

            case EnemyStateID.Recovery:
                _stateTimer = _activeAttack != null
                    ? _activeAttack.RecoveryDuration
                    : stats.recoveryDuration;
                break;

            case EnemyStateID.Hurt:
                _stateTimer = stats.hurtDuration;
                if (attackHitbox != null) attackHitbox.DeactivateHitbox();
                break;

            case EnemyStateID.KnockedDown:
                _stateTimer = stats.knockdownDuration;
                if (attackHitbox != null) attackHitbox.DeactivateHitbox();
                break;

            case EnemyStateID.GetUp:
                _stateTimer = stats.getUpDuration;
                break;

            case EnemyStateID.Dead:
                if (attackHitbox != null) attackHitbox.DeactivateHitbox();
                _velX = 0f;
                _velDepth = 0f;
                _jumpHeight = 0f;
                _jumpVelocity = 0f;
                _brain?.NotifyDeath();
                StartCoroutine(DestroyAfterDelay(1.5f));
                break;
        }
    }

    private void OnExitState(EnemyStateID exiting, EnemyStateID entering)
    {
        bool leavingAttack = exiting == EnemyStateID.Windup ||
                             exiting == EnemyStateID.Attacking ||
                             exiting == EnemyStateID.Recovery;
        bool enteringAttack = entering == EnemyStateID.Windup ||
                              entering == EnemyStateID.Attacking ||
                              entering == EnemyStateID.Recovery;

        if (leavingAttack && !enteringAttack)
            if (attackHitbox != null) attackHitbox.DeactivateHitbox();
    }


    public void TakeDamage(int amount, Vector2 knockback, bool knockdown = false)
    {
        
        if (!IsAlive || CurrentState == EnemyStateID.Dead) return;

        if (CurrentState == EnemyStateID.Blocking)
        {
            amount = Mathf.RoundToInt(amount * blockDamageMultiplier);
            knockdown = false;
            knockback *= 0.2f;

            _currentHealth -= amount;
            if (_currentHealth <= 0) { _currentHealth = 0; TransitionTo(EnemyStateID.Dead); }
            return;
        }

        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            TransitionTo(EnemyStateID.Dead);
            return;
        }

        if(hasBeenHit)
        {
            _velX = knockback.x;

            Debug.Log(_velX);
            // _jumpVelocity = knockback.y;
            if (knockback.y > 0f)
            {
                _jumpVelocity = knockback.y;
                if (IsGrounded)
                {
                    _jumpHeight = 0.01f; // lift off floor
                }

            }
            else if (knockback.y < 0f)
            {
                // Spike
                _jumpVelocity = knockback.y;
            }

            //Tutaj, dla lepszego gamefeelu można dodać później coś w stylu "tylko jeśli atak nadejdzie od postaci w powietrzu"
            if (!IsGrounded)
            {
                Debug.Log("Gówno");
                //_jumpVelocity = 0f;
                _jumpVelocity = Mathf.Max(_jumpVelocity, stats.juggleUplift);
            }
        }

        

        hasBeenHit = true;

        TransitionTo(knockdown ? EnemyStateID.KnockedDown : EnemyStateID.Hurt);
    }

    private void ApplyFriction()
    {
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);
        _velDepth = Mathf.MoveTowards(_velDepth, 0f, stats.deceleration * Time.deltaTime);
        _groundY += _velDepth * Time.deltaTime;
    }

    private void TickFallPhysics()
    {
        float g = 3.5f * Mathf.Abs(Physics2D.gravity.y);
        _jumpVelocity -= g * Time.deltaTime;
        _jumpVelocity = Mathf.Max(_jumpVelocity, -20f);
        _jumpHeight += _jumpVelocity * Time.deltaTime;
    }


    private void FacePlayer()
    {
        if (_playerTransform == null) return;
        float diff = _playerTransform.position.x - transform.position.x;
        if (diff > 0.05f && !_facingRight) Flip();
        if (diff < -0.05f && _facingRight) Flip();
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }


    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger(AnimStateID, (int)CurrentState);
    }


    private void OnDrawGizmosSelected()
    {
        float x = transform.position.x;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(x - 5f, groundYMin), new Vector3(x + 5f, groundYMin));
        Gizmos.DrawLine(new Vector3(x - 5f, groundYMax), new Vector3(x + 5f, groundYMax));

        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(x, _groundY), 0.1f);
    }
}
