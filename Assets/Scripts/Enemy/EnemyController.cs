// COMPONENT REQUIREMENTS:
//   - EnemyStats   (ScriptableObject)
//   - Rigidbody2D  (Gravity Scale = 0)
//   - BoxCollider2D
//   - Animator
//   - SpriteRenderer

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private EnemyStats stats;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AttackHitbox attackHitbox;

    [Header("2.5D Lane Bounds")]
    [SerializeField] private float groundYMin = -3f;
    [SerializeField] private float groundYMax = 3f;

    private static readonly int AnimStateID = Animator.StringToHash("enemyStateID");

    
    public EnemyStateID CurrentState { get; private set; } = EnemyStateID.Idle;
    public bool IsGrounded => _jumpHeight <= 0f;
    public bool IsAlive => _currentHealth > 0;
    public int CurrentHealth => _currentHealth;

    
    private Rigidbody2D _rb;

    
    private Transform _playerTransform;
    private PlayerController _playerController;

    
    private float _groundY;
    private float _jumpHeight;
    private float _jumpVelocity;
    private float _velX;
    private float _velDepth;

    
    private bool _facingRight = true;

    
    private float _stateTimer;
    public float _attackCooldownTimer;

    
    private int _currentHealth;

    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        _currentHealth = stats.maxHealth;
        _groundY = transform.position.y;

        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerController = playerObj.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("EnemyController: No GameObject tagged 'Player' found.", this);
        }

       
        if (attackHitbox != null)
            attackHitbox.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateAnimator();
        //if (!IsAlive) return;

        
        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;

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

    }

    

    private void TickState()
    {
        switch (CurrentState)
        {
            case EnemyStateID.Idle: TickIdle(); break;
            case EnemyStateID.Chasing: TickChasing(); break;
            case EnemyStateID.Windup: TickWindup(); break;
            case EnemyStateID.Attacking: TickAttacking(); break;
            case EnemyStateID.Recovery: TickRecovery(); break;
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
            TransitionTo(EnemyStateID.Chasing);
    }

    
    private void TickChasing()
    {
        if (_playerTransform == null) return;

        float playerGroundY = GetPlayerGroundY();

        float diffX = _playerTransform.position.x - transform.position.x;
        float diffDepth = playerGroundY - _groundY;

        
        float targetVX = Mathf.Sign(diffX) * stats.walkSpeed;
        float targetVDepth = Mathf.Sign(diffDepth) * stats.depthSpeed;

        
        if (Mathf.Abs(diffX) < 0.05f) targetVX = 0f;
        if (Mathf.Abs(diffDepth) < 0.05f) targetVDepth = 0f;

        _velX = Mathf.MoveTowards(_velX, targetVX, stats.acceleration * Time.deltaTime);
        _velDepth = Mathf.MoveTowards(_velDepth, targetVDepth, stats.acceleration * Time.deltaTime);
        _groundY += _velDepth * Time.deltaTime;

        FacePlayer();

        
        if (_attackCooldownTimer <= 0f && IsInAttackRange())
        {
            //Debug.Log("Attacking");
            _velX = 0f;
            _velDepth = 0f;
            TransitionTo(EnemyStateID.Windup);
        }
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
        ApplyFriction();

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
            _attackCooldownTimer = stats.attackCooldown;
            TransitionTo(EnemyStateID.Chasing);
        }
    }

    
    private void TickHurt()
    {
        if (!IsGrounded) TickFallPhysics();
        _velX = Mathf.MoveTowards(_velX, 0f, stats.deceleration * Time.deltaTime);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f && IsGrounded)
        {
            _velX = 0f;
            TransitionTo(EnemyStateID.Chasing);
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
        {
            _attackCooldownTimer = stats.attackCooldown * 0.5f; 
            TransitionTo(EnemyStateID.Chasing);
        }
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

    

    private void TransitionTo(EnemyStateID next)
    {
        if (next == CurrentState) return;
        OnExitState(CurrentState);
        CurrentState = next;
        OnEnterState(next);
    }

    private void OnEnterState(EnemyStateID state)
    {
        switch (state)
        {
            case EnemyStateID.Idle:
                //Will have to put a patrol/move around randomly routine here
                _stateTimer = Random.Range(0.1f, 0.4f);
                break;

            case EnemyStateID.Windup:
                _stateTimer = stats.windupDuration;
                break;

            case EnemyStateID.Attacking:
                _stateTimer = stats.attackDuration;
                
                if (attackHitbox != null) attackHitbox.ActivateHitbox();
                break;

            case EnemyStateID.Recovery:
                _stateTimer = stats.recoveryDuration;
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
                _jumpVelocity = 0f;
                _jumpHeight = 0f;
                StartCoroutine(DestroyAfterDelay(1.5f));
                break;
        }
    }

    private void OnExitState(EnemyStateID state) { }

    

    public void TakeDamage(int amount, Vector2 knockback, bool knockdown = false)
    {
        if (!IsAlive) return;
        if (CurrentState == EnemyStateID.Dead) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            TransitionTo(EnemyStateID.Dead);
            //Debug.Log(CurrentState);
            return;
        }

        _velX = knockback.x;
        _jumpVelocity = knockback.y;
        if (knockback.y > 0f) _jumpHeight = 0.01f;

        if (knockdown)
            TransitionTo(EnemyStateID.KnockedDown);
        else
            TransitionTo(EnemyStateID.Hurt);
    }

    
    private bool IsInAttackRange()
    {
        if (_playerTransform == null) return false;
        //Debug.Log("PlayerFound");

        float diffX = Mathf.Abs(_playerTransform.position.x - transform.position.x);
        float diffDepth = Mathf.Abs(GetPlayerGroundY() - _groundY);

        return diffX <= stats.attackRangeX && diffDepth <= stats.attackRangeDepth;
    }

    
    private float GetPlayerGroundY()
    {
        if (_playerController != null)
        {
            
            return _playerController.GroundY;
        }
        return _playerTransform != null ? _playerTransform.position.y : transform.position.y;
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
        //Lane bounds
        float x = transform.position.x;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(x - 5f, groundYMin), new Vector3(x + 5f, groundYMin));
        Gizmos.DrawLine(new Vector3(x - 5f, groundYMax), new Vector3(x + 5f, groundYMax));

        //Attack range box
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, _groundY),
            new Vector3(stats != null ? stats.attackRangeX * 2f : 1f,
                        stats != null ? stats.attackRangeDepth * 2f : 0.5f,
                        0f));

        //Current ground position
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(x, _groundY), 0.1f);
    }
}
