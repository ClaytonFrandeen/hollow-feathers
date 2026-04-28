using UnityEngine;
using System;


// Players Stats, can level up
public class PlayerAttributes : MonoBehaviour
{
    public int constitution = 10;
    public int endurance = 10;
    public int strength = 10;
    public event Action OnAttributesChanged;

    public void ModifyConstitution(int amount)
    {
        constitution += amount;
        OnAttributesChanged?.Invoke();
    }

    public void ModifyEndurance(int amount)
    {
        endurance += amount;
        OnAttributesChanged?.Invoke();
    }

    public void ModifyStrength(int amount)
    {
        strength += amount;
        OnAttributesChanged?.Invoke();
    }
}