using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Battle UI")]
    [SerializeField] private GameObject battleUI;
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
        if (icon != null) playerIcon.sprite = icon;
    }
    public void SetMaxHealth(float max)
    {
        maxHealthValue.text = max.ToString();
        healthBar.maxValue = max;
    }
    public void SetCurrentHealth(float health)
    {
        if (health <= healthBar.maxValue)
        {
            healthValue.text = health.ToString();
            healthBar.value = health;
        }
        else
        {
            healthValue.text = healthBar.maxValue.ToString();
            healthBar.value = healthBar.maxValue;
        }
    }
    public void SetMaxEnergy(float max)
    {
        maxEnergyValue.text = max.ToString();
        energyBar.maxValue = max;
    }
    public void SetCurrentEnergy(float energy)
    {
        if (energy <= energyBar.maxValue)
        {
            energyValue.text = energy.ToString();
            energyBar.value = energy;
        }
        else
        {
            energyValue.text = energyBar.maxValue.ToString();
            energyBar.value = energyBar.maxValue;
        }
        
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
        if (energy <= energySliderTeam.maxValue)
            energySliderTeam.value = energy;
        else energySliderTeam.value = energySliderTeam.maxValue;
    }
    public void SetTeammateIcon(Sprite icon)
    {
        if (icon != null) iconTeam.sprite = icon;
    }
}