using UnityEngine;

public class State_HeadSmashAttack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 2.0f;
    private float actionDuration = 0.4f;
    private bool actionExecuted = false;

    // Shockwave state (mid-long range)
    private bool isShockwaveMode = false;
    private bool shockwaveLaunched = false;
    private float shockwaveExitTimer = 0f;
    private const float CLOSE_RANGE_THRESHOLD = 10f;
    private const float SHOCKWAVE_EXIST_TIME = 2.5f; // How long the cube travels before state ends
    private Vector3 shockwaveDirection;

    public State_HeadSmashAttack(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stateTimer = 0f;
        actionExecuted = false;
        shockwaveLaunched = false;
        shockwaveExitTimer = 0f;
        brain.desiredMovementDirection = Vector3.zero;
        brain.headSmashUsageCount++;

        if (brain.currentTarget != null)
        {
            float dist = Vector3.Distance(brain.transform.position, brain.currentTarget.GetPosition());
            isShockwaveMode = dist > CLOSE_RANGE_THRESHOLD;

            shockwaveDirection = brain.currentTarget.GetPosition() - brain.transform.position;
            shockwaveDirection.y = 0f;
            shockwaveDirection.Normalize();
        }
        else
        {
            isShockwaveMode = false;
            shockwaveDirection = brain.transform.forward;
        }
        
        brain.PlayAnimation("boss_windUp");

        Debug.Log($"[HeadSmash] Mode: {(isShockwaveMode ? "GROUND SHOCKWAVE" : "CLOSE SMASH")}");
    }

    public override void Tick()
    {
        stateTimer += Time.deltaTime;
        brain.desiredMovementDirection = Vector3.zero;

        // WINDUP: track player rotation
        if (stateTimer < windupDuration)
        {
            if (brain.currentTarget != null)
            {
                Vector3 targetPos = brain.currentTarget.GetPosition();
                targetPos.y = brain.transform.position.y;
                Vector3 dir = (targetPos - brain.transform.position).normalized;
                if (dir != Vector3.zero)
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            }
            return;
        }

        // ACTION
        if (!actionExecuted)
        {
            actionExecuted = true;
            brain.PlayAnimation("boss_headSmash", 0.1f);
        }

        if (isShockwaveMode)
        {
            shockwaveExitTimer += Time.deltaTime;
            if (shockwaveExitTimer >= SHOCKWAVE_EXIST_TIME)
                brain.ChooseNextState();
        }
        // Removed close-range timer exit; now handled by OnActionComplete event
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnSmash")
        {
            if (isShockwaveMode)
                LaunchGroundShockwave();
            else
                ExecuteCloseSmash();
        }
        else if (eventName == "OnActionComplete" && !isShockwaveMode)
        {
            brain.ChooseNextState();
        }
    }

    // -------------------------------------------------------
    // GROUND SHOCKWAVE — a long flat cube that slides across
    // the ground from the boss toward the player. Anything it
    // touches takes damage and gets pushed back.
    // -------------------------------------------------------
    private void LaunchGroundShockwave()
    {
        // Spawn a wide flat cube at the boss's feet
        Vector3 spawnPos = brain.transform.position + shockwaveDirection * 1.5f;
        spawnPos.y = brain.transform.position.y; // Ground level

        GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Wide and flat — looks like a shockwave slab
        shockwave.transform.localScale = new Vector3(4f, 0.6f, 2.5f);
        shockwave.transform.position = spawnPos;

        // Orient the long axis (Z) toward the player
        shockwave.transform.rotation = Quaternion.LookRotation(shockwaveDirection);

        // Hot glowing color
        Renderer rd = shockwave.GetComponent<Renderer>();
        if (rd != null) rd.material.color = new Color(1f, 0.45f, 0f); // Orange glow

        // Make it a trigger so it doesn't physically block movement
        Collider col = shockwave.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        ShockwaveSlab slab = shockwave.AddComponent<ShockwaveSlab>();
        slab.Initialize(shockwaveDirection, 22f, 32f, 20f);

        Debug.Log($"[HeadSmash] Ground shockwave launched toward {shockwaveDirection}");
    }

    // -------------------------------------------------------
    // CLOSE SMASH — melee range punch with AoE knockback
    // -------------------------------------------------------
    private Character.MovementController FindMovementController()
    {
        if (brain.currentTarget == null) return null;
        MonoBehaviour targetMB = brain.currentTarget as MonoBehaviour;
        if (targetMB != null)
        {
            var mc = targetMB.GetComponent<Character.MovementController>(); if (mc != null) return mc;
            mc = targetMB.GetComponentInParent<Character.MovementController>(); if (mc != null) return mc;
            mc = targetMB.transform.root.GetComponentInChildren<Character.MovementController>(); if (mc != null) return mc;
        }
        return Object.FindFirstObjectByType<Character.MovementController>();
    }

    private void ExecuteCloseSmash()
    {
        Vector3 smashPoint = brain.transform.position + brain.transform.forward * 2.0f;
        Collider[] hits = Physics.OverlapSphere(smashPoint, 3.0f);

        IDamageable playerDamageable = null;
        Vector3 hitPos = Vector3.zero;

        foreach (Collider hit in hits)
        {
            if (hit.isTrigger && !hit.CompareTag("Player")) continue;

            bool isPlayer = hit.CompareTag("Player");
            if (!isPlayer)
            {
                var pmCheck = hit.GetComponentInParent<Character.MovementController>();
                if (pmCheck == null) pmCheck = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                isPlayer = (pmCheck != null);
            }

            if (isPlayer)
            {
                playerDamageable = hit.GetComponent<IDamageable>()
                    ?? hit.attachedRigidbody?.GetComponent<IDamageable>()
                    ?? hit.GetComponentInParent<IDamageable>()
                    ?? hit.transform.root.GetComponentInChildren<IDamageable>();
                if (playerDamageable != null) { hitPos = hit.transform.position; break; }
            }
        }

        if (playerDamageable != null)
        {
            playerDamageable.TakeDamage(40f, hitPos);
            Character.MovementController mc = FindMovementController();
            if (brain.headSmashUsageCount > 3 && mc != null)
            {
                Vector3 backDir = -brain.transform.forward; backDir.y = 0f;
                mc.ApplyKnockback(backDir.normalized * 140f, 1.5f);
                brain.headSmashUsageCount = 0;
                Debug.Log("[HeadSmash] THROW!");
            }
            else if (mc != null)
            {
                Vector3 fwd = brain.transform.forward; fwd.y = 0f;
                mc.ApplyKnockback(fwd.normalized * 95f, 1.0f);
                Debug.Log("[HeadSmash] KNOCKBACK");
            }
        }
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
        brain.SetCooldown(this.GetType(), 7.0f);
    }

    public override Color GetStateColor()
    {
        if (stateTimer < windupDuration) return new Color(1f, 0.4f, 0.7f);
        return isShockwaveMode ? new Color(1f, 0.4f, 0f) : new Color(0.5f, 0.2f, 0.35f);
    }

    public override string GetStateName()
    {
        if (stateTimer < windupDuration) return isShockwaveMode ? "CHARGING SHOCKWAVE" : "PREPARING SMASH";
        return isShockwaveMode ? "GROUND SHOCKWAVE!" : "HEAD SMASH!";
    }
}
