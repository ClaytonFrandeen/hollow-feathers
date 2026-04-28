using UnityEngine;

public class SkeletonAnimationEvents : MonoBehaviour
{
    [SerializeField] private SkeletonFist fist;

    public void EnableHitbox()  { fist.EnableHitbox(); }
    public void DisableHitbox() { fist.DisableHitbox(); }
}