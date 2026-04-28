using UnityEngine;
using System.Collections.Generic;

public class State_ReturnToCenter : BossState
{
    private Vector3 targetPos;
    private Vector3 jumpStartPos;
    private float jumpTimer = 0f;
    private float jumpDuration = 1.0f;
    private float jumpPeakHeight = 7.0f;

    public State_ReturnToCenter(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        jumpStartPos = brain.transform.position;
        jumpTimer = 0f;

        // Find the Center Point coordinates
        targetPos = brain.originalPosition; // Fallback
        
        if (brain.arenaWaypoints != null && brain.arenaWaypoints.waypoints != null)
        {
            var centers = brain.arenaWaypoints.GetPointsByType(BossArenaWaypoints.WaypointType.Center);
            if (centers.Count > 0)
            {
                targetPos = centers[0].position;
            }
        }

        Debug.Log($"[Arena Sequence] Sequence complete. Returning to Center Point at {targetPos}.");
    }

    public override void Tick()
    {
        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / jumpDuration);

        Vector3 currentBasePos = Vector3.Lerp(jumpStartPos, targetPos, t);
        float jumpHeight = 4f * jumpPeakHeight * t * (1f - t);

        brain.transform.position = new Vector3(currentBasePos.x, currentBasePos.y + jumpHeight, currentBasePos.z);

        // Face the center while jumping
        Vector3 moveDir = targetPos - jumpStartPos;
        moveDir.y = 0;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(moveDir.normalized),
                Time.deltaTime * 5f
            );
        }

        if (t >= 1.0f)
        {
            Debug.Log("[Arena Sequence] Reset complete. Entering Hard Idle.");
            brain.isInArenaSequence = false; // Sequence is over
            brain.ChangeState(new State_Idle(brain, 3.5f, true, true)); // Hard idle for 3.5 seconds
        }
    }

    public override void Exit()
    {
        brain.desiredMovementDirection = Vector3.zero;
        UnityEngine.AI.NavMeshAgent agent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = true;
    }

    public override Color GetStateColor() { return Color.white; }
    public override string GetStateName() { return "RETURNING TO CENTER"; }
}
