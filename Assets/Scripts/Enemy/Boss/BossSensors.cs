using UnityEngine;

public class BossSensors : MonoBehaviour
{
    public float DistanceToTarget { get; private set; }
    public Vector3 DirectionToTarget { get; private set; }

    public void UpdateSensors(IPlayerState target)
    {
        if (target == null) return;

        // Calculate a flat vector to the player (ignoring vertical height differences)
        Vector3 targetPos = target.GetPosition();
        targetPos.y = transform.position.y; 
        
        Vector3 offset = targetPos - transform.position;
        
        DistanceToTarget = offset.magnitude;
        
        // Prevent DivideByZero errors if they are occupying the exact same mathematical point
        DirectionToTarget = DistanceToTarget > 0.01f ? offset.normalized : transform.forward;
    }
}