using System;
using Enemy;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyVitals : MonoBehaviour
{
    public float maxHealth {get; private set;}
    public float currentHealth{get; private set;}
    public float baseDamage;
    [SerializeField] public EnemyData enemyData;
    public event Action<float, float> OnEnemyHealthChanged;

    private void Awake()
    {
        maxHealth = enemyData.GetBaseHealth();
        currentHealth = maxHealth;
        baseDamage = enemyData.GetBaseDamage();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    //private void Start()
    //{}
    private void Update()
    {
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        OnEnemyHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
