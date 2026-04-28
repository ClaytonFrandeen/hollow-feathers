using Unity.VisualScripting;
using UnityEngine;
//This makes it so weapon data can be created through unity project menu
//In create in the assest menu, look for weapons. This way we can create serlizable
//fields for all new weapons
namespace Enemy{
    [CreateAssetMenu(menuName = "Enemy/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private float baseDamage;
        [SerializeField] private float baseHealth;
        [SerializeField] private string currency;
        [SerializeField] public AnimatorOverrideController weaponAnimator;
        public AnimatorOverrideController GetWeaponAnimator() => weaponAnimator;
        public float GetBaseDamage(){return baseDamage;}
        public float GetBaseHealth(){return baseHealth;}
        public string GetCurrency(){return currency;}
    }
}