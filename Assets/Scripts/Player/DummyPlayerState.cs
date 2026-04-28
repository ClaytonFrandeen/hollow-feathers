using UnityEngine;

public class DummyPlayerState : MonoBehaviour, IPlayerState
{
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public bool IsAlive()
    {
        return true; // Still immortal for testing purposes
    }
}