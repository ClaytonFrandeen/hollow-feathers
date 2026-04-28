using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
    private PlayerStatsManager stats;
    private PlayerInventory inventory;
    public float maxHealth {get; private set;}
    public float currentHealth{get; private set;}

    public float maxStamina{get; private set;}
    public float currentStamina{get; private set;}

    private float staminaRegenRate = 20f;
    private float staminaRegenDelay = 3f;
    private float lastStaminaUseTime;

    public event Action<float, float> OnHealthChanged;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        stats = GetComponent<PlayerStatsManager>();
        inventory = GetComponent<PlayerInventory>();
        RecalculateVitals(); //!!this MUST be moved here, otherwise updateMaxHP() under healthbar.cs will run first with a value of zero. Awake has greater priority than Start does.
    }

    private void OnEnable()
    {
        stats.attributes.OnAttributesChanged += RecalculateVitals;
    }

    private void OnDisable()
    {
        stats.attributes.OnAttributesChanged -= RecalculateVitals;
    }

    //private void Start()
    //{}
    private void Update()
    {
        RegenStamina();
    }

    public void RecalculateVitals()
    {
        maxHealth = stats.maxHealth;
        maxStamina = stats.maxStamina;

        //Debug.Log("vital maxHealth is " + maxHealth);

        currentHealth = maxHealth;
        currentStamina = maxStamina;

        //Debug.Log("vital current health is " + currentHealth);
    }

    private void RegenStamina ()
    {
        if (Time.time - lastStaminaUseTime < staminaRegenDelay) return; //stamina delay

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool TryUseStamina(float amount)
    {
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    public void Heal(float amount)
    {   
        if (inventory.healPotions <= 0) return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
