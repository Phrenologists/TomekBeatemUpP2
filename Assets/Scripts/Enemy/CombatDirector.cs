using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatDirector : MonoBehaviour
{
    public static CombatDirector Instance { get; private set; }

    [Header("Token Budget")]
    [Tooltip("Base token budget available at intensity 0.")]
    [SerializeField] private int baseTokenBudget = 3;

    [Tooltip("Additional budget added at full intensity (1.0).")]
    [SerializeField] private int bonusBudgetAtMaxIntensity = 4;

    [Header("Intensity")]
    [Tooltip("How much intensity is added per enemy kill. Scale by enemy value in AddKill().")]
    [SerializeField] private float intensityPerKillUnit = 0.05f;

    [Tooltip("Passive intensity ramp — added per second automatically.")]
    [SerializeField] private float passiveIntensityRamp = 0.005f;

    [Tooltip("Maximum passive ramp rate (intensity won't ramp beyond this threshold passively).")]
    [SerializeField] private float passiveRampCeiling = 0.7f;

    [Tooltip("How quickly intensity decays when no enemies are present (per second).")]
    [SerializeField] private float intensityDecayRate = 0.02f;

    [Header("Timing")]
    [Tooltip("Minimum seconds between two enemies being granted an attack token " +
             "simultaneously. Prevents all enemies swinging at the same moment.")]
    [SerializeField] private float minTimeBetweenGrants = 0.3f;

    [Tooltip("How often (seconds) the director re-evaluates role assignments.")]
    [SerializeField] private float roleReassignInterval = 1.5f;

    public Action<float> OnIntensityChanged;

    public Action<int> OnBudgetChanged;

    public Action<string> OnEnemyTypeRegistered;

    public Action<string> OnEnemyTypeRemoved;

    public float Intensity => _intensity;
    public int CurrentBudget => _currentBudget;
    public int MaxBudget => Mathf.RoundToInt(Mathf.Lerp(baseTokenBudget, baseTokenBudget + bonusBudgetAtMaxIntensity, _intensity));

    private float _intensity = 0f;
    private int _currentBudget = 0;
    private float _lastGrantTime = -999f;
    private float _roleReassignTimer = 0f;

    private readonly List<EnemyAIBrain> _enemies = new List<EnemyAIBrain>();

    private readonly Dictionary<string, int> _presenceCounts = new Dictionary<string, int>();

    private readonly Dictionary<EnemyAIBrain, int> _heldTokens = new Dictionary<EnemyAIBrain, int>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _currentBudget = baseTokenBudget;
    }

    private void Update()
    {
        TickIntensity();
        SyncBudgetToIntensity();

        _roleReassignTimer -= Time.deltaTime;
        if (_roleReassignTimer <= 0f)
        {
            ReassignRoles();
            _roleReassignTimer = roleReassignInterval;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    public void RegisterEnemy(EnemyAIBrain brain, string typeTag)
    {
        if (_enemies.Contains(brain)) return;

        _enemies.Add(brain);
        _heldTokens[brain] = 0;

        if (!_presenceCounts.ContainsKey(typeTag))
        {
            _presenceCounts[typeTag] = 0;
            OnEnemyTypeRegistered?.Invoke(typeTag);
        }
        _presenceCounts[typeTag]++;
    }

    public void UnregisterEnemy(EnemyAIBrain brain, string typeTag)
    {
        if (!_enemies.Contains(brain)) return;

        if (_heldTokens.TryGetValue(brain, out int held) && held > 0)
        {
            _currentBudget += held;
            OnBudgetChanged?.Invoke(_currentBudget);
        }

        _enemies.Remove(brain);
        _heldTokens.Remove(brain);

        if (_presenceCounts.ContainsKey(typeTag))
        {
            _presenceCounts[typeTag]--;
            if (_presenceCounts[typeTag] <= 0)
            {
                _presenceCounts.Remove(typeTag);
                OnEnemyTypeRemoved?.Invoke(typeTag);
            }
        }
    }


    public bool RequestTokens(EnemyAIBrain brain, int cost)
    {
        if (cost <= 0) return true;
        if (_currentBudget < cost) return false;

        if (Time.time - _lastGrantTime < minTimeBetweenGrants) return false;

        _currentBudget -= cost;
        _heldTokens[brain] = (_heldTokens.TryGetValue(brain, out int current) ? current : 0) + cost;
        _lastGrantTime = Time.time;

        OnBudgetChanged?.Invoke(_currentBudget);
        return true;
    }


    public void ReleaseTokens(EnemyAIBrain brain)
    {
        if (!_heldTokens.TryGetValue(brain, out int held) || held <= 0) return;

        _currentBudget = Mathf.Min(_currentBudget + held, MaxBudget);
        _heldTokens[brain] = 0;

        OnBudgetChanged?.Invoke(_currentBudget);
    }


    public int GetHeldTokens(EnemyAIBrain brain) => _heldTokens.TryGetValue(brain, out int held) ? held : 0;


    public void AddKill(float killValue = 1f)
    {
        SetIntensity(_intensity + intensityPerKillUnit * killValue);
    }


    public void AddBossHealthLoss(float fraction)
    {
        SetIntensity(_intensity + fraction);
    }

    public void SetIntensity(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, _intensity)) return;
        _intensity = clamped;
        OnIntensityChanged?.Invoke(_intensity);
    }


    public bool IsPresent(string typeTag) => _presenceCounts.TryGetValue(typeTag, out int count) && count > 0;

    public int CountPresent(string typeTag) => _presenceCounts.TryGetValue(typeTag, out int count) ? count : 0;

    public IEnumerable<string> PresentTypes() => _presenceCounts.Keys;


//Role Assignemnt
    private void ReassignRoles()
    {
        if (_enemies.Count == 0) return;

        //Ok here's how this works: Each enemy has a role and those roles get assigned
        //based on multiple conditions. Those roles are defined in EnemyAttackData and they
        //tell the enemy if they should attack, move around, hold position, block, etc.
        int maxAttackers = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 3f, _intensity)));
        int maxFlankers = Mathf.RoundToInt(Mathf.Lerp(0f, 2f, _intensity));

        int attackerCount = 0;
        int flankerCount = 0;

        // Enemies already holding tokens keep Attacker role
        foreach (var brain in _enemies)
        {
            if (GetHeldTokens(brain) > 0)
            {
                brain.AssignRole(EnemyRole.Attacker);
                attackerCount++;
            }
        }

        // Assigns remaining roles
        foreach (var brain in _enemies)
        {
            // Skip enemies already assigned above or in a non-negotiable state
            if (brain.CurrentRole == EnemyRole.Attacker) continue;
            if (brain.CurrentRole == EnemyRole.Blocking) continue;
            if (brain.CurrentRole == EnemyRole.Taunting) continue;
            if (brain.CurrentRole == EnemyRole.Retreating) continue;

            if (attackerCount < maxAttackers)
            {
                brain.AssignRole(EnemyRole.Attacker);
                attackerCount++;
            }
            else if (flankerCount < maxFlankers)
            {
                brain.AssignRole(EnemyRole.Flanker);
                flankerCount++;
            }
            else
            {
                brain.AssignRole(EnemyRole.Waiter);
            }
        }
    }


    private void TickIntensity()
    {
        if (_enemies.Count == 0)
        {
            SetIntensity(_intensity - intensityDecayRate * Time.deltaTime);
            return;
        }

        if (_intensity < passiveRampCeiling)
            SetIntensity(_intensity + passiveIntensityRamp * Time.deltaTime);
    }

    private void SyncBudgetToIntensity()
    {
        int newMax = MaxBudget;
        if (newMax > _currentBudget + GetTotalHeldTokens())
        {
            int delta = newMax - (_currentBudget + GetTotalHeldTokens());
            _currentBudget += delta;
            if (delta > 0) OnBudgetChanged?.Invoke(_currentBudget);
        }
    }

    private int GetTotalHeldTokens()
    {
        int total = 0;
        foreach (var kvp in _heldTokens) total += kvp.Value;
        return total;
    }



#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUI.color = Color.black;
        GUI.Label(new Rect(11, 41, 301, 21), $"Intensity: {_intensity:F2}  Budget: {_currentBudget}/{MaxBudget}  Enemies: {_enemies.Count}");
        GUI.color = new Color(1f, 0.6f, 0f);
        GUI.Label(new Rect(10, 40, 300, 20), $"Intensity: {_intensity:F2}  Budget: {_currentBudget}/{MaxBudget}  Enemies: {_enemies.Count}");
        GUI.color = Color.white;
    }
#endif
}
