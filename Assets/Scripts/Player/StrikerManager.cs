using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(StrikerInputHandler))]
public class StrikerManager : MonoBehaviour
{
    [Header("Striker Slots  (Up / Right / Down / Left)")]
    [SerializeField] private StrikerData[] slots = new StrikerData[4];

    [Header("Settings")]
    [Tooltip("If true, LB calls the selected striker. If false, LB/RB call fixed slots " +
             "(LB = slot 0, RB = slot 1) regardless of D-pad selection.")]
    [SerializeField] private bool useSelectionMode = true;

    [Tooltip("Parent transform for spawned striker GameObjects. Leave null to use scene root.")]
    [SerializeField] private Transform strikerParent;

    public Action<int> OnSelectionChanged;

    public Action<int, float> OnCooldownChanged;

    public Action<int> OnStrikerCalled;

    private StrikerInputHandler _input;

    private StrikerController[] _instances = new StrikerController[4];
    private float[] _cooldownTimers = new float[4];
    private bool[] _isOnCooldown = new bool[4];

    private int _selectedSlot = 0;   // current D-pad selection
    private int _prevDpadSlot = -1;  // to detect D-pad changes without repeating


    private void Awake()
    {
        _input = GetComponent<StrikerInputHandler>();
        PrewarmPool();
    }

    private void Update()
    {
        HandleSelection();
        HandleActivation();
        TickCooldowns();
        _input.ConsumeFrameInputs();
    }



    private void PrewarmPool()
    {
        for (int i = 0; i < 4; i++)
        {
            if (slots[i] == null || slots[i].prefab == null) continue;

            GameObject go = Instantiate(
                slots[i].prefab,
                Vector3.zero,
                Quaternion.identity,
                strikerParent);

            go.SetActive(false);
            _instances[i] = go.GetComponent<StrikerController>();

            if (_instances[i] == null)
                Debug.LogWarning($"StrikerManager: prefab in slot {i} has no StrikerController.", this);
        }
    }


    private void HandleSelection()
    {
        int dpadSlot = _input.DpadToSlotIndex();

        if (dpadSlot != -1 && dpadSlot != _prevDpadSlot)
        {
            _selectedSlot = dpadSlot;
            _prevDpadSlot = dpadSlot;
            OnSelectionChanged?.Invoke(_selectedSlot);
            Debug.Log(_selectedSlot);
        }
        else if (dpadSlot == -1)
        {
            _prevDpadSlot = -1;   // reset so next push registers
        }
    }


    private void HandleActivation()
    {
        if (!_input.AnyCallPressed) return;

        if (useSelectionMode)
        {
            // LB or RB both call the currently selected striker
            if (_input.LBPressed || _input.RBPressed)
            { 
                TryCallStriker(_selectedSlot);
                Debug.Log(_selectedSlot);
            }
        }
        else
        {
            // Direct mode: LB = slot 0, RB = slot 1
            if (_input.LBPressed) TryCallStriker(0);
            if (_input.RBPressed) TryCallStriker(1);
        }
    }

    private void TryCallStriker(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        if (slots[slotIndex] == null) return;
        if (_instances[slotIndex] == null) return;
        if (_isOnCooldown[slotIndex])
        {
            // put a sound effect here later
            return;
        }

        StrikerData data = slots[slotIndex];

        _instances[slotIndex].Activate(data, onComplete: () =>
        {
            
            StartCooldown(slotIndex, data.cooldown);
        });

        OnStrikerCalled?.Invoke(slotIndex);
    }



    private void StartCooldown(int slotIndex, float duration)
    {
        _isOnCooldown[slotIndex] = true;
        _cooldownTimers[slotIndex] = duration;
    }

    private void TickCooldowns()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!_isOnCooldown[i]) continue;

            _cooldownTimers[i] -= Time.deltaTime;

            
            float originalCooldown = slots[i] != null ? slots[i].cooldown : 1f;
            float fraction = 1f - Mathf.Clamp01(_cooldownTimers[i] / originalCooldown);
            OnCooldownChanged?.Invoke(i, fraction);

            if (_cooldownTimers[i] <= 0f)
            {
                _isOnCooldown[i] = false;
                OnCooldownChanged?.Invoke(i, 1f);   
            }
        }
    }


    public bool IsReady(int slot) => !_isOnCooldown[slot] && slots[slot] != null;
    public float CooldownFraction(int slot) => _isOnCooldown[slot]
        ? 1f - Mathf.Clamp01(_cooldownTimers[slot] / slots[slot].cooldown)
        : 1f;
    public int SelectedSlot => _selectedSlot;
    public StrikerData GetSlotData(int slot) => slot >= 0 && slot < 4 ? slots[slot] : null;

    public void ResetAllCooldowns()
    {
        for (int i = 0; i < 4; i++)
        {
            _isOnCooldown[i] = false;
            _cooldownTimers[i] = 0f;
            OnCooldownChanged?.Invoke(i, 1f);
        }
    }
}
