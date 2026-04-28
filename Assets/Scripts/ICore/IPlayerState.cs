using UnityEngine;

public interface IPlayerState
{
    Vector3 GetPosition();
    bool IsAlive();
    // later, can add things like: bool IsHealing(), float GetCurrentStamina(), etc.
}