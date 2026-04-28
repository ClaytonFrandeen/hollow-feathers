using UnityEngine;

public class State_WavePushBack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 1.5f;
    private float actionDuration = 0f;

    private int totalFlaps = 0;
    private int flapsExecuted = 0;
    private float flapInterval = 0.6f;

    private Character.MovementController cachedMC = null;

    public State_WavePushBack(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stateTimer = 0f;
        flapsExecuted = 0;
        brain.desiredMovementDirection = Vector3.zero;

        totalFlaps = Random.Range(3, 6);
        actionDuration = totalFlaps * flapInterval;

        cachedMC = FindMovementController();
        if (cachedMC == null)
            Debug.LogWarning("[State_WavePushBack] Could NOT find MovementController at Enter! Will retry each flap.");
        else
            Debug.Log("[State_WavePushBack] MovementController FOUND on: " + cachedMC.gameObject.name);

        Debug.Log($"[State_WavePushBack] Preparing {totalFlaps} sequential wave pushbacks.");
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

    public override void Tick()
    {
        stateTimer += Time.deltaTime;
        brain.desiredMovementDirection = Vector3.zero;

        if (stateTimer < windupDuration)
        {
            // WINDUP
        }
        else if (stateTimer < windupDuration + actionDuration)
        {
            float actionTimer = stateTimer - windupDuration;
            if (flapsExecuted < totalFlaps && actionTimer >= flapsExecuted * flapInterval)
            {
                // Trigger the animation - the AnimationEvent will call OnAnimationEvent
                brain.PlayAnimation("boss_flap", 0.1f);
                flapsExecuted++;
            }
        }
        else
        {
            brain.ChooseNextState();
        }
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnFlap")
        {
            ExecuteFlap(flapsExecuted - 1); // Use the index of the flap that just started
        }
    }

    private void ExecuteFlap(int currentFlapIndex)
    {
        if (brain.currentTarget == null) return;

        if (cachedMC == null) cachedMC = FindMovementController();

        if (cachedMC == null)
        {
            Debug.LogWarning("[State_WavePushBack] STILL cannot find Character.MovementController. Knockback skipped.");
            return;
        }

        Vector3 playerPos = brain.currentTarget.GetPosition();
        Vector3 bossPos = brain.transform.position;
        Vector3 pushDir = (playerPos - bossPos);
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude > 0.01f) pushDir.Normalize();

        float pushForce;
        float stunTime;

        if (currentFlapIndex == 0)
        {
            pushForce = 42f;   // 45 m/s horizontal push
            stunTime = 0.5f;
        }
        else if (currentFlapIndex == 1)
        {
            pushForce = 70f;   // 70 m/s mega horizontal launch
            stunTime = 1.2f;
        }
        else
        {
            pushForce = 32f;   // 35 m/s follow-up
            stunTime = 0.4f;
        }

        // Pure horizontal push — zero the Y entirely
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.01f) pushDir = -brain.transform.forward;
        pushDir = pushDir.normalized;

        cachedMC.ApplyKnockback(pushDir * pushForce, stunTime);
        Debug.Log($"[State_WavePushBack] Flap {currentFlapIndex + 1}/{totalFlaps} KNOCKBACK APPLIED force={pushForce}.");
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
        brain.SetCooldown(this.GetType(), 8.0f);
    }

    public override Color GetStateColor()
    {
        return stateTimer < windupDuration ? Color.white : Color.cyan;
    }

    public override string GetStateName()
    {
        return stateTimer < windupDuration ? "WAVE WINDUP" : "WAVE PUSHBACK ACTION";
    }
}
