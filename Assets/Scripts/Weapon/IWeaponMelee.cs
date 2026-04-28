using UnityEngine;
namespace weapon {
    public interface IWeaponMelee{
        void TriggerAttack();
        void EnableHitbox();
        void DisableHitbox();
        //void StartSwing();
       // void EndSwing();
        AnimatorOverrideController WeaponAnimator { get; }
    }
}
