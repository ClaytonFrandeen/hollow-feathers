using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public GameObject enemyPrefab;
    private GameObject currentEnemy;
    void Start()
    {
        EnemyManager.Instance.RegisterSpawnPoint(this);
        Spawn();
    }

    public void Spawn()
    {
        if (currentEnemy != null) return;
        
        currentEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
    }

    public void Despawn()
    {
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
            currentEnemy = null;
        }
    }

    public void ResetEnemy()
    {
        Despawn();
        Spawn();
    }
}
