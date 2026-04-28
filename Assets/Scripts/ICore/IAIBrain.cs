using UnityEngine;

public interface IAIBrain
{
    // Where does the AI want to go right now?
    Vector3 GetMovementDirection();
    
    // Does the AI want to attack right now?
    bool WantsToAttack();
    
    // What color (state) should I be in right now? 
    Color GetDebugColor();
}
