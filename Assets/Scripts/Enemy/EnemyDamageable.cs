using UnityEngine;
using System.Collections;
using System;
using Enemy;

public class EnemyDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData data;
    public float maxHealth {get; private set;}
    public float currentHealth {get; private set;}

    public event Action<float, float> OnHealthChanged;

    [SerializeField] Animator animator;
    [SerializeField] GameObject bloodEffectPrefab;
    public static event Action<int> OnEnemyDeath;
    public int currency;
    private void Awake()
    {
        maxHealth = data.GetBaseHealth();
        currentHealth = maxHealth;
        currency = 100;
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {   
        StartCoroutine(HitStop());
        animator.SetTrigger("Flinch");
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (bloodEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity, transform);
            Destroy(blood, 0.7f); // destroy after 2 seconds (adjust as needed)
        }
        //TODO: add hit animation
        Debug.Log($"Training Dummy hit for {damage}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    //Adds slight pause to make hits feel heavier
     private IEnumerator HitStop(float duration = 0.1f) {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
    public bool IsDead()
    {
        return currentHealth <=0;
    }
    private void Die()
    {   
        animator.SetTrigger("IsDead");
        StartCoroutine(DeathRoutine());
        //TODO: Add animations and loot drop
    }
    

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(5f); // adjust to animation length
        OnEnemyDeath?.Invoke(currency);
        Destroy(gameObject);
    }

}