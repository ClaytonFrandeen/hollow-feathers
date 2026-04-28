using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float damage = 15f;

    private void OnTriggerEnter(Collider other)
    {
        // Don't collide with the boss that shot it
        if (other.gameObject.name.Contains("Boss") || other.GetComponentInParent<BossStateBrain>() != null)
            return;

        // Ignore non-player triggers
        if (other.isTrigger && !other.CompareTag("Player")) return;

        // Robust player detection
        bool isPlayer = other.CompareTag("Player");
        if (!isPlayer)
        {
            var mcCheck = other.GetComponentInParent<Character.MovementController>();
            if (mcCheck == null) mcCheck = other.transform.root.GetComponentInChildren<Character.MovementController>();
            isPlayer = (mcCheck != null);
        }

        if (isPlayer)
        {
            // Find IDamageable exhaustively
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null && other.attachedRigidbody != null) damageable = other.attachedRigidbody.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) damageable = other.transform.root.GetComponentInChildren<IDamageable>();

            if (damageable != null) damageable.TakeDamage(damage, transform.position);

            // No knockback on regular projectiles — precise shot state handles its own
            Destroy(gameObject);
            return;
        }

        // Hit geometry — destroy
        if (!other.isTrigger) Destroy(gameObject);
    }
}
