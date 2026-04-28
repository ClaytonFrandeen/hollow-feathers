using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    public BossStateBrain bossBrain;
    public BossController bossToControl;

    void Start()
    {
        // Auto-wire boss refs so this works just by attaching the script.
        if (bossBrain == null)
            bossBrain = GetComponentInParent<BossStateBrain>();
        if (bossToControl == null)
            bossToControl = GetComponentInParent<BossController>();

        if (bossBrain == null)
            bossBrain = FindObjectOfType<BossStateBrain>();
        if (bossToControl == null)
            bossToControl = FindObjectOfType<BossController>();

        if (bossBrain == null)
            Debug.LogError("BossArenaTrigger: bossBrain not assigned and not found in parent hierarchy or scene.");
        if (bossToControl == null)
            Debug.LogError("BossArenaTrigger: bossToControl not assigned and not found in parent hierarchy or scene.");

        // Check if player is already inside the arena at game start
        Collider arenaCollider = GetComponent<Collider>();
        if (arenaCollider == null)
        {
            Debug.LogWarning("BossArenaTrigger: No trigger collider found on Boss_Fight_Zone.");
            return;
        }

        Vector3 checkCenter = transform.position;
        float checkRadius = 0f;

        if (arenaCollider is SphereCollider sphere)
        {
            checkRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        }
        else if (arenaCollider is BoxCollider box)
        {
            // Use the farthest half-extent as an equivalent sphere radius.
            checkRadius = box.bounds.extents.magnitude;
        }
        else
        {
            Debug.LogWarning("BossArenaTrigger: Collider is not BoxCollider or SphereCollider; using default radius 5.");
            checkRadius = 5f;
        }

        if (bossBrain == null)
        {
            Debug.LogError("BossArenaTrigger.Start: bossBrain reference is missing on Boss_Fight_Zone! Please assign one.");
            return;
        }

        // Robust player detection using IPlayerState and arena bounds, independent of Physics layers.
        Bounds arenaBounds = arenaCollider.bounds;
        int foundCount = 0;

        foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb is IPlayerState player)
            {
                Vector3 playerPos = player.GetPosition();
                if (arenaBounds.Contains(playerPos))
                {
                    foundCount++;
                    bossBrain.isPlayerInArena = true;
                    bossBrain.hasPlayerEverEnteredArena = true;

                    if (bossToControl != null)
                    {
                        bossToControl.WakeUp(player);
                    }
                    else
                    {
                        Debug.LogWarning("BossArenaTrigger.Start: bossToControl is missing, cannot call WakeUp.");
                    }

                    Debug.Log("BossArenaTrigger.Start: Player found inside arena at startup; boss is now awake.");
                    break;
                }
            }
        }

        Debug.Log($"BossArenaTrigger.Start: Found {foundCount} IPlayerState objects inside arena bounds at start.");

        if (!bossBrain.isPlayerInArena)
        {
            Debug.Log("BossArenaTrigger.Start: No player in arena at startup; boss will sleep until entry.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        IPlayerState player = other.GetComponent<IPlayerState>();
        if (player != null && bossBrain != null)
        {
            bossBrain.isPlayerInArena = true;
            if (!bossBrain.hasPlayerEverEnteredArena)
            {
                bossBrain.hasPlayerEverEnteredArena = true;
                bossToControl?.WakeUp(player);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        IPlayerState player = other.GetComponent<IPlayerState>();
        if (player != null && bossBrain != null)
        {
            if (!bossBrain.isPlayerInArena)
            {
                Debug.Log("BossArenaTrigger: Player detected in arena during OnTriggerStay.");
            }

            bossBrain.isPlayerInArena = true;
            if (!bossBrain.hasPlayerEverEnteredArena)
            {
                bossBrain.hasPlayerEverEnteredArena = true;
                bossToControl?.WakeUp(player);
                Debug.Log("BossArenaTrigger: Player first entry via OnTriggerStay, waking up boss.");
            }
            else if (!bossToControl.enabled)
            {
                // Player re-entered after PowerDown (post-grapple or other exit)
                bossToControl?.WakeUp(player);
                Debug.Log("BossArenaTrigger: Player re-entered arena, waking up boss from PowerDown.");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        IPlayerState player = other.GetComponent<IPlayerState>();
        if (player != null && bossBrain != null)
        {
            bossBrain.isPlayerInArena = false;
            Debug.Log("BossArenaTrigger: Player left arena.");
        }
    }
}
