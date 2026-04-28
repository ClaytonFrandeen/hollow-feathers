using UnityEngine;

public class State_ReturnToSpawn : BossState
{
    private float idleAtSpawnTimer = 0f;
    private float idleAtSpawnDuration = 3.0f;
    private float walkSpeed = 3.5f;

    public State_ReturnToSpawn(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        idleAtSpawnTimer = 0f;
        brain.desiredMovementDirection = Vector3.zero;
        brain.isRetrievingPlayer = false;
        brain.PlayAnimation("walking");
        Debug.Log("[State_ReturnToSpawn] Returning to spawn after out-of-arena sequence.");
    }

    public override void Tick()
    {
        if (brain.currentTarget == null || !brain.currentTarget.IsAlive())
            return;

        if (brain.isPlayerInArena)
        {
            brain.ChangeState(new State_Idle(brain, 1.0f));
            return;
        }

        Vector3 toSpawn = brain.originalPosition - brain.transform.position;
        toSpawn.y = 0f;
        float dist = toSpawn.magnitude;

        if (dist > 0.25f)
        {
            Vector3 moveDir = toSpawn.normalized;
            brain.desiredMovementDirection = moveDir * walkSpeed;

            // Face the movement direction
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(moveDir),
                Time.deltaTime * 8f);

            idleAtSpawnTimer = 0f; // reset if still walking
            return;
        }

        // At spawn position, idle and power down if player still away
        if (brain.desiredMovementDirection != Vector3.zero)
        {
            brain.PlayAnimation("boss_idle");
        }
        brain.desiredMovementDirection = Vector3.zero;
        idleAtSpawnTimer += Time.deltaTime;

        if (idleAtSpawnTimer >= idleAtSpawnDuration)
        {
            if (!brain.isPlayerInArena)
            {
                var controller = brain.GetComponent<BossController>();
                if (controller != null)
                {
                    Debug.Log("[State_ReturnToSpawn] Player still out of arena. Powering down.");
                    controller.PowerDown();
                }
            }
            else
            {
                brain.ChangeState(new State_Idle(brain, 1.0f));
            }
        }
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
    }

    public override Color GetStateColor() { return Color.green; }
    public override string GetStateName() { return "RETURN TO SPAWN"; }
}
