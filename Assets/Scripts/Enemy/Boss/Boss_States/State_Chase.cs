using UnityEngine;

public class State_Chase : BossState
{
    private Transform bossTransform;
    private float chaseTimer = 0f;
    private float maxChaseTime = 12.0f;
    private float attackRange = 5.0f;

    private int totalPushbacks = 0;
    private const int MAX_PUSHBACKS = 3; // Exit after this many pushbacks

    private bool inPushPause = false;
    private float pushPauseTimer = 0f;

    private float currentSpeed = 6.0f;
    private float acceleration = 1.5f;
    private float maxChaseSpeed = 12.0f;

    public State_Chase(BossStateBrain brain) : base(brain)
    {
        bossTransform = brain.transform;
    }

    public override void Enter()
    {
        chaseTimer = 0f;
        totalPushbacks = 0;
        currentSpeed = 6.0f;
        brain.PlayAnimation("walking");
    }

    private Character.MovementController FindMovementController()
    {
        if (brain.currentTarget == null) return null;
        MonoBehaviour targetMB = brain.currentTarget as MonoBehaviour;
        if (targetMB != null)
        {
            var mc = targetMB.GetComponent<Character.MovementController>();
            if (mc != null) return mc;
            mc = targetMB.GetComponentInParent<Character.MovementController>();
            if (mc != null) return mc;
            mc = targetMB.transform.root.GetComponentInChildren<Character.MovementController>();
            if (mc != null) return mc;
        }
        return Object.FindFirstObjectByType<Character.MovementController>();
    }

    private IDamageable FindDamageableOnPlayer(Collider hit)
    {
        if (!hit.CompareTag("Player"))
        {
            var pmCheck = hit.GetComponentInParent<Character.MovementController>();
            if (pmCheck == null) pmCheck = hit.transform.root.GetComponentInChildren<Character.MovementController>();
            if (pmCheck == null) return null;
        }

        IDamageable dmg = hit.GetComponent<IDamageable>();
        if (dmg != null) return dmg;
        if (hit.attachedRigidbody != null) { dmg = hit.attachedRigidbody.GetComponent<IDamageable>(); if (dmg != null) return dmg; }
        dmg = hit.GetComponentInParent<IDamageable>();
        if (dmg != null) return dmg;
        return hit.transform.root.GetComponentInChildren<IDamageable>();
    }

    public override void Tick()
    {
        if (brain.currentTarget == null || !brain.currentTarget.IsAlive())
        {
            brain.ChangeState(new State_Idle(brain, 1.0f));
            return;
        }

        // Timer expiry — exit regardless of pushback count
        chaseTimer += Time.deltaTime;
        if (chaseTimer >= maxChaseTime)
        {
            brain.ChooseNextState();
            return;
        }

        // Push pause: boss briefly stops after each hit before resuming chase
        if (inPushPause)
        {
            pushPauseTimer -= Time.deltaTime;
            brain.desiredMovementDirection = Vector3.zero;
            if (pushPauseTimer <= 0f)
            {
                inPushPause = false;

                // After MAX_PUSHBACKS, leave the chase state
                if (totalPushbacks >= MAX_PUSHBACKS)
                {
                    brain.ChooseNextState();
                    return;
                }
                // Otherwise continue chasing
            }
            return;
        }

        float distance = brain.sensors.DistanceToTarget;
        Vector3 direction = brain.sensors.DirectionToTarget;

        // Always keep chasing — only push when in range
        if (distance <= attackRange)
        {
            ExecutePushback(direction);
        }
        else
        {
            // Keep accelerating toward player
            float oldSpeed = currentSpeed;
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxChaseSpeed);
            brain.desiredMovementDirection = direction * currentSpeed;

            // Transition from walking to running if speed crosses threshold
            if (oldSpeed < 9.0f && currentSpeed >= 9.0f)
            {
                brain.PlayAnimation("running");
            }
            else if (oldSpeed >= 9.0f && currentSpeed < 9.0f)
            {
                brain.PlayAnimation("walking");
            }
        }
    }

    private void ExecutePushback(Vector3 direction)
    {
        Character.MovementController mc = FindMovementController();

        // Sweep damage
        Collider[] hits = Physics.OverlapSphere(brain.transform.position, 4.0f);
        foreach (Collider hit in hits)
        {
            if (hit.isTrigger && !hit.CompareTag("Player")) continue;

            IDamageable dmg = FindDamageableOnPlayer(hit);
            if (dmg != null)
            {
                dmg.TakeDamage(15f, hit.transform.position);
                if (mc == null)
                {
                    mc = hit.GetComponentInParent<Character.MovementController>();
                    if (mc == null) mc = hit.transform.root.GetComponentInChildren<Character.MovementController>();
                }
                break;
            }
        }

        if (mc != null)
        {
            Vector3 pushVector = direction;
            pushVector.y = 0f;
            pushVector = pushVector.normalized;
            mc.ApplyKnockback(pushVector * 100f, 1.2f);
            Debug.Log($"[State_Chase] Push #{totalPushbacks + 1}/{MAX_PUSHBACKS}");
        }

        totalPushbacks++;
        inPushPause = true;
        pushPauseTimer = 0.8f; // Brief pause after each push before re-closing
        brain.desiredMovementDirection = Vector3.zero;
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
    }

    public override Color GetStateColor() { return Color.Lerp(Color.yellow, Color.red, 0.5f); }
    public override string GetStateName() { return $"CHASE [{totalPushbacks}/{MAX_PUSHBACKS}]"; }
}
