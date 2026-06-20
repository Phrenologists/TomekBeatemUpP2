using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(EnemyController))]
public class EnemyAIBrain : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Tag that identifies this enemy type in the CombatDirector presence registry. " +
             "E.g. 'Grunt', 'Heavy', 'Commander'. All enemies of the same type share a tag.")]
    [SerializeField] private string typeTag = "Grunt";

    [Header("Attack Data")]
    [SerializeField] private EnemyAttackRoster roster;

    [Header("Wander Settings")]
    [Tooltip("Maximum distance from current position the enemy will wander when waiting.")]
    [SerializeField] private float wanderRadius = 1.5f;
    [Tooltip("Seconds spent at a wander destination before picking a new one.")]
    [SerializeField] private float wanderPauseMin = 0.5f;
    [SerializeField] private float wanderPauseMax = 1.8f;
    [Tooltip("Maximum Y distance the enemy will wander from the player's depth lane. " +
             "Keeps wandering enemies from disappearing off screen.")]
    [SerializeField] private float wanderMaxDepthFromPlayer = 2f;

    [Header("Taunt Settings")]
    [Tooltip("Chance (0-1) that a waiting enemy taunts instead of wandering " +
             "after being idle for tauntCooldown seconds.")]
    [SerializeField] private float tauntChance = 0.25f;
    [SerializeField] private float tauntCooldown = 4f;
    [Tooltip("How long a taunt lasts before returning to Waiter behaviour.")]
    [SerializeField] private float tauntDuration = 1.2f;

    [Header("Block Settings")]
    [Tooltip("Chance per second (while Waiting) that the enemy enters block stance " +
             "when the player is in an attack state.")]
    [SerializeField] private float blockChancePerSecond = 0.3f;
    [Tooltip("Maximum seconds the enemy holds a block before dropping it.")]
    [SerializeField] private float maxBlockDuration = 1.5f;
    [Tooltip("Minimum seconds before the enemy can block again after dropping.")]
    [SerializeField] private float blockCooldown = 2f;

    [Header("Retreat Settings")]
    [Tooltip("Distance the enemy tries to move away from the player after attacking.")]
    [SerializeField] private float retreatDistance = 2f;
    [Tooltip("Speed multiplier applied during retreat (relative to walk speed).")]
    [SerializeField] private float retreatSpeedMult = 0.8f;
    [Tooltip("Seconds spent retreating before rejoining the wait pool.")]
    [SerializeField] private float retreatDuration = 0.6f;

    [Header("Presence Reactions")]
    [Tooltip("Behaviour modifier rules that fire when a specific enemy type " +
             "enters or leaves the encounter.")]
    [SerializeField] private List<PresenceReaction> presenceReactions = new List<PresenceReaction>();

    public EnemyRole CurrentRole { get; private set; } = EnemyRole.Waiter;
    public BrainState CurrentBrain { get; private set; } = BrainState.Wandering;
    public string TypeTag => typeTag;

    private EnemyController _controller;
    private PlayerController _playerController;
    private Transform _playerTransform;

    private float _brainTimer;
    private float _tauntCooldownTimer;
    private float _blockCooldownTimer;

    private EnemyAttackData _activeAttack;

    private Vector2 _wanderTarget;
    private bool _hasWanderTarget;


    private float _speedMultiplier = 1f;
    private float _aggressionMultiplier = 1f;   // scales attack cooldown down


    public CombatDirector CombatDirector;

    private EnemyRole _roleBeforeRetreat;



    private void Awake()
    {
        _controller = GetComponent<EnemyController>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerController = playerObj.GetComponent<PlayerController>();
        }

        if (CombatDirector != null)
        {
            CombatDirector.RegisterEnemy(this, typeTag);
            CombatDirector.OnEnemyTypeRegistered += HandleEnemyTypeRegistered;
            CombatDirector.OnEnemyTypeRemoved += HandleEnemyTypeRemoved;
        }
        else
        {
            Debug.LogWarning("EnemyAIBrain: No CombatDirector found in scene.", this);
        }
    }

    private void OnDestroy()
    {
        if (CombatDirector.Instance != null)
        {
            CombatDirector.Instance.UnregisterEnemy(this, typeTag);
            CombatDirector.Instance.OnEnemyTypeRegistered -= HandleEnemyTypeRegistered;
            CombatDirector.Instance.OnEnemyTypeRemoved -= HandleEnemyTypeRemoved;
        }
    }

    private void Update()
    {
        if (!_controller.IsAlive) return;

        //The brain won't make AI decisions while the controller is handling reactive states
        if (_controller.IsInReactiveState()) return;

        TickTimers();
        TickBrainState();
    }



    public void AssignRole(EnemyRole role)
    {
        if (role == CurrentRole) return;
        CurrentRole = role;

        // Immediately reacts to role changes
        switch (role)
        {
            case EnemyRole.Attacker:
                SetBrainState(BrainState.Pursuing);
                break;
            case EnemyRole.Flanker:
                SetBrainState(BrainState.Pursuing);
                break;
            case EnemyRole.Waiter:
                // Don't interrupt taunting or blocking
                if (CurrentBrain != BrainState.Taunting &&
                    CurrentBrain != BrainState.Blocking)
                    SetBrainState(BrainState.Wandering);
                break;
        }
    }



    private void TickBrainState()
    {
        switch (CurrentBrain)
        {
            case BrainState.Wandering: TickWandering(); break;
            case BrainState.Pursuing: TickPursuing(); break;
            case BrainState.WaitingForToken: TickWaitingForToken(); break;
            case BrainState.RequestingAttack: TickRequestingAttack(); break;
            case BrainState.Attacking: TickAttacking(); break;
            case BrainState.Retreating: TickRetreating(); break;
            case BrainState.Taunting: TickTaunting(); break;
            case BrainState.Blocking: TickBlocking(); break;
        }
    }


    private void TickWandering()
    {
        if (_tauntCooldownTimer <= 0f && Random.value < tauntChance * Time.deltaTime)
        {
            SetBrainState(BrainState.Taunting);
            return;
        }

        TryEnterBlock();

        if (!_hasWanderTarget || _brainTimer <= 0f)
        {
            bool shouldPause = Random.value < 0.4f;
            if (shouldPause)
            {
                _controller.SetMoveCommand(Vector2.zero);
                _hasWanderTarget = false;
                _brainTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            }
            else
            {
                PickWanderTarget();
            }
            return;
        }

        Vector2 myPos = new Vector2(transform.position.x, _controller.GroundY);
        Vector2 toTarget = _wanderTarget - myPos;

        if (toTarget.sqrMagnitude < 0.05f)
        {
            _controller.SetMoveCommand(Vector2.zero);
            _hasWanderTarget = false;
            _brainTimer = Random.Range(wanderPauseMin, wanderPauseMax);
        }
        else
        {
            Vector2 dir = toTarget.normalized * _speedMultiplier * 0.5f; // half speed while wandering, probably should turn this into a modifiable variable
            _controller.SetMoveCommand(dir);
        }
    }


    // Attacker / Flanker behaviour: move toward the player (or flank position).
    private void TickPursuing()
    {
        if (_playerTransform == null) return;

        float playerGroundY = GetPlayerGroundY();
        float diffX = _playerTransform.position.x - transform.position.x;
        float diffDepth = playerGroundY - _controller.GroundY;

        // Flankers aim for a position offset from the player, not directly at them
        if (CurrentRole == EnemyRole.Flanker)
        {
            float flankSide = transform.position.x < _playerTransform.position.x ? -1f : 1f;
            diffX = (_playerTransform.position.x + flankSide * 1.5f) - transform.position.x;
        }

        Vector2 moveDir = new Vector2(
            Mathf.Abs(diffX) > 0.1f ? Mathf.Sign(diffX) : 0f,
            Mathf.Abs(diffDepth) > 0.1f ? Mathf.Sign(diffDepth) : 0f
        ) * _speedMultiplier;

        _controller.SetMoveCommand(moveDir);

        // Check if in range for any available attack
        if (IsInRangeForAnyAttack())
            SetBrainState(BrainState.WaitingForToken);
    }

    // In attack range but waiting for the director to grant a token.
    private void TickWaitingForToken()
    {
        _controller.SetMoveCommand(Vector2.zero);
        TryEnterBlock();

        // If player moved away, resume pursuing
        if (!IsInRangeForAnyAttack())
        {
            SetBrainState(BrainState.Pursuing);
            return;
        }

        SetBrainState(BrainState.RequestingAttack);
    }

    // Actively asks the director for a token each frame until granted or the player moves out of range.
    private void TickRequestingAttack()
    {
        _controller.SetMoveCommand(Vector2.zero);

        if (!IsInRangeForAnyAttack())
        {
            SetBrainState(BrainState.Pursuing);
            return;
        }

        // Selects the best attack for current conditions
        EnemyAttackData chosen = SelectAttack();
        if (chosen == null)
        {
            SetBrainState(BrainState.Pursuing);
            return;
        }

        // Asks the director for tokens
        if (CombatDirector.Instance != null &&
            CombatDirector.Instance.RequestTokens(this, chosen.tokenCost))
        {
            _activeAttack = chosen;
            _controller.BeginAttack(chosen);
            SetBrainState(BrainState.Attacking);
        }
        // If denied, stays in RequestingAttack and try again next frame
    }

    // Waits for EnemyController to signal the attack is complete.
    private void TickAttacking()
    {
        // EnemyController.IsAttacking goes false when the attack finishes
        if (!_controller.IsAttacking)
        {
            CombatDirector.Instance?.ReleaseTokens(this);
            _activeAttack = null;
            SetBrainState(BrainState.Retreating);
        }
    }

    // Briefly move away from the player after attacking.
    private void TickRetreating()
    {
        _brainTimer -= Time.deltaTime;

        if (_playerTransform != null)
        {
            float awayDir = transform.position.x < _playerTransform.position.x ? -1f : 1f;
            _controller.SetMoveCommand(new Vector2(awayDir * retreatSpeedMult, 0f));
        }

        if (_brainTimer <= 0f)
        {
            Debug.Log("Back to old role");
            _controller.SetMoveCommand(Vector2.zero);
            // Return to whatever role the director assigned
            AssignRole(_roleBeforeRetreat);
        }
    }

    private void TickTaunting()
    {
        _controller.SetMoveCommand(Vector2.zero);
        _brainTimer -= Time.deltaTime;

        if (_brainTimer <= 0f)
        {
            _tauntCooldownTimer = tauntCooldown;
            _controller.EndTaunt();
            SetBrainState(BrainState.Wandering);
        }
    }

    private void TickBlocking()
    {
        _brainTimer -= Time.deltaTime;

        // Drops block if timer expires or player is no longer attacking
        bool playerAttacking = _playerController != null &&
            (_playerController.CurrentState == PlayerStateID.Attacking ||
             _playerController.CurrentState == PlayerStateID.AirAttacking);

        if (_brainTimer <= 0f || !playerAttacking)
        {
            _blockCooldownTimer = blockCooldown;
            _controller.EndBlock();
            SetBrainState(CurrentRole == EnemyRole.Waiter
                ? BrainState.Wandering
                : BrainState.Pursuing);
        }
    }



    private void TickTimers()
    {
        if (_tauntCooldownTimer > 0f) _tauntCooldownTimer -= Time.deltaTime;
        if (_blockCooldownTimer > 0f) _blockCooldownTimer -= Time.deltaTime;
    }

    private void SetBrainState(BrainState next)
    {
        if (next == CurrentBrain) return;

        OnExitBrainState(CurrentBrain);
        CurrentBrain = next;
        OnEnterBrainState(next);
    }

    private void OnEnterBrainState(BrainState state)
    {
        switch (state)
        {
            case BrainState.Retreating:
                _brainTimer = retreatDuration;
                _roleBeforeRetreat = CurrentRole;
                CurrentRole = EnemyRole.Retreating;
                break;
            case BrainState.Taunting:
                _brainTimer = tauntDuration;
                CurrentRole = EnemyRole.Taunting;
                _controller.BeginTaunt();
                break;
            case BrainState.Blocking:
                _brainTimer = Random.Range(0.3f, maxBlockDuration);
                CurrentRole = EnemyRole.Blocking;
                _controller.BeginBlock();
                break;
            case BrainState.Wandering:
                _hasWanderTarget = false;
                _brainTimer = 0f;
                break;
        }
    }

    private void OnExitBrainState(BrainState state) { }

    private void TryEnterBlock()
    {
        if (_blockCooldownTimer > 0f) return;
        if (CurrentBrain == BrainState.Blocking) return;

        bool playerAttacking = _playerController != null &&
            (_playerController.CurrentState == PlayerStateID.Attacking ||
             _playerController.CurrentState == PlayerStateID.AirAttacking);

        if (!playerAttacking) return;

        float chance = blockChancePerSecond * Time.deltaTime;
        if (Random.value < chance)
            SetBrainState(BrainState.Blocking);
    }

    private void PickWanderTarget()
    {
        float playerGroundY = GetPlayerGroundY();

        float randX = transform.position.x + Random.Range(-wanderRadius, wanderRadius);
        float randY = _controller.GroundY + Random.Range(-wanderRadius * 0.5f, wanderRadius * 0.5f);

        // Keep within depth band relative to player, might change later
        if (_playerController != null)
            randY = Mathf.Clamp(randY,
                playerGroundY - wanderMaxDepthFromPlayer,
                playerGroundY + wanderMaxDepthFromPlayer);

        _wanderTarget = new Vector2(randX, randY);
        _hasWanderTarget = true;
        _brainTimer = 3f;   // max time to reach target before picking again
    }

    private bool IsInRangeForAnyAttack()
    {
        if (roster == null || _playerTransform == null) return false;

        float distX = Mathf.Abs(_playerTransform.position.x - transform.position.x);
        float distDepth = Mathf.Abs(GetPlayerGroundY() - _controller.GroundY);
        int budget = CombatDirector.Instance?.CurrentBudget ?? 999;
        float intensity = CombatDirector.Instance?.Intensity ?? 0f;

        return roster.SelectAttack(distX, distDepth, budget, intensity, CurrentRole) != null;
    }

    private EnemyAttackData SelectAttack()
    {
        if (roster == null || _playerTransform == null) return null;

        float distX = Mathf.Abs(_playerTransform.position.x - transform.position.x);
        float distDepth = Mathf.Abs(GetPlayerGroundY() - _controller.GroundY);
        int budget = CombatDirector.Instance?.CurrentBudget ?? 0;
        float intensity = CombatDirector.Instance?.Intensity ?? 0f;

        return roster.SelectAttack(distX, distDepth, budget, intensity, CurrentRole);
    }

    private float GetPlayerGroundY() =>
        _playerController != null ? _playerController.GroundY
        : _playerTransform != null ? _playerTransform.position.y
        : transform.position.y;


    private void HandleEnemyTypeRegistered(string tag)
    {
        foreach (var reaction in presenceReactions)
        {
            if (reaction.enemyTypeTag == tag && reaction.triggerOn == PresenceTrigger.OnPresent)
                ApplyPresenceReaction(reaction);
        }
    }

    private void HandleEnemyTypeRemoved(string tag)
    {
        foreach (var reaction in presenceReactions)
        {
            if (reaction.enemyTypeTag == tag && reaction.triggerOn == PresenceTrigger.OnAbsent)
                ApplyPresenceReaction(reaction);
        }

        // Also revert any reactions that were tied to this type being present
        RebuildPresenceMultipliers();
    }

    private void ApplyPresenceReaction(PresenceReaction reaction)
    {
        // Applied additively, multiple reactions stack
        _speedMultiplier += reaction.speedModifier - 1f;
        _aggressionMultiplier += reaction.aggressionModifier - 1f;

        _speedMultiplier = Mathf.Max(0.1f, _speedMultiplier);
        _aggressionMultiplier = Mathf.Max(0.1f, _aggressionMultiplier);
    }

    private void RebuildPresenceMultipliers()
    {
        // Reset to base then reapply all currently valid reactions
        _speedMultiplier = 1f;
        _aggressionMultiplier = 1f;

        if (CombatDirector.Instance == null) return;

        foreach (var reaction in presenceReactions)
        {
            bool present = CombatDirector.Instance.IsPresent(reaction.enemyTypeTag);
            if (reaction.triggerOn == PresenceTrigger.OnPresent && present)
                ApplyPresenceReaction(reaction);
            else if (reaction.triggerOn == PresenceTrigger.OnAbsent && !present)
                ApplyPresenceReaction(reaction);
        }
    }


    public void NotifyDeath()
    {
        CombatDirector.Instance?.ReleaseTokens(this);
        CombatDirector.Instance?.AddKill(1f);
    }
}


public enum BrainState
{
    Wandering,        // idle walk, small random movements
    Pursuing,         // moving toward player or flank position
    WaitingForToken,  // in attack range, waiting for director permission
    RequestingAttack, // actively trying to get a token this frame
    Attacking,        // attack in progress, waiting for controller to finish
    Retreating,       // post-attack fallback
    Taunting,         // playing taunt animation
    Blocking,         // in block stance
}


[Serializable]
public class PresenceReaction
{
    [Tooltip("Enemy type tag to react to (must match the typeTag on that enemy's EnemyAIBrain).")]
    public string enemyTypeTag;

    [Tooltip("Whether to trigger when the type appears (OnPresent) or disappears (OnAbsent).")]
    public PresenceTrigger triggerOn;

    [Tooltip("Speed multiplier applied to this enemy while the condition is true. 1 = no change.")]
    public float speedMultiplier = 1f;

    // Stored as a field name matching ApplyPresenceReaction
    [Tooltip("How this reaction changes movement speed. 1 = unchanged, 1.5 = 50% faster.")]
    public float speedModifier = 1f;

    [Tooltip("How this reaction changes attack aggression. 1 = unchanged, 2 = twice as aggressive.")]
    public float aggressionModifier = 1f;

    [Tooltip("Optional: force a role change when this condition triggers.")]
    public bool overrideRole = false;
    public EnemyRole forcedRole = EnemyRole.None;
}

public enum PresenceTrigger
{
    OnPresent,   // fires when the enemy type enters the encounter
    OnAbsent     // fires when the enemy type is no longer present
}
