using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackHandler : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AttackMap attackMap;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AttackHitbox runtimeHitbox;  // single reusable hitbox child
    public bool IsAttacking => _isActive;
    public AttackData CurrentAttack => _currentAttack;
    public AttackPhase CurrentPhase => _phase;
    public Vector2 CurrentMovement => _currentMovement;

    private Vector2 _currentMovement;

    private PlayerEnergy _energy;
    public float LungeSpeed => (_isActive && _phase == AttackPhase.Active)
        ? _currentAttack.activeLungeSpeed
        : 0f;

    public bool IsFrozenInAir {  get; private set; }

    public System.Action OnAttackSequenceEnded;

    private bool _isActive;
    private AttackData _currentAttack;
    private AttackPhase _phase;
    private float _phaseTimer;

    private string _bufferedInputAction;
    private bool _hasBufferedInput;

    private bool _isAirSequence;
    private float _airStopHitTimer;

    private int _currentTriggerHash;

    private AttackEffectPlayer _effectPlayer;

    private float _phaseElapsed;
    private HashSet<int> _triggeredEffects = new HashSet<int>();

    private void Awake()
    {
        _energy = GetComponent<PlayerEnergy>();
        if (runtimeHitbox != null)
            runtimeHitbox.OnHitConnected += HandleHitConnected;
        _effectPlayer = GetComponent<AttackEffectPlayer>();
    }

    private void OnDestroy()
    {
        if (runtimeHitbox != null)
            runtimeHitbox.OnHitConnected -= HandleHitConnected;
    }

    private void HandleHitConnected()
    {
        if( !_isActive || _currentAttack == null) return;
        if (!_isAirSequence) return;
        if (_currentAttack.airStopMode != AirStopMode.StopOnHit) return;

        IsFrozenInAir = true;
        _airStopHitTimer = _currentAttack.airStopOnHitDuration;
    }

    public bool TryStartAttack(string inputActionName, bool grounded)
    {
        if (_isActive)
        {
            // Only buffer during Recovery — too early to chain otherwise
            if (_phase == AttackPhase.Recovery)
            {
                _bufferedInputAction = inputActionName;
                _hasBufferedInput = true;
            }
            return false;   // not started yet — PlayerController should stay in Attacking state
        }

        AttackData data = attackMap != null ? attackMap.GetAttack(inputActionName, grounded) : null;
        if (data == null) return false;

        if (_energy != null && !_energy.TrySpend(data.energyCost))
        {
            return false;
        }

        _isAirSequence = !grounded;

        StartAttack(data);
        return true;
    }

    public bool Tick(bool facingRight)
    {
        if (!_isActive)
        {
            _currentMovement = Vector2.zero;
            return false;
        }

        _phaseTimer -= Time.deltaTime;
        _phaseElapsed += Time.deltaTime;

        _currentMovement = GetMovement(facingRight);

        if (_isAirSequence && IsFrozenInAir && _currentAttack != null && _currentAttack.airStopMode == AirStopMode.StopOnHit)
        {
            _airStopHitTimer -= Time.deltaTime;
            if (_airStopHitTimer <= 0)
            {
                IsFrozenInAir = false;
            }
        }

        _effectPlayer?.TickEffects(_currentAttack.effects, _phase, _phaseElapsed, facingRight,_triggeredEffects);

        switch (_phase)
        {
            case AttackPhase.Startup:
                if (_phaseTimer <= 0f)
                    EnterActive(facingRight);
                break;

            case AttackPhase.Active:
                // Reposition hitbox each frame so it tracks the character
                UpdateHitboxTransform(facingRight);

                if (_phaseTimer <= 0f)
                    EnterRecovery();
                break;

            case AttackPhase.Recovery:
                if (_phaseTimer <= 0f)
                    TryChainOrEnd(facingRight);
                break;
        }

        return _isActive;
    }

    
    // Interrupts the attack immediately (e.g. player was hit).
    
    public void Cancel()
    {
        DeactivateHitbox();
        _isActive = false;
        _hasBufferedInput = false;
        _bufferedInputAction = null;
        _currentAttack = null;
        _currentMovement = Vector2.zero;
        _phase = AttackPhase.Startup;
        IsFrozenInAir = false;
        _airStopHitTimer = 0f;
        _isAirSequence = false;
        _effectPlayer?.CancelAttackBoundEffects();
        _phaseElapsed = 0f;
        _triggeredEffects.Clear();
    }


    private void StartAttack(AttackData data)
    {
        _currentAttack = data;
        _isActive = true;
        _hasBufferedInput = false;
        _bufferedInputAction = null;
        _phaseElapsed = 0f;
        _triggeredEffects.Clear();

        DeactivateHitbox();

        if (animator != null && !string.IsNullOrEmpty(data.animTriggerName))
        {
            _currentTriggerHash = Animator.StringToHash(data.animTriggerName);
            animator.SetTrigger(_currentTriggerHash);
        }

        _phase = AttackPhase.Startup;
        _phaseTimer = data.StartupDuration;

        _airStopHitTimer = 0f;
        IsFrozenInAir = _isAirSequence && data.airStopMode == AirStopMode.AlwaysStopInAir;
    }

    private void EnterActive(bool facingRight)
    {
        _phase = AttackPhase.Active;
        _phaseTimer = _currentAttack.ActiveDuration;
        _phaseElapsed = 0f;

        if (runtimeHitbox != null)
        {
            runtimeHitbox.damage = _currentAttack.damage;
            runtimeHitbox.knockback = GetFlippedKnockback(facingRight);
            runtimeHitbox.causesKnockdown = _currentAttack.causesKnockdown;
            runtimeHitbox.energyGainOnHit = -1f;   // use PlayerEnergy default per hit
            UpdateHitboxTransform(facingRight);
            runtimeHitbox.ActivateHitbox();
        }
    }

    private void EnterRecovery()
    {
        _phase = AttackPhase.Recovery;
        _phaseTimer = _currentAttack.RecoveryDuration;
        DeactivateHitbox();
    }

    private void TryChainOrEnd(bool facingRight)
    {
        // Determine follow-up: buffered input takes priority, then fixed chain
        AttackData followUp = null;

        if (_hasBufferedInput)
        {
            // Ask the current attack if the buffered input routes to a follow-up
            followUp = _currentAttack.GetFollowUp(_bufferedInputAction);

            // If no specific follow-up for this input, check if there's a fixed chain
            if (followUp == null)
                followUp = _currentAttack.fixedFollowUp;
        }
        else
        {
            // No buffered input — only follow through if a fixed chain is defined
            followUp = _currentAttack.fixedFollowUp;
        }
        if (followUp != null && _energy != null && !_energy.TrySpend(followUp.energyCost))
            followUp = null;

        if (followUp != null)
        {
            StartAttack(followUp);
        }
        else
        {
            // Sequence over
            _effectPlayer?.CancelAttackBoundEffects();
            _phaseElapsed = 0f;
            _triggeredEffects.Clear();
            _isActive = false;
            _hasBufferedInput = false;
            _bufferedInputAction = null;
            _currentAttack = null;
            _currentMovement = Vector2.zero;
            OnAttackSequenceEnded?.Invoke();
            IsFrozenInAir = false;
            _airStopHitTimer = 0f;
        }
    }
    private Vector2 GetMovement(bool facingRight)
    {
        if (_currentAttack == null) return Vector2.zero;

        Vector2 move = _currentAttack.GetMovementForPhase(_phase);

        if (!facingRight) move.x = -move.x;

        return move;
    }

    private void UpdateHitboxTransform(bool facingRight)
    {
        if (runtimeHitbox == null || _currentAttack == null) return;

        Vector2 offset = _currentAttack.hitboxOffset;
        //if (!facingRight) offset.x = -offset.x;

        // Position relative to character root in world space
        runtimeHitbox.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        //Debug.Log(offset.x);
        //Debug.Log(runtimeHitbox.transform.localPosition);


        // Resize the collider
        var col = runtimeHitbox.GetComponent<BoxCollider2D>();
        if (col != null)
            col.size = _currentAttack.hitboxSize;

        //Debug.Log(runtimeHitbox.transform.localPosition);
    }

    private Vector2 GetFlippedKnockback(bool facingRight)
    {
        Vector2 kb = _currentAttack.knockback;
        if (!facingRight) kb.x = -kb.x;
        return kb;
    }

    private void DeactivateHitbox()
    {
        if (runtimeHitbox != null)
            runtimeHitbox.DeactivateHitbox();
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.DrawWireSphere(new Vector3(_currentAttack.hitboxOffset.x, _currentAttack.hitboxOffset.y), 0);
    }
    

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!_isActive || _currentAttack == null) return;

        float remaining = _phaseTimer * (_currentAttack?.targetFPS ?? 60f);
        string label = $"{_currentAttack.attackName}  |  {_phase}  |  {remaining:F0}f remaining";

        GUI.color = _phase switch
        {
            AttackPhase.Startup => Color.yellow,
            AttackPhase.Active => Color.red,
            AttackPhase.Recovery => Color.cyan,
            _ => Color.white
        };

        GUI.Label(new Rect(10, 10, 400, 24), label);
        GUI.color = Color.white;
    }
#endif
}
