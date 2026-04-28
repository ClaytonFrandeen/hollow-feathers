using UnityEngine;

public class State_WingAttack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 2.0f;
    private float actionDuration = 1.5f;
    
    private bool actionStarted = false;
    private bool isFastWindup = false;

    public State_WingAttack(BossStateBrain brain, bool isFastWindup = false) : base(brain) 
    { 
        this.isFastWindup = isFastWindup;
    }

    public override void Enter()
    {
        stateTimer = 0f;
        actionStarted = false;
        brain.isAttackingFlag = true;
        brain.desiredMovementDirection = Vector3.zero;

        if (isFastWindup)
        {
            windupDuration = 0.7f; // Faster windup if triggered by hit counter interrupt
        }

        brain.PlayAnimation("boss_windUp");
    }

    public override void Tick()
    {
        stateTimer += Time.deltaTime;
        brain.desiredMovementDirection = Vector3.zero;

        if (stateTimer < windupDuration)
        {
            // --- WINDUP PHASE ---
            if (brain.currentTarget != null)
            {
                Vector3 targetPos = brain.currentTarget.GetPosition();
                targetPos.y = brain.transform.position.y;
                
                Vector3 directionToPlayer = (targetPos - brain.transform.position).normalized;
                if (directionToPlayer != Vector3.zero)
                {
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(directionToPlayer), Time.deltaTime * 3f);
                }
            }
        }
        else if (stateTimer < windupDuration + actionDuration)
        {
            // --- ACTION PHASE ---
            if (!actionStarted)
            {
                actionStarted = true;
                if (brain.bossWeapon != null) brain.bossWeapon.EnableHitbox();
                brain.PlayAnimation("boss_wingAttack", 0.1f);
                Debug.Log("[State_WingAttack] Action phase started. Hitboxes active.");
            }
        }
        // Removed timer-based exit; now waiting for OnActionComplete
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnActionComplete")
        {
            brain.ChooseNextState();
        }
    }

    public override void Exit()
    {
        brain.isAttackingFlag = false;
        if (brain.bossWeapon != null) brain.bossWeapon.DisableHitbox();
        brain.SetCooldown(this.GetType(), 6.0f);
    }

    public override Color GetStateColor() 
    { 
        return stateTimer < windupDuration ? Color.red : new Color(0.5f, 0f, 0f); 
    }
    
    public override string GetStateName() 
    { 
        return stateTimer < windupDuration ? "WING SLASH WINDUP" : "WING SLASH ACTION!"; 
    }
}
