using UnityEngine;
using System.Collections.Generic;

// Attach this to the Boss's Weapon GameObject
[RequireComponent(typeof(Collider))]
public class BossWeaponHitbox : MonoBehaviour
{
    public float damageAmount = 15f;
    private Collider hitboxCollider;

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        if (hitboxCollider == null) return;
        hitTargets.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root == transform.root) return;

        if (other.isTrigger && !other.CompareTag("Player")) return;

        // Robust player detection via Character.MovementController
        bool isPlayer = other.CompareTag("Player");
        if (!isPlayer)
        {
            var mcCheck = other.GetComponentInParent<Character.MovementController>();
            if (mcCheck == null) mcCheck = other.transform.root.GetComponentInChildren<Character.MovementController>();
            isPlayer = (mcCheck != null);
        }

        if (!isPlayer) return;

        // Exhaustive IDamageable search
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null && other.attachedRigidbody != null) damageable = other.attachedRigidbody.GetComponent<IDamageable>();
        if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) damageable = other.transform.root.GetComponentInChildren<IDamageable>();

        if (damageable != null && !hitTargets.Contains(damageable))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            damageable.TakeDamage(damageAmount, hitPoint);
            hitTargets.Add(damageable);

            // Apply knockback
            Character.MovementController mc = other.GetComponentInParent<Character.MovementController>();
            if (mc == null) mc = other.transform.root.GetComponentInChildren<Character.MovementController>();
            if (mc == null) mc = Object.FindFirstObjectByType<Character.MovementController>();
            if (mc != null)
            {
                Vector3 pushDir = other.transform.position - transform.position;
                pushDir.y = 0f;  // Pure horizontal push
                pushDir = pushDir.normalized;
                mc.ApplyKnockback(pushDir * 87f, 0.8f);  // 45 m/s horizontal
            }

            Debug.Log($"[Boss Combat] Dealt {damageAmount} damage + knockback");
        }
    }
}