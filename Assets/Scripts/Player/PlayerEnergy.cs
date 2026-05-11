using UnityEngine;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum energy. Exposed in case you want to scale it later.")]
    [SerializeField] private float maxEnergy = 100f;

    [Tooltip("Energy awarded per hit when the AttackData doesn't override it.")]
    [SerializeField] private float defaultEnergyPerHit = 10f;

    public System.Action<float, float> OnEnergyChanged;

    public float CurrentEnergy => _energy;
    public float MaxEnergy => maxEnergy;
    public float Fraction => _energy / maxEnergy;

    private float _energy = 0f;

    public void OnHitLanded(float gain = -1f)
    {
        float amount = gain < 0f ? defaultEnergyPerHit : gain;
        SetEnergy(_energy + amount);
    }


    public bool TrySpend(int cost)
    {
        if (cost <= 0) return true;
        if (_energy < cost) return false;

        SetEnergy(_energy - cost);
        return true;
    }


    public void SetEnergy(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxEnergy);
        if (Mathf.Approximately(clamped, _energy)) return;

        _energy = clamped;
        OnEnergyChanged?.Invoke(_energy, maxEnergy);
    }

    public void FillEnergy() => SetEnergy(maxEnergy);

    public void DrainEnergy() => SetEnergy(0f);
}
