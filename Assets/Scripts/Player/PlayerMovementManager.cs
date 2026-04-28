using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float rotationSpeed = 10f;
    public float currentSpeed = 0f;

    [Header("Dodge")]
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.3f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float yVelocity = 0;
    public float groundedOffset = -2f;

    private CharacterController controller;
    private Animator animator;

    private Vector2 moveInput;
    private Vector3 moveDir;
    private bool isSprinting;
    private bool isDodging;
    private float dodgeTimer;

    // Knockback variables
    private Vector3 knockbackVelocity = Vector3.zero;
    private float knockbackTime = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.applyRootMotion = true;
    }

    void Update()
    {
        HandleMovement();
        HandleDodge();
        ApplyGravity();

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 finalMove =
        moveDir * speed +
        knockbackVelocity +
        Vector3.up * yVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    public void OnAnimatorMove()
    {
        if (isDodging)
        {
            Vector3 motion = animator.deltaPosition;
            motion.y = 0f;
            controller.Move(motion);

            transform.rotation = animator.rootRotation;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    public void OnDodge()
    {
        if (!isDodging)
        {
            isDodging = true;

            animator.SetTrigger("Dodge");
        }
    }

    public void EndDodge()
    {
        isDodging = false;
    }
    

    void HandleMovement()
    {
        if (isDodging || knockbackTime > 0)
        {
            if (knockbackTime > 0)
            {
                controller.Move(knockbackVelocity * Time.deltaTime);
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime / knockbackTime);
                knockbackTime -= Time.deltaTime;
            }
            return;
        }

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        if (input.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            moveDir = rotation * Vector3.forward;

            float targetSpeed = isSprinting ? 1f : 0.5f;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 10f * Time.deltaTime);
            animator.SetFloat("Speed", currentSpeed);
        }
        else
        {
            moveDir = Vector3.zero;
            animator.SetFloat("Speed", 0f);
        }

    }


    void HandleDodge()
    {
        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;

            //controller.Move(transform.forward * dodgeSpeed * Time.deltaTime);

            if (dodgeTimer <= 0f)
            {
                isDodging = false;
            }
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (yVelocity < 0)
            {
                yVelocity = -2f;
            }
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        Vector3 yMove = new Vector3(0, yVelocity, 0);
        controller.Move(yMove * Time.deltaTime);
    }

    public void ApplyKnockback(Vector3 force, float duration)
    {
        force.y = Mathf.Clamp(force.y, 0f, 2f);
        knockbackVelocity = force;
        knockbackTime = duration;
    }
}