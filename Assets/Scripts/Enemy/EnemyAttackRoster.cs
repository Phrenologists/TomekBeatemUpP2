// ScriptableObject that holds an ordered list of EnemyAttackData assets
// for a single enemy type. EnemyAIBrain iterates this list when selecting
// an attack, so order matters, preffered attack should be put first.

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "BeatEmUp/EnemyAttackRoster")]
public class EnemyAttackRoster : ScriptableObject
{
    [Tooltip("Attacks in priority order. EnemyAIBrain picks the first usable one " +
             "unless randomSelection is enabled.")]
    public List<EnemyAttackData> attacks = new List<EnemyAttackData>();

    [Header("Selection Strategy")]
    [Tooltip("If true, a random usable attack is chosen instead of the first valid one. " +
             "Adds variety at the cost of predictability.")]
    public bool randomSelection = false;

    [Tooltip("If true, the same attack won't be chosen twice in a row.")]
    public bool preventRepeat = true;

    private int _lastUsedIndex = -1;


    public EnemyAttackData SelectAttack(
        float distanceX,
        float distanceDepth,
        int availableBudget,
        float currentIntensity,
        EnemyRole currentRole)
    {
        var candidates = new List<(EnemyAttackData attack, int index)>();

        for (int i = 0; i < attacks.Count; i++)
        {
            if (attacks[i] == null) continue;

            if (preventRepeat && i == _lastUsedIndex) continue;

            if (attacks[i].IsUsable(distanceX, distanceDepth,
                                    availableBudget, currentIntensity, currentRole))
            {
                candidates.Add((attacks[i], i));
            }
        }

        if (candidates.Count == 0 && preventRepeat)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                if (attacks[i] == null) continue;
                if (attacks[i].IsUsable(distanceX, distanceDepth,
                                        availableBudget, currentIntensity, currentRole))
                    candidates.Add((attacks[i], i));
            }
        }

        if (candidates.Count == 0) return null;

        int chosen = randomSelection
            ? Random.Range(0, candidates.Count)
            : 0;   // first = highest priority

        _lastUsedIndex = candidates[chosen].index;
        return candidates[chosen].attack;
    }

    public int MinTokenCost()
    {
        int min = int.MaxValue;
        foreach (var a in attacks)
            if (a != null && a.tokenCost < min) min = a.tokenCost;
        return min == int.MaxValue ? 1 : min;
    }
}
