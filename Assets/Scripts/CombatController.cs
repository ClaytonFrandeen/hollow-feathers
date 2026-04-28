using System;
using NUnit.Framework;
using UnityEngine;
using weapon;

namespace Character
{
    /// <summary>
    /// Owns weapon state and hitbox toggling.
    /// Attack *triggers* come from PlayerController; this class handles the weapon side.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        private IWeaponMelee equippedWeapon;
        public bool IsAttacking { get; private set; }

        [SerializeField] private Animator playerAnimator;

        [SerializeField] private PlayerController playerController;

        private void Awake()
        {
            EquipWeapon(GetComponentInChildren<IWeaponMelee>());
            IsAttacking = false;
        }

        public void EquipWeapon(IWeaponMelee weapon)
        {
            equippedWeapon = weapon;
            if (equippedWeapon?.WeaponAnimator != null)
            {
                playerAnimator.runtimeAnimatorController = equippedWeapon.WeaponAnimator;
            }
        }

        public void LightAttack()
        {
            if (IsAttacking) return;
            IsAttacking = true;
            playerAnimator.SetTrigger("LightAttack");
        }

        public void HeavyAttack()
        {
            if (IsAttacking) return;
            IsAttacking = true;
            playerAnimator.SetTrigger("HeavyAttack");
        }

        public void EndAttack()
        {
            IsAttacking = false;
            playerController.EndAttack();
        }

        public void EnableHitbox()
        {
            equippedWeapon.EnableHitbox();
        }

        public void DisableHitbox()
        {
            equippedWeapon.DisableHitbox();
        }
    }
}
