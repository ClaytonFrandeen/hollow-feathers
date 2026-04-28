using UnityEngine;

[CreateAssetMenu(menuName = "Inventory Item Data")]
public class InvItemData : ScriptableObject
{
        public string ID;
        public string displayName;
        public rarity Rarity;
        public enum rarity { common, uncommon, rare, special };
        public int value;
        public Sprite invSprite;
        public GameObject prefab;
}

//to create an object of this, when you right click -> create something, there should be an 'inventory item data' option now
