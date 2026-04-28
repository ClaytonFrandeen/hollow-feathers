using UnityEngine;
using System.Collections.Generic;

public class State_ArenaReposition : BossState
{
    private enum Phase { JumpingUp, Firing, JumpingDown }
    private Phase currentPhase = Phase.JumpingUp;

    private float timer = 0f;
    private Vector3 jumpStartPos;
    private Vector3 jumpTargetPos;
    
    // Jump specific
    private float floatCurrentDuration = 1.2f;
    private float floatPeakHeight = 6.0f;
    
    // Fire specific
    private bool ringFired = false;
    private float fireDelayTimer = 0f;

    public State_ArenaReposition(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        brain.SetCooldown(this.GetType(), 20.0f);
        brain.desiredMovementDirection = Vector3.zero;
        
        // Disable NavMesh entirely so we can leap anywhere without snapping/getting stuck
        UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        brain.isInArenaSequence = true;

        if (brain.arenaWaypoints == null || brain.arenaWaypoints.waypoints == null || brain.arenaWaypoints.waypoints.Length == 0)
        {
            Debug.LogError("[State_ArenaReposition] No waypoints baked!");
            brain.ChangeState(new State_Idle(brain));
            return;
        }

        // ALWAYS pick a High point to jump to, if possible.
        List<BossArenaWaypoints.WaypointData> highPoints = brain.arenaWaypoints.GetPointsByType(BossArenaWaypoints.WaypointType.High);
        
        BossArenaWaypoints.WaypointData chosenPoint;
        if (highPoints.Count > 0)
        {
            // Pick a high point basically at random, maybe prefer one further from player
            chosenPoint = highPoints[Random.Range(0, highPoints.Count)];
            if (brain.currentTarget != null && Vector3.Distance(chosenPoint.position, brain.currentTarget.GetPosition()) < 5f)
            {
                chosenPoint = highPoints[Random.Range(0, highPoints.Count)]; // Re-roll once if too close
            }
        }
        else
        {
            // Fallback to random point
            chosenPoint = brain.arenaWaypoints.waypoints[Random.Range(0, brain.arenaWaypoints.waypoints.Length)];
        }

        currentPhase = Phase.JumpingUp;
        jumpStartPos = brain.transform.position;
        jumpTargetPos = chosenPoint.position;
        timer = 0f;
        
        Debug.Log($"[Arena Reposition] Phase 1: Leaping to High Point ({chosenPoint.pointName})");
    }

    public override void Tick()
    {
        brain.desiredMovementDirection = Vector3.zero;

        if (currentPhase == Phase.JumpingUp)
        {
            HandleJumpTick();
            if (timer >= floatCurrentDuration)
            {
                // Arrived at high point
                currentPhase = Phase.Firing;
                ringFired = false;
                fireDelayTimer = 0f;
                Debug.Log("[Arena Reposition] Phase 2: Firing Ring Drop");
            }
        }
        else if (currentPhase == Phase.Firing)
        {
            HandleFiringTick();
        }
        else if (currentPhase == Phase.JumpingDown)
        {
            HandleJumpTick();
            if (timer >= floatCurrentDuration)
            {
                // Arrived at center, sequence done
                Debug.Log("[Arena Reposition] Done. Back to center.");
                brain.ChooseNextState();
            }
        }
    }

    private void HandleJumpTick()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / floatCurrentDuration);

        Vector3 currentBasePos = Vector3.Lerp(jumpStartPos, jumpTargetPos, t);
        float jumpHeight = 4f * floatPeakHeight * t * (1f - t);

        brain.transform.position = new Vector3(currentBasePos.x, currentBasePos.y + jumpHeight, currentBasePos.z);

        // Turn to face target position while jumping
        Vector3 moveDir = jumpTargetPos - jumpStartPos;
        moveDir.y = 0;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(moveDir.normalized),
                Time.deltaTime * 6f
            );
        }
    }

    private void HandleFiringTick()
    {
        // Face player while on the rock
        if (brain.currentTarget != null)
        {
            Vector3 dir = brain.currentTarget.GetPosition() - brain.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 5f);
        }

        fireDelayTimer += Time.deltaTime;

        if (!ringFired && fireDelayTimer >= 0.6f)
        {
            FireRingDrop();
            ringFired = true;
        }

        // Wait a bit for the ring to rain down before leaping back
        if (ringFired && fireDelayTimer >= 2.8f)
        {
            currentPhase = Phase.JumpingDown;
            timer = 0f;
            jumpStartPos = brain.transform.position;
            
            // Re-find the center waypoint
            jumpTargetPos = GetCenterPoint();
            floatCurrentDuration = 1.0f; // Jump back down is faster
            floatPeakHeight = 3.0f; // Doesn't need to be as high
            Debug.Log("[Arena Reposition] Phase 3: Leaping back to center");
        }
    }

    private Vector3 GetCenterPoint()
    {
        if (brain.arenaWaypoints == null) return new Vector3(0, 0, 0);
        foreach (var wp in brain.arenaWaypoints.waypoints)
        {
            if (wp.type == BossArenaWaypoints.WaypointType.Center) return wp.position;
        }
        return new Vector3(0, 0, 0); // fallback origin
    }

    private void FireRingDrop()
    {
        if (brain.currentTarget == null) return;
        Vector3 playerPos = brain.currentTarget.GetPosition();
        
        // Boss is already on high ground, so just launch from slightly above it
        Vector3 launchPos = brain.transform.position + Vector3.up * 3f;

        float g = Mathf.Abs(Physics.gravity.y);
        int ringCount = 14; 
        float ringRadius = 7f;

        // Outer ring — tightening circle
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (360f / ringCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            float thisRadius = ringRadius * Mathf.Lerp(1.2f, 0.4f, i / (float)(ringCount - 1));

            Vector3 target = playerPos + new Vector3(
                Mathf.Cos(rad) * thisRadius, 0f,
                Mathf.Sin(rad) * thisRadius);

            float flightTime = Random.Range(1.2f, 2.0f);
            SpawnOrb(launchPos, target, flightTime, 1.4f, g, 25f);
        }

        // Center cluster — player must move
        for (int i = 0; i < 4; i++)
        {
            Vector3 center = playerPos + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            SpawnOrb(launchPos, center, Random.Range(1.5f, 2.2f), 1.2f, g, 30f);
        }
    }

    private void SpawnOrb(Vector3 from, Vector3 to, float flightTime, float scale, float g, float damage)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.localScale = Vector3.one * scale;
        orb.transform.position = from;

        Renderer rd = orb.GetComponent<Renderer>();
        if (rd != null) rd.material.color = new Color(0.35f, 0.25f, 0.12f); // Dark rocky brown

        Collider col = orb.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        rb.useGravity = true;

        BossProjectile proj = orb.AddComponent<BossProjectile>();
        proj.damage = damage;

        Vector3 diff = to - from;
        Vector3 vXZ = new Vector3(diff.x, 0f, diff.z) / flightTime;
        float vY = (diff.y + 0.5f * g * flightTime * flightTime) / flightTime;
        rb.linearVelocity = new Vector3(vXZ.x, vY, vXZ.z);

        GameObject.Destroy(orb, 12f);
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
        brain.isInArenaSequence = false;
        
        // Re-enable NavMesh ONLY when we are explicitly finished and back down at the center.
        UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = true;
    }

    public override Color GetStateColor() { return new Color(0f, 0.9f, 0.4f); }
    public override string GetStateName() 
    { 
        if (currentPhase == Phase.JumpingUp) return "LEAPING HIGH";
        if (currentPhase == Phase.Firing) return "RING DROP BARRAGE";
        return "RETURNING TO CENTER";
    }
}