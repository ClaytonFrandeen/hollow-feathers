using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 spawnPosition;
    private Quaternion spawnRoatation;
    private void Start()
    {
        spawnPosition = transform.position;
        spawnRoatation = transform.rotation;
    }

    public void SetSpawnPoint(Transform newSpawn)
    {
        spawnPosition = newSpawn.position;
        spawnRoatation = newSpawn.rotation;
    }

    public void Respawn()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRoatation;

        GetComponent<PlayerVitals>().Heal(GetComponent<PlayerVitals>().maxHealth);
        GetComponent<PlayerInventory>().addCurrency(-GetComponent<PlayerInventory>().fragments);
        GetComponent<PlayerInventory>().ResetHealPotions();

        EnemyManager.Instance.ResetAllEnemies();
    }
}
