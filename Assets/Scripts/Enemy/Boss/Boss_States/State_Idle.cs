using UnityEngine;

public class State_Idle : BossState
{
    private float idleTimer = 0f;
    private float timeUntilNextAction;
    private float idleDuration = 1.0f;
    private float previousDistance = Mathf.Infinity;
    private bool facePlayer = true;
    private bool isHardIdle = false;

    public State_Idle(BossStateBrain brain, float idleDuration = 1.0f, bool facePlayer = true, bool isHardIdle = false) : base(brain)
    {
        this.idleDuration = idleDuration;
        this.facePlayer = facePlayer;
        this.isHardIdle = isHardIdle;
    }

    public override void Enter()
    {
        idleTimer = 0f;
        timeUntilNextAction = idleDuration;
        
        brain.PlayAnimation("boss_idle");
    }

    public override void Tick()
    {
        if (brain.currentTarget == null || !brain.currentTarget.IsAlive())
            return;

        // Track the player with head/rotation but do not move
        if (facePlayer && brain.currentTarget != null)
        {
            Vector3 lookDir = brain.currentTarget.GetPosition() - brain.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                brain.transform.rotation = Quaternion.Slerp(
                    brain.transform.rotation,
                    Quaternion.LookRotation(lookDir.normalized),
                    Time.deltaTime * 5f);
            }
        }

        float currentDistance = brain.sensors.DistanceToTarget;


        idleTimer += Time.deltaTime;
        previousDistance = currentDistance;

        if (idleTimer >= timeUntilNextAction)
        {
            brain.ChooseNextState();
        }
    }

    public override Color GetStateColor() { return Color.yellow; }
    public override string GetStateName() { return isHardIdle ? "HARD IDLE (RECOVERING)" : "Idling... plotting..."; }

    public override void Exit()
    {
    }
}
