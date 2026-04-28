using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollingRockDamage : MonoBehaviour
{
    private Rigidbody rb;
    private float lifeTimer = 0f;
    private const float MAX_LIFETIME = 5.0f;

    private float maxDamage = 35f;
    private float maxVelocityExpected = 25f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= MAX_LIFETIME)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignore boss
        if (collision.gameObject.name.Contains("Boss") || collision.transform.GetComponentInParent<BossStateBrain>() != null)
            return;

        // Robust player detection via Character.MovementController
        bool isPlayer = collision.gameObject.CompareTag("Player");
        if (!isPlayer)
        {
            var mcCheck = collision.transform.GetComponentInParent<Character.MovementController>();
            if (mcCheck == null) mcCheck = collision.transform.root.GetComponentInChildren<Character.MovementController>();
            isPlayer = (mcCheck != null);
        }

        if (isPlayer)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            float damageModifier = Mathf.Clamp01(currentSpeed / maxVelocityExpected);
            float finalDamage = Mathf.Max(5f, maxDamage * damageModifier);

            // Exhaustive IDamageable search
            IDamageable dmg = collision.gameObject.GetComponent<IDamageable>();
            if (dmg == null && collision.rigidbody != null) dmg = collision.rigidbody.GetComponent<IDamageable>();
            if (dmg == null) dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if (dmg == null) dmg = collision.transform.root.GetComponentInChildren<IDamageable>();

            if (dmg != null)
            {
                dmg.TakeDamage(finalDamage, collision.contacts[0].point);
                Debug.Log($"[RollingRock] Hit player for {finalDamage} damage.");
            }

            // Knockback in the rock's rolling direction
            Character.MovementController mc = collision.transform.GetComponentInParent<Character.MovementController>();
            if (mc == null) mc = collision.transform.root.GetComponentInChildren<Character.MovementController>();
            if (mc == null) mc = Object.FindFirstObjectByType<Character.MovementController>();
            if (mc != null)
            {
                // Push horizontally in the rock's rolling direction
                Vector3 pushDir = rb.linearVelocity;
                pushDir.y = 0f;  // Pure horizontal
                if (pushDir.sqrMagnitude < 0.01f) pushDir = -Vector3.forward;
                pushDir = pushDir.normalized;
                float knockForce = Mathf.Lerp(55f, 110f, damageModifier);  // 30-60 m/s horizontal
                mc.ApplyKnockback(pushDir * knockForce, 0.7f);
            }

            Destroy(gameObject);
        }
    }
}
