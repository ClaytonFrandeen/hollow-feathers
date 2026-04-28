using UnityEngine;

public class State_PreciseProjectileAttack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 3.0f;
    private float actionDuration = 0.5f;
    private bool hasFired = false;

    public State_PreciseProjectileAttack(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stateTimer = 0f;
        hasFired = false;
        brain.desiredMovementDirection = Vector3.zero;
        brain.SetCooldown(this.GetType(), 10.0f);
        brain.PlayAnimation("boss_windUp");
        Debug.Log("[State_PreciseProjectileAttack] Extreme sniper preparation begun.");
    }

    public override void Tick()
    {
        stateTimer += Time.deltaTime;
        brain.desiredMovementDirection = Vector3.zero;

        if (stateTimer < windupDuration)
        {
            // --- WINDUP PHASE (EXTREME TRACKING) ---
            if (brain.currentTarget != null)
            {
                Vector3 targetPos = brain.currentTarget.GetPosition();
                targetPos.y = brain.transform.position.y;
                Vector3 directionToPlayer = (targetPos - brain.transform.position).normalized;
                
                if (directionToPlayer != Vector3.zero)
                {
                    // Track perfectly, no smoothing lerp (or very fast)
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(directionToPlayer), Time.deltaTime * 20f);
                }
            }
        }
        else if (stateTimer < windupDuration + actionDuration)
        {
            // --- ACTION PHASE ---
            if (!hasFired)
            {
                hasFired = true;
                brain.PlayAnimation("boss_preciseProjectile", 0.1f);
            }
        }
        // Removed actionDuration timer exit; now handled by OnActionComplete event
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnFire")
        {
            FirePreciseShot();
            PunishHuggers();
        }
        else if (eventName == "OnActionComplete")
        {
            brain.ChooseNextState();
        }
    }

    private void FirePreciseShot()
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.localScale = new Vector3(1.5f, 1.5f, 4.0f); // Massive precise javelin/projectile
        
        Renderer rd = orb.GetComponent<Renderer>();
        if (rd != null) rd.material.color = Color.blue;

        Vector3 spawnPos = brain.headTransform != null ? 
            brain.headTransform.position + brain.headTransform.forward * 1.5f : 
            brain.transform.position + brain.transform.forward * 2f + Vector3.up * 1.5f;

        orb.transform.position = spawnPos;

        // Perfectly aim at current exact coordinate
        Vector3 targetPos = brain.currentTarget.GetPosition() + Vector3.up * 0.5f; // Aiming at lower center
        Vector3 directionToPlayer = (targetPos - spawnPos).normalized;
        orb.transform.rotation = Quaternion.LookRotation(directionToPlayer);

        Collider col = orb.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        rb.useGravity = false;
        
        BossProjectile proj = orb.AddComponent<BossProjectile>();
        proj.damage = 45f; // Heavy damage for precision shot
        // Knockback for precise shot is applied in PunishHuggers via MovementController directly

        float extremeSpeed = 65f;
        rb.AddForce(directionToPlayer * extremeSpeed, ForceMode.Impulse);

        Object.Destroy(orb, 30f);
    }

    private void PunishHuggers()
    {
        Collider[] hits = Physics.OverlapSphere(brain.transform.position, 4.0f);
        foreach (Collider hit in hits)
        {
            if (hit.isTrigger && !hit.CompareTag("Player")) continue;

            // Robust player detection
            bool isPlayer = hit.CompareTag("Player");
            if (!isPlayer)
            {
                Character.MovementController pmCheck = hit.GetComponentInParent<Character.MovementController>();
                if (pmCheck == null) pmCheck = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                isPlayer = (pmCheck != null);
            }

            if (isPlayer)
            {
                Character.MovementController pm = hit.GetComponentInParent<Character.MovementController>();
                if (pm == null) pm = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                if (pm == null) pm = Object.FindFirstObjectByType<Character.MovementController>();
                
                if (pm != null)
                {
                    Vector3 pushDir = hit.transform.position - brain.transform.position;
                    pushDir.y = 0f;  // Pure horizontal
                    pushDir = pushDir.normalized;
                    pm.ApplyKnockback(pushDir * 80f, 0.6f);  // 40 m/s horizontal push
                }

                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg == null && hit.attachedRigidbody != null) dmg = hit.attachedRigidbody.GetComponent<IDamageable>();
                if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg == null) dmg = hit.transform.root.GetComponentInChildren<IDamageable>();
                if (dmg != null) dmg.TakeDamage(10f, hit.transform.position);

                Debug.Log("[State_PreciseProjectileAttack] Punished player for hugging during shot!");
                break;
            }
        }
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
    }

    public override Color GetStateColor() 
    { 
        return stateTimer < windupDuration ? Color.blue : new Color(0f, 0f, 0.5f); 
    }
    
    public override string GetStateName() 
    { 
        return stateTimer < windupDuration ? "LOCKING PRECISION TARGET" : "PRECISE FIRE!"; 
    }
}
