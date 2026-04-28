using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(MovementController))]
    [RequireComponent(typeof(CombatController))]
    public class PlayerController : MonoBehaviour
    {
       
        private MovementController moveController;
        private CombatController combatController;

        [SerializeField] private PlayerVitals vitals;
        private Camera mainCamera;

        [SerializeField] private Animator animator;
        [SerializeField] private float dodgeSpeed = 5f;
        
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float cameraDistance = 5f;
        [SerializeField] private float cameraHeight = 2f;
        [SerializeField] private float cameraSensitivity = 100f;
        [SerializeField] private float rotationSmoothing = 10f;
        private float cameraYaw;
        private float cameraPitch;

        [SerializeField] private GameObject cameraTargetLocator;

        [Header("Targeting")]
        [SerializeField] private GameObject lookAtLocator;

        [Header("Player")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Rigidbody rb; 
        
        [Header("Combat Timing")]
        [SerializeField] private float heavyAttackThreshold = 0.4f;
        private float attackButtonTime;
        private bool isHoldingAttack;
        private bool heavyTriggered;
        private bool isAttacking;
        private bool isDodging;
        private float currentSpeed;
        public bool isResting;

        public void OnMove(InputValue inputValue)
        {
            if (isResting) return;
            _rawMoveInput = inputValue.Get<Vector2>();
        }

        private Vector2 _rawMoveInput;

        public void OnSprint(InputValue value)
        {
            moveController.SetSprinting(value.isPressed);
        }

        public void OnDodge()
        {
            if (isAttacking) return;
            if (!vitals.TryUseStamina(20f)) return;

            // Snap to input direction before dodging
            if (_rawMoveInput.sqrMagnitude > 0.1f)
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 dodgeDir = camForward * _rawMoveInput.y + camRight * _rawMoveInput.x;
                transform.rotation = Quaternion.LookRotation(dodgeDir);
            }

            if (moveController.TryDodge())
            {
                animator.SetTrigger("Dodge");
                isDodging = true;
            }
        }


        public void OnLock(InputValue inputValue)
        {
            lookAtLocator.GetComponent<LockOnTarget>().LockOn();
        }


        public void OnLightAttack(InputValue inputValue) {
            if (isDodging) return;
            if (!vitals.TryUseStamina(10f)) return;
            isAttacking = true;
            combatController.LightAttack();
        }
        public void OnHeavyAttack(InputValue inputValue) {
            if (isDodging) return;
            if (!vitals.TryUseStamina(20f)) return;
            isAttacking = true;
            combatController.HeavyAttack();
        }

      
        public void EndAttack()
        {
            isAttacking = false;
        }

  
        public void EndDodge()
        {
            moveController.EndDodge();
            isDodging = false;
        }


        void Start()
        {
            moveController = GetComponent<MovementController>();
            combatController = GetComponent<CombatController>();
            mainCamera = Camera.main;
            cameraYaw = cameraTransform.eulerAngles.y;
            rb.WakeUp();
        }

        void Update()
        {
            if (isResting) return;
            HandleCameraRelativeMovement();
            HandleHeavyAttackHold();
        }

        void LateUpdate()
        {   
            OrbitCamera();
        }


        public void OnAnimatorMove()
        {
            if (isResting) return;
            if (moveController.IsDodging)
            {
                Vector3 move = transform.forward * dodgeSpeed * Time.deltaTime;
                move.y = 0f;
                rb.MovePosition(rb.position + move);
            }
            else
            {
                // Apply root motion but strip vertical drift
                Vector3 delta = animator.deltaPosition;
                delta.y = 0f;
                rb.MovePosition(rb.position + delta);
                transform.rotation *= animator.deltaRotation;
            }
        }


        private void HandleCameraRelativeMovement()
        {
            if (isAttacking || moveController.IsDodging) return;

            // Also read right-stick for camera (gamepad)
            if (Gamepad.current != null)
            {
                Vector2 look = Gamepad.current.rightStick.ReadValue();
                if (look.sqrMagnitude > 0.01f)
                {
                    cameraYaw += look.x * cameraSensitivity * Time.deltaTime;
                    cameraPitch -= look.y * cameraSensitivity * Time.deltaTime;
                    cameraPitch = Mathf.Clamp(cameraPitch, -30f, 60f);
                }
            }

            Vector2 move = _rawMoveInput;

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * move.y + camRight * move.x;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                // Rotate player toward move direction
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRotation,
                    rotationSmoothing * Time.deltaTime);

                moveController.Move(moveDir.normalized * moveSpeed);

                // Animator blend (0.5 walk, 1.0 sprint)
                float target = moveController.IsSprinting ? 1f : 0.5f;
                currentSpeed = Mathf.Lerp(currentSpeed, target, 10f * Time.deltaTime);
            }
            else
            {
                moveController.Move(Vector3.zero);
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, 10f * Time.deltaTime);
            }

            animator.SetFloat("Speed", currentSpeed);
        }

        private void HandleHeavyAttackHold()
        {
            if (!isHoldingAttack || heavyTriggered || isAttacking) return;

            if (Time.time - attackButtonTime >= heavyAttackThreshold)
            {
                heavyTriggered = true;
                isAttacking = true;
                combatController.HeavyAttack();
            }
        }

        private void OrbitCamera()
        {
            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -cameraDistance);

            cameraTransform.position = transform.position + Vector3.up * cameraHeight + offset;
            cameraTransform.LookAt(transform.position + Vector3.up * cameraHeight);
        }
        
        public void OnInteract(InputValue inputValue)
        {
                Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
                foreach (Collider hit in hits)
                {   
                    if (hit.CompareTag("Interactable"))
                    {
                        IInteractable interactable = hit.GetComponent<IInteractable>();
                        interactable?.Interact(transform);
                        break;
                    }
                }
        }   

        public void OnHeal(InputValue inputValue)
        {
            vitals.Heal(100f);
        }
    }
}
