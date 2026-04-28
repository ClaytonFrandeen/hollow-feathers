using System;
using System.Collections;
using NUnit.Framework.Internal.Commands;
using UnityEngine;
public class PlayerLevel : MonoBehaviour
{
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    private int currentLevel;
    private int cost;

    void Start()
    {
        currentLevel = 0;
        cost = 100;
    }

    void LevelUp (string level) 
    {
        if (playerInventory.fragments < cost) return;
        playerInventory.fragments -= cost;

        switch(level)
        {
            case "constitution": 
                playerAttributes.ModifyConstitution(1);
                break;
            case "endurance":
                playerAttributes.ModifyEndurance(1);
                break;
            case "strength":
                playerAttributes.ModifyStrength(1);
                break;
            default :
                break;
        }
        currentLevel ++;

        cost = Mathf.RoundToInt(100f * Mathf.Pow(1.15f, currentLevel));
    }
}