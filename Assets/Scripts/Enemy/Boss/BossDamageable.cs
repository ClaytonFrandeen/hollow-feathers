using System;
using UnityEngine;

public class BossDamageable : MonoBehaviour, IDamageable
{
    public float maxHealth = 130f; // Lowered slightly per user request for longer combat sequences

    // Shown in Inspector so you can watch health drop during play
    public float currentHealth;

    [SerializeField] private GameObject bloodEffectPrefab;

    [Header("Debug")]
    [Tooltip("Tick in Play Mode to instantly kill the boss and fire OnBossDeath.")]
    [SerializeField] private bool debugKillBoss = false;

    private float lastDamageTime = -1f;

    public event Action OnBossDeath;
    public event Action OnTakeDamage;
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (debugKillBoss)
        {
            debugKillBoss = false;
            Debug.Log("[BossDamageable] debugKillBoss triggered — calling Die().");
            Die();
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (IsDead()) return;

        // I-frame check (short, snappier for fast swing registration)
        if (Time.time - lastDamageTime < 0.05f) return;
        lastDamageTime = Time.time;

        currentHealth -= damage;

        if (bloodEffectPrefab != null)
            Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity);

        Debug.Log($"[Boss Health] Took {damage}. HP: {currentHealth}");

        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    private void Die()
    {
        Debug.Log("[Boss Health] Boss defeated!");
        OnBossDeath?.Invoke();
    }
}