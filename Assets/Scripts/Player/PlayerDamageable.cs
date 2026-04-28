using UnityEngine;
using System.Collections;
using System;
using UnityEditor.Rendering;
using Unity.VisualScripting;

public class PlayerDamageable : MonoBehaviour, IDamageable
{
    private PlayerVitals vitals;
    //TODO: Add death and hit/stagger animations
    //[SerializeField] Animator animator; 
    [SerializeField] GameObject bloodEffectPrefab;
    [SerializeField] DeathScreen deathScreen;
    [SerializeField] PlayerRespawn playerRespawn;
    Team team;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
        team = GetComponent<Team>();
    }


    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        StartCoroutine(HitStop());
        vitals.TakeDamage(damage);
        if (bloodEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity, transform);
            Destroy(blood, 0.7f); // destroy after 2 seconds (adjust as needed)
        }

        if (vitals.currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }
     
    private IEnumerator HitStop(float duration = 0.08f) {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
    public bool IsDead()
    {
        return vitals.currentHealth <=0;
    }
    private IEnumerator Die()
    {
        deathScreen.ShowDeathScreen();
        
        yield return new WaitForSeconds(2f);

        PlayerRespawn();
    }
    
    void PlayerRespawn()
    {
        playerRespawn.Respawn();
    }
    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(5f); // adjust to animation length

        Destroy(gameObject);
    }
}