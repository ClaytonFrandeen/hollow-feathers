using Unity.VisualScripting;
using UnityEngine;
//This makes it so weapon data can be created through unity project menu
//In create in the assest menu, look for weapons. This way we can create serlizable
//fields for all new weapons
namespace weapon{
    [CreateAssetMenu(menuName = "Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject, IWeaponData
    {
        [SerializeField] private float baseDamage;
        [SerializeField] private float heavyDamage;
        [SerializeField] private float weaponWeight;
        [SerializeField] private string weaponName;
        

        // Animation Override for each individual weapon
        [SerializeField] public AnimatorOverrideController weaponAnimator;
        public AnimatorOverrideController GetWeaponAnimator() => weaponAnimator;
        public float GetBaseDamage(){return baseDamage;}
        public float GetWeaponWeight(){return weaponWeight;}
        public string GetWeaponName(){return weaponName;}
        public float GetHeavyDamage(){return heavyDamage;}
    }
}