using System.Collections.Generic;
using UnityEngine;

//!!code borrowed from https://youtu.be/SGz3sbZkfkg?si=tifO68AFToG9ziTl for this class and all inventory related classes since 4/4/26!!
//following a template, feel free to make adjustments to this to better suit our game!!
public class InventorySystem : MonoBehaviour
{
    private Dictionary<InvItemData, InventoryItem> m_itemDictionary; //grab item stack more easily with the reference data
    public List<InventoryItem> inventory { get; private set; }

    public void Awake()
    {
        inventory = new List<InventoryItem>();
        m_itemDictionary = new Dictionary<InvItemData, InventoryItem>();
    }

    public void Add(InvItemData referenceData)
    {
        if(m_itemDictionary.TryGetValue(referenceData, out InventoryItem value)) //if we already have the item, just increase the stack
        { value.AddToStack(); }
        else //if we dont already have the item in our inventory, make a new instance of the item and add it to our inventory
        {
            InventoryItem newItem = new InventoryItem(referenceData);
            inventory.Add(newItem);
            m_itemDictionary.Add(referenceData, newItem);
        }
    }

    public void Remove(InvItemData referenceData)
    {
        if(m_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
        {
            value.RemoveFromStack();
            //same idea as add, when removing we always remove from the stack but also
            if(value.stackSize == 0) //if we run out of the stack, we dont have the item anymore and its removed from our inventory
            {
                inventory.Remove(value);
                m_itemDictionary.Remove(referenceData);
            }
        }
    }

}
