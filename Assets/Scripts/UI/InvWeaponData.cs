using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory Weapon Data")]
public class InvWeaponData : ScriptableObject
{
        public string ID;
        public string displayName;
        public rarity Rarity;
        public enum rarity { common, uncommon, rare, special };
        public wepType wType;
        public enum wepType { sword, axe, scythe, shield, bow, greatsword, hammer }; //we dont have to use all these for the MVP, just stick with basic sword
        public wElement Element;
        public enum wElement { neutral, blood, poison, corrupt, rust, truth }; //we dont have to use all these for the MVP, just stick with neutral
        public int value;
        public int baseDamage;
        public Sprite invSprite;
        public GameObject prefab;
}

//to create an object of this, when you right click -> create something, there should be an 'inventory weapon data' option now
