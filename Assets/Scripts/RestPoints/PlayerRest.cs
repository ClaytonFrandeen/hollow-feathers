using Character;
using UnityEngine;

using weapon;

public class PlayerRest : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private RestMenu RestUI;
    private IWeaponMelee currentWeapon;
    private GameObject currentWeaponObject;
    private bool isResting = false;

    public void StartResting(Transform restTransform)
{
    if (isResting) return;
    isResting = true;

    var controller = GetComponent<PlayerController>();
    var rb = GetComponent<Rigidbody>();
    var move = GetComponent<MovementController>();

    // Stop input and movement first
    move.Stop();

    // Make kinematic BEFORE repositioning so physics can't fight it
    rb.isKinematic = true;
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    // Now safely teleport via Transform (more reliable than rb.position when kinematic)
    transform.position = restTransform.position;
    transform.rotation = restTransform.rotation;

    currentWeapon = GetComponentInChildren<IWeaponMelee>();
    if (currentWeapon != null)
    {
        // Convert the interface reference to a Component to get the GameObject
        currentWeaponObject = (currentWeapon as MonoBehaviour).gameObject;
        currentWeaponObject.SetActive(false);
    }
    // Disable movement controller after repositioning
    move.enabled = false;

    // Set isResting AFTER position is set so OnAnimatorMove exits cleanly
    controller.isResting = true;
    animator.applyRootMotion = false;
    animator.SetFloat("Speed", 0f);
    animator.ResetTrigger("EndRest"); // clear stale triggers
    animator.SetTrigger("Rest");
}

    // Called via Animation Event at end of animation
    public void EndRest()
{
    var rb = GetComponent<Rigidbody>();
    var move = GetComponent<MovementController>();

    rb.isKinematic = false;
    rb.WakeUp();

    move.enabled = true;
    currentWeaponObject.SetActive(true);


    animator.SetTrigger("EndRest");
    animator.applyRootMotion = true; // Add this line

    var controller = GetComponent<PlayerController>();
    controller.isResting = false;
    isResting = false;
}
}