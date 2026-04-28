using UnityEngine;

public class State_DashAttack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 2.5f;
    private float actionDuration = 2.0f; // Long dash so boss rockets far past the player

    private Vector3 dashDirection;
    private bool hasDealtDamage = false;
    private float maxDashSpeed = 85f; // Extreme speed

    public State_DashAttack(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stateTimer = 0f;
        hasDealtDamage = false;
        brain.SetCooldown(this.GetType(), 6.0f);

        // Calculate and lock dash direction immediately
        if (brain.currentTarget != null)
        {
            Vector3 targetPos = brain.currentTarget.GetPosition();
            targetPos.y = brain.transform.position.y;
            dashDirection = (targetPos - brain.transform.position).normalized;
        }
        else
        {
            dashDirection = brain.transform.forward;
        }

        brain.PlayAnimation("boss_windUp");
    }

    public override void Tick()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer < windupDuration)
        {
            // --- WINDUP PHASE ---
            brain.desiredMovementDirection = Vector3.zero;
        }
        else if (stateTimer < windupDuration + actionDuration)
        {
            // --- ACTION PHASE ---
            if (stateTimer - Time.deltaTime < windupDuration)
            {
                brain.PlayAnimation("boss_dash", 0.05f);
            }
            
            // Extreme sliding speed
            brain.desiredMovementDirection = dashDirection * maxDashSpeed;
            
            if (brain.bossWeapon != null) brain.bossWeapon.EnableHitbox();

            // Override guaranteed damage check for player
            if (!hasDealtDamage)
            {
                Collider[] hits = Physics.OverlapSphere(brain.transform.position, 3.5f);
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
                        IDamageable dmg = hit.GetComponent<IDamageable>();
                        if (dmg == null && hit.attachedRigidbody != null) dmg = hit.attachedRigidbody.GetComponent<IDamageable>();
                        if (dmg == null) dmg = hit.GetComponentInParent<IDamageable>();
                        if (dmg == null) dmg = hit.transform.root.GetComponentInChildren<IDamageable>();
                        
                        if (dmg != null)
                        {
                            dmg.TakeDamage(35f, hit.transform.position);
                            hasDealtDamage = true;

                            // KNOCKBACK on dash hit
                            Character.MovementController pm = hit.GetComponentInParent<Character.MovementController>();
                            if (pm == null) pm = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                            if (pm == null) pm = Object.FindFirstObjectByType<Character.MovementController>();
                            if (pm != null)
                            {
                                Vector3 pushDir = dashDirection;
                                pushDir.y = 0f;
                                pushDir = pushDir.normalized;
                                pm.ApplyKnockback(pushDir * 125f, 1.2f);
                                Debug.Log("[State_DashAttack] KNOCKBACK APPLIED!");
                            }
                            // NO early cutoff — boss continues dashing past the player
                        }
                    }
                }
            }
        }
        else
        {
            // End of dash
            brain.ChangeState(new State_Idle(brain, 2.0f, false));
        }
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
        if (brain.bossWeapon != null) brain.bossWeapon.DisableHitbox();
        brain.SetCooldown(this.GetType(), 8.0f);
    }

    public override Color GetStateColor() 
    { 
        return stateTimer < windupDuration ? Color.magenta : new Color(0.5f, 0f, 0.5f); 
    }
    
    public override string GetStateName() 
    { 
        return stateTimer < windupDuration ? "PREPARING DASH..." : "DASHING!"; 
    }
}