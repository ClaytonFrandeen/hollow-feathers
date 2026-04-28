using System;
using UnityEngine;

[Serializable] //wrapper class
public class InventoryItem //actual instanced version of our data
{
    public InvItemData data { get; private set; }
    public int stackSize { get; private set; }

    public InventoryItem(InvItemData source) //overwrite?
    {
        data = source;
        AddToStack();
    }

    public void AddToStack()
    { stackSize++; }

    public void RemoveFromStack()
    { stackSize--; }

}
