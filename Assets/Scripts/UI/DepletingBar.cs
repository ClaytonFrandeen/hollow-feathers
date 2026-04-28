using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DepletingBar : MonoBehaviour
{
    [SerializeField] private SlicedFilledImage barFill; //use health/staminabarfill 
    [SerializeField] private SlicedFilledImage barDeplete; //use health/staminabardeplete
    public float FillA { set { barFill.fillAmount = value; } } //getset
    public float FillB { set { barDeplete.fillAmount = value; } } //getset

    public void SetFillA(float value)
    {  
        barFill.fillAmount = value;
    }

    public void SetFillB(float value)
    { 
        barDeplete.fillAmount = value;
    }
}
