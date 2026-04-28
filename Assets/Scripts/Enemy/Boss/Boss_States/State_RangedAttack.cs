using UnityEngine;
using System.Collections.Generic;

public class State_RangedAttack : BossState
{
    private float stateTimer = 0f;
    private float nextShotTimer = 0f;
    private int targetShots = 0;
    private int shotsFired = 0;

    private bool isIntense = false;
    private float shotScale = 1.0f;

    // For the cinematic ring drop
    private bool ringFired = false;

    public State_RangedAttack(BossStateBrain brain, bool isIntense = false) : base(brain)
    {
        this.isIntense = isIntense;
    }

    public override void Enter()
    {
        brain.SetCooldown(this.GetType(), 8.0f);
        brain.desiredMovementDirection = Vector3.zero;
        Rigidbody rb = brain.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        brain.PlayAnimation("boss_windUp");

        shotsFired = 0;
        ringFired = false;

        if (isIntense)
        {
            targetShots = 1; // One cinematic ring drop — handled specially
            shotScale = 1.0f;
            nextShotTimer = Time.time + 1.2f; // Dramatic pause before drop
        }
        else
        {
            targetShots = Random.Range(3, 6);
            shotScale = 1.0f;
            nextShotTimer = Time.time + Random.Range(0.2f, 0.5f);
        }

        stateTimer = 0f;

        if (brain.currentTarget != null)
        {
            Vector3 directionToPlayer = (brain.currentTarget.GetPosition() - brain.transform.position);
            directionToPlayer.y = 0;
            if (directionToPlayer.sqrMagnitude > 0.0001f)
                brain.transform.rotation = Quaternion.LookRotation(directionToPlayer.normalized);
        }

        Debug.Log($"[State_RangedAttack] Mode: {(isIntense ? "RING DROP" : "Straight")} | Firing {targetShots} projectiles.");
    }

    public override void Tick()
    {
        brain.desiredMovementDirection = Vector3.zero;

        if (brain.currentTarget == null || !brain.currentTarget.IsAlive())
        {
            brain.ChangeState(new State_Idle(brain, 1.5f));
            return;
        }

        stateTimer += Time.deltaTime;

        // Keep facing player
        Vector3 lookDir = brain.currentTarget.GetPosition() - brain.transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(lookDir.normalized),
                Time.deltaTime * 10f);
        }

        if (isIntense)
        {
            // CINEMATIC RING DROP — all fire at once after the pause
            if (!ringFired && Time.time >= nextShotTimer)
            {
                ringFired = true;
                brain.PlayAnimation("boss_projectile", 0.1f);
                shotsFired = 1;
                nextShotTimer = Time.time + 2.5f; // Wait for boulders to land before ending
            }

            if (ringFired && Time.time >= nextShotTimer)
            {
                brain.ChooseNextState();
            }
        }
        else
        {
            // Sequential straight shots
            if (Time.time >= nextShotTimer && shotsFired < targetShots)
            {
                brain.PlayAnimation("boss_projectile", 0.1f);
                shotsFired++;
                nextShotTimer = Time.time + Random.Range(0.35f, 0.6f);
            }

            if (shotsFired >= targetShots)
            {
                // Removed timer-based exit; transition now handled by OnActionComplete
            }
        }
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnFire")
        {
            if (isIntense)
                FireRingDrop();
            else
                FireStraightShot();
        }
        else if (eventName == "OnActionComplete")
        {
            // Only transition out if we've fired all our intended shots
            if (shotsFired >= targetShots)
            {
                if (brain.isInArenaSequence)
                    brain.ChangeState(new State_ReturnToCenter(brain));
                else
                    brain.ChooseNextState();
            }
        }
    }

    // --- CINEMATIC RING DROP ---
    // Fires a ring of boulders arranged in a circle around the player's position,
    // all launched from high above with staggered flight times to create a dramatic tightening effect.
    private void FireRingDrop()
    {
        if (brain.currentTarget == null) return;

        Vector3 playerPos = brain.currentTarget.GetPosition();
        Vector3 launchHeight = brain.transform.position + Vector3.up * 12f;

        int ringCount = 12; // 12 boulders in a full ring
        float ringRadius = 8f; // Drop ring radius around player

        float g = Mathf.Abs(Physics.gravity.y);

        for (int i = 0; i < ringCount; i++)
        {
            float angle = (360f / ringCount) * i;
            float rad = angle * Mathf.Deg2Rad;

            // Ring positions at various radii — tighter toward center for the last few
            float thisRadius = ringRadius * (1f - (i / (float)ringCount) * 0.3f);

            Vector3 target = playerPos + new Vector3(
                Mathf.Cos(rad) * thisRadius,
                0f,
                Mathf.Sin(rad) * thisRadius
            );

            // Vary flight time slightly for a cascading rain effect
            float flightTime = Random.Range(1.0f, 1.8f);

            SpawnParabolicOrb(launchHeight, target, flightTime, 1.4f, g);
        }

        // Also fire 3 direct center shots with shorter flight so the player HAS to move
        for (int i = 0; i < 3; i++)
        {
            Vector3 centerTarget = playerPos + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            float flightTime = Random.Range(1.4f, 1.9f);
            SpawnParabolicOrb(launchHeight, centerTarget, flightTime, 1.2f, g);
        }

        Debug.Log("[State_RangedAttack] RING DROP fired!");
    }

    private void SpawnParabolicOrb(Vector3 spawnPos, Vector3 target, float flightTime, float scale, float g)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.localScale = Vector3.one * scale;
        orb.transform.position = spawnPos;

        Renderer rd = orb.GetComponent<Renderer>();
        if (rd != null) rd.material.color = new Color(0.35f, 0.25f, 0.12f); // Dark rocky brown

        Collider col = orb.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        BossProjectile proj = orb.AddComponent<BossProjectile>();
        proj.damage = 25f;

        rb.useGravity = true;

        Vector3 diff = target - spawnPos;
        Vector3 velocityXZ = new Vector3(diff.x, 0, diff.z) / flightTime;
        float velocityY = (diff.y + 0.5f * g * flightTime * flightTime) / flightTime;

        rb.linearVelocity = new Vector3(velocityXZ.x, velocityY, velocityXZ.z);

        GameObject.Destroy(orb, 10f);
    }

    // --- STRAIGHT SHOT ---
    private void FireStraightShot()
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        float scale = 1.0f * shotScale;
        orb.transform.localScale = Vector3.one * scale;

        Vector3 spawnPos;
        if (brain.headTransform != null)
            spawnPos = brain.headTransform.position + brain.headTransform.forward * 0.5f;
        else
            spawnPos = brain.transform.position + brain.transform.forward * 1.5f + Vector3.up * 1.5f;

        orb.transform.position = spawnPos;

        Renderer rd = orb.GetComponent<Renderer>();
        if (rd != null) rd.material.color = new Color(0.4f, 0.3f, 0.2f);

        Collider col = orb.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        BossProjectile proj = orb.AddComponent<BossProjectile>();
        proj.damage = 20f;

        rb.useGravity = false;

        Vector3 targetBodyPos = brain.currentTarget.GetPosition() + Vector3.up * 1.0f;
        Vector3 directionToPlayer = (targetBodyPos - spawnPos).normalized;
        rb.linearVelocity = directionToPlayer * 22f;

        GameObject.Destroy(orb, 15f);
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
    }

    public override Color GetStateColor() { return isIntense ? new Color(1f, 0.5f, 0f) : Color.yellow; }
    public override string GetStateName() { return isIntense ? "RING DROP BARRAGE" : "STRAIGHT SHOTS"; }
}