using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{   
    public static EnemyManager Instance;
    private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
    }
    public void RegisterSpawnPoint(EnemySpawnPoint sp)
    {
        spawnPoints.Add(sp);
    }

    // Update is called once per frame
    public void ResetAllEnemies()
    {
        foreach (var sp in spawnPoints)
        {
            sp.ResetEnemy();
        }
    }
}
