using UnityEngine;

public class ParabolicEgg : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float journeyTime = 2.0f;
    private float flightTimer = 0f;
    private float peakHeight = 15f;

    public void Initialize(Vector3 start, Vector3 target)
    {
        startPos = start;
        targetPos = target;
        flightTimer = 0f;
    }

    void Update()
    {
        flightTimer += Time.deltaTime;
        float t = Mathf.Clamp01(flightTimer / journeyTime);

        Vector3 currentBasePos = Vector3.Lerp(startPos, targetPos, t);
        float jumpHeight = 4f * peakHeight * t * (1f - t);

        transform.position = new Vector3(currentBasePos.x, currentBasePos.y + jumpHeight, currentBasePos.z);

        if (t >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f);
        foreach (Collider hit in hits)
        {
            if (hit.isTrigger && !hit.CompareTag("Player")) continue;

            bool isPlayer = hit.CompareTag("Player");
            if (!isPlayer)
            {
                var mcCheck = hit.GetComponentInParent<Character.MovementController>();
                if (mcCheck == null) mcCheck = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                isPlayer = (mcCheck != null);
            }

            if (isPlayer)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg == null && hit.attachedRigidbody != null) dmg = hit.attachedRigidbody.GetComponent<IDamageable>();
                if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg == null) dmg = hit.transform.root.GetComponentInChildren<IDamageable>();

                if (dmg != null)
                {
                    dmg.TakeDamage(30f, hit.transform.position);
                    Debug.Log("[ParabolicEgg] Hit player for 30 damage!");
                }

                Character.MovementController mc = hit.GetComponentInParent<Character.MovementController>();
                if (mc == null) mc = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                if (mc == null) mc = Object.FindFirstObjectByType<Character.MovementController>();
                if (mc != null)
                {
                    Vector3 pushDir = hit.transform.position - transform.position;
                    pushDir.y = 0f;  // Pure horizontal blast
                    pushDir = pushDir.normalized;
                    mc.ApplyKnockback(pushDir * 100f, 1.0f);  // 50 m/s horizontal blast
                }
            }
        }

        Destroy(gameObject);
    }
}
