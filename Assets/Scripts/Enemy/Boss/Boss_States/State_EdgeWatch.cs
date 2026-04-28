using UnityEngine;

public class State_EdgeWatch : BossState
{
    private float stalkTimer = 0f;
    private const float totalStalkDuration = 5.0f;   // REDUCED from 10 to 5 seconds
    private const float grappleWindowStart = 1.5f;   // REDUCED from 3 to 1.5 seconds
    private const float grappleRange = 15.0f;
    private bool isGrappling = false;
    private float grappleTimer = 0f;
    private LineRenderer grappleLine = null;
    private Vector3 grappleStartPos;
    private Vector3 grappleTargetPos;

    public State_EdgeWatch(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stalkTimer = 0f;
        isGrappling = false;
        grappleTimer = 0f;
        grappleLine = null;
        brain.desiredMovementDirection = Vector3.zero;
        Debug.Log("[State_EdgeWatch] Player left arena, entering 5-second stalk...");
    }

    public override void Tick()
    {
        if (brain.currentTarget == null || !brain.currentTarget.IsAlive())
        {
            brain.ChangeState(new State_Idle(brain, 1.0f));
            return;
        }

        // If player returns to arena during stalk, interrupt
        if (brain.isPlayerInArena)
        {
            Debug.Log("[State_EdgeWatch] Player returned to arena, resuming combat.");
            brain.ChangeState(new State_Idle(brain, 1.0f));
            return;
        }

        stalkTimer += Time.deltaTime;

        // 5-second timeout - absolute enforcement
        if (stalkTimer >= totalStalkDuration)
        {
            Debug.Log("[State_EdgeWatch] 5-second stalk complete. Returning to spawn.");
            brain.ChangeState(new State_ReturnToSpawn(brain));
            return;
        }

        // Lock rotation on Y-axis toward player
        Vector3 lookDir = brain.currentTarget.GetPosition() - brain.transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(lookDir.normalized),
                Time.deltaTime * 6f);
        }

        // Check for grapple trigger
        if (!isGrappling && stalkTimer > grappleWindowStart)
        {
            Vector3 bossFlat = brain.transform.position;
            bossFlat.y = 0f;
            Vector3 playerFlat = brain.currentTarget.GetPosition();
            playerFlat.y = 0f;
            float playerDistance = Vector3.Distance(playerFlat, bossFlat);

            if (playerDistance < grappleRange)
            {
                isGrappling = true;
                grappleTimer = 0f;
                grappleStartPos = brain.transform.position;
                grappleTargetPos = brain.currentTarget.GetPosition();

                UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                
                // Instantiate LineRenderer
                grappleLine = brain.gameObject.AddComponent<LineRenderer>();
                grappleLine.startWidth = 0.2f;
                grappleLine.endWidth = 0.2f;
                grappleLine.positionCount = 2;
                
                Debug.Log("[State_EdgeWatch] Grapple triggered! Executing internal grapple sequence.");
            }
        }

        // Execute grapple sequence
        if (isGrappling)
        {
            grappleTimer += Time.deltaTime;
            
            if (grappleTimer <= 0.2f)
            {
                // 0.0s - 0.2s: Set grappleLine positions from Boss to Player
                grappleLine.SetPosition(0, brain.transform.position);
                grappleLine.SetPosition(1, grappleTargetPos);
                brain.desiredMovementDirection = Vector3.zero;
            }
            else if (grappleTimer <= 0.4f)
            {
                // 0.2s - 0.4s: Lerp to player's coordinates
                float t = (grappleTimer - 0.2f) / 0.2f;
                brain.transform.position = Vector3.Lerp(grappleStartPos, grappleTargetPos, t);
                grappleLine.SetPosition(0, brain.transform.position);
                grappleLine.SetPosition(1, grappleTargetPos);
            }
            else if (grappleTimer <= 1.5f)
            {
                // 0.4s - 1.5s: Hold position, deal damage if connected
                brain.desiredMovementDirection = Vector3.zero;
                grappleLine.SetPosition(0, brain.transform.position);
                grappleLine.SetPosition(1, grappleTargetPos);
                
                // Check for connection and apply damage
                if (Vector3.Distance(brain.transform.position, grappleTargetPos) < 1f)
                {
                    Character.MovementController mc = ((MonoBehaviour)brain.currentTarget).GetComponent<Character.MovementController>();
                    if (mc == null) mc = ((MonoBehaviour)brain.currentTarget).GetComponentInParent<Character.MovementController>();
                    if (mc == null) mc = ((MonoBehaviour)brain.currentTarget).transform.root.GetComponentInChildren<Character.MovementController>();
                    if (mc != null)
                    {
                        Vector3 knockbackDir = (grappleTargetPos - brain.transform.position).normalized;
                        knockbackDir.y = 0.2f;
                        mc.ApplyKnockback(knockbackDir * 15f, 0.5f);

                        IDamageable dmg = mc.GetComponent<IDamageable>();
                        if (dmg == null) dmg = mc.GetComponentInChildren<IDamageable>();
                        if (dmg == null) dmg = mc.GetComponentInParent<IDamageable>();
                        if (dmg == null) dmg = mc.transform.root.GetComponentInChildren<IDamageable>();
                        if (dmg != null) dmg.TakeDamage(20f, mc.transform.position);

                        Debug.Log("[State_EdgeWatch] Grapple connected! Applied knockback and damage.");
                    }
                }
            }
            else
            {
                // 1.5s+: End grapple
                if (grappleLine != null)
                {
                    Object.Destroy(grappleLine);
                    grappleLine = null;
                }
                
                UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = true;

                isGrappling = false;
                stalkTimer = totalStalkDuration; // Force exit on next frame
                Debug.Log("[State_EdgeWatch] Grapple sequence complete.");
            }
        }
        else
        {
            // Remain idle (no movement) when not grappling
            brain.desiredMovementDirection = Vector3.zero;
        }
    }

    public override void Exit()
    {
        if (grappleLine != null)
        {
            Object.Destroy(grappleLine);
            grappleLine = null;
        }
        UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        brain.desiredMovementDirection = Vector3.zero;
    }

    public override Color GetStateColor() { return Color.magenta; }
    public override string GetStateName() { return "EDGE WATCH (STALKING)"; }
}
