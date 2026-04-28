using UnityEngine;

public class GrapplingArm : MonoBehaviour
{
    private BossStateBrain boss;
    private IPlayerState target;
    private float duration = 2.5f;
    private float timer = 0f;
    private LineRenderer lineRenderer;

    public void Initialize(BossStateBrain bossBrain, IPlayerState targetPlayer)
    {
        boss = bossBrain;
        target = targetPlayer;

        // Optional: Add a line renderer to visualize the extending arm
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.startColor = Color.magenta;
        lineRenderer.endColor = Color.magenta;
    }

    void Update()
    {
        if (target == null || boss == null)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;

        Vector3 bossPos = boss.transform.position;
        Vector3 playerPos = target.GetPosition();
        Vector3 armDirection = (playerPos - bossPos).normalized;

        // Expand the arm toward the player
        float currentLength = Mathf.Lerp(0f, 30f, timer / duration);
        Vector3 armEndPos = bossPos + armDirection * currentLength;

        // Optional: Draw the arm line
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, bossPos + Vector3.up * 0.5f);
            lineRenderer.SetPosition(1, armEndPos + Vector3.up * 0.5f);
        }

        // If the arm reaches and wraps around the player, pull them back toward center
        if (Vector3.Distance(armEndPos, playerPos) < 2.0f && timer > 0.5f)
        {
            PullPlayerBack(playerPos);
        }

        // Destroy after duration
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void PullPlayerBack(Vector3 playerPos)
    {
        if (target == null || boss == null) return;

        // Calculate direction from player toward arena center
        Vector3 centerDir = (boss.originalPosition - playerPos).normalized;
        centerDir.y = 0f; // Keep horizontal

        // Apply launch toward center (stronger force for launch effect)
        Character.MovementController playerMove = (target as MonoBehaviour)?.GetComponent<Character.MovementController>();
        if (playerMove != null)
        {
            playerMove.ApplyKnockback(centerDir * 35f, 2.0f); // Increased force and duration for launch
            
            IDamageable dmg = playerMove.GetComponent<IDamageable>();
            if (dmg == null) dmg = playerMove.GetComponentInChildren<IDamageable>();
            if (dmg == null) dmg = playerMove.GetComponentInParent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(25f, playerMove.transform.position);

            Debug.Log("[Grappling Arm] Player launched toward arena center with heavy damage!");
        }
    }
}
