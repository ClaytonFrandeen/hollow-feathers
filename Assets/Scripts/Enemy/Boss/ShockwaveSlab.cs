using UnityEngine;

public class ShockwaveSlab : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;
    private float maxDistance;
    private float damage;
    private float distanceTraveled = 0f;
    private bool initialized = false;

    public void Initialize(Vector3 direction, float speed, float travelDistance, float damageAmount)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        maxDistance = travelDistance;
        damage = damageAmount;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        float moveDist = moveSpeed * Time.deltaTime;
        transform.position += moveDirection * moveDist;
        distanceTraveled += moveDist;

        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore boss
        if (other.gameObject.name.Contains("Boss") || other.GetComponentInParent<BossStateBrain>() != null)
            return;

        // Ignore non-player triggers
        if (other.isTrigger && !other.CompareTag("Player")) return;

        bool isPlayer = other.CompareTag("Player");
        if (!isPlayer)
        {
            var mcCheck = other.GetComponentInParent<Character.MovementController>();
            if (mcCheck == null) mcCheck = other.transform.root.GetComponentInChildren<Character.MovementController>();
            isPlayer = (mcCheck != null);
        }

        if (isPlayer)
        {
            IDamageable damageable = other.GetComponent<IDamageable>()
                ?? other.attachedRigidbody?.GetComponent<IDamageable>()
                ?? other.GetComponentInParent<IDamageable>()
                ?? other.transform.root.GetComponentInChildren<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform.position);
            }

            Character.MovementController mc = other.GetComponentInParent<Character.MovementController>();
            if (mc == null) mc = other.transform.root.GetComponentInChildren<Character.MovementController>();
            if (mc == null) mc = Object.FindFirstObjectByType<Character.MovementController>();

            if (mc != null)
            {
                // Push back in the direction the slab is moving
                Vector3 pushDir = moveDirection;
                pushDir.y = 0f; // Pure horizontal
                pushDir = pushDir.normalized;
                mc.ApplyKnockback(pushDir * 70f, 0.8f); // Strong horizontal push
            }
        }
    }
}
