using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Battle UI")]
    [SerializeField] private GameObject battleUI;
    [SerializeField] private Button pauseButton;
    [Header("Progress")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressValue;
    [Header("Player")]
    [SerializeField] private Image playerIcon;
    [SerializeField] private List<TeammateUI> teammates = new List<TeammateUI>();
    [SerializeField] private TMP_Text healthValue;
    [SerializeField] private TMP_Text maxHealthValue;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text energyValue;
    [SerializeField] private TMP_Text maxEnergyValue;
    [SerializeField] private Slider energyBar;

    public void Awake()
    {
        progressSlider.maxValue = 100f;
    }

    //Methods for modifying values of UI elements
    public void SetPlayerIcon (Sprite icon)
    {
        playerIcon.sprite = icon;
    }
    public void SetMaxHealth(float max)
    {
        maxHealthValue.text = max.ToString();
        healthBar.maxValue = max;
    }
    public void SetCurrentHealth(float health)
    {
        healthValue.text = health.ToString();
        healthBar.value = health;
    }
    public void SetMaxEnergy(float max)
    {
        maxEnergyValue.text = max.ToString();
        energyBar.maxValue = max;
    }
    public void SetEnergyHealth(float energy)
    {
        energyValue.text = energy.ToString();
        energyBar.value = energy;
    }
}

[System.Serializable]
public class TeammateUI
{
    [SerializeField] private Slider energySliderTeam;
    [SerializeField] private Image iconTeam;

    public void SetMaxEnergy(float maxEnergy)
    {
        energySliderTeam.maxValue = maxEnergy;
    }
    public void SetCurrentEnergy(float energy)
    {
        energySliderTeam.value = energy;
    }
    public void SetTeammateIcon(Sprite icon)
    {
        iconTeam.sprite = icon;
    }
}