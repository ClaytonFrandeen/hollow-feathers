using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int maxHealPotions;
    public int healPotions;
    public int fragments;

    private void Start()
    {   
        maxHealPotions = 2;
        healPotions = 2;
        fragments = 0;
    }
    private void OnEnable()
    {
        EnemyDamageable.OnEnemyDeath += addCurrency;
    }

    private void OnDisable()
    {
        EnemyDamageable.OnEnemyDeath -= addCurrency;
    }

    public void addCurrency(int amount)
    {
        fragments += amount;
    }

    public void ResetHealPotions()
    {
        healPotions = maxHealPotions;
    }
}