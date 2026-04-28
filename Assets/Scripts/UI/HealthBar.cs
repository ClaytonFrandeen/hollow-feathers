using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private DepletingBar healthBar;
    private float maxHP; 
    private float currentHP; 
    private RectTransform healthBarRT;
    [SerializeField] private RectTransform healthFillRT; //!!getcomponentsofchildren() returns parent too for some reason, so i just split these all up and made them serialize fields for my convenience
    [SerializeField] private RectTransform healthDepleteRT; //if we had a bunch of children, it'd be cleaner to figure out a way for get components of children to work, refer to farm project farmtile for 'foreach'
    [SerializeField] private PlayerDamageable playerDamageable;
    [SerializeField] private PlayerVitals playerVitals;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBarRT = gameObject.GetComponent<RectTransform>(); //get the parent game objects rectTransform 

        Debug.Assert(healthBarRT, "a rect transform for health bar is missing!");
        Debug.Assert(healthFillRT, "a rect transform for health bar fill is missing!");
        Debug.Assert(healthDepleteRT, "a rect transform for health bar deplete is missing!");
        
        maxHP = playerVitals.maxHealth;
        currentHP = playerVitals.currentHealth;

        //Debug.Log("maxhp is " + maxHP);

        UpdateMaxHP(); //!!TODO: add a listener for a maxHP changing event call this function instead!!
        //This returns two floats, currenthp and maxhp
    }

    private void OnEnable()
    {
        playerVitals.OnHealthChanged += HandleHealthChanged; // If player takes damage call this
    }

    private void OnDisable()
    {
        playerVitals.OnHealthChanged -= HandleHealthChanged; 
    }
    // This handles the health update from the damaged event on playerdamageable
    private void HandleHealthChanged(float current, float max)
    {
        maxHP = max;
        ChangeCurrentHP(current);
    }

    
    private void ChangeCurrentHP(float newHP)
    {
        //Debug.Log("changing current hp");
            StartCoroutine(HealthBarRolldown(healthBar, currentHP, newHP));
            StopCoroutine(HealthBarRolldown(healthBar, currentHP, newHP));
            currentHP = newHP;
    }
    private IEnumerator HealthBarRolldown(DepletingBar hp, float originalValue, float newValue)
    {
        //Debug.Log("rolling first bar");
        float oV2 = originalValue;
        float nV2 = newValue; //make clones of these for the second bar

        while (originalValue != newValue) //first bar depletion
        {
            originalValue = Mathf.MoveTowards(originalValue, newValue, Time.fixedDeltaTime * 40f);
            hp.SetFillA(originalValue / maxHP); //needs to be fixed later !!TODO: make a formula that clamps our health to an equivalent value between 0 and 1 (fillamount only work with values between 0 and 1)!!
            yield return null;
        }

        //Debug.Log("delay start");
        yield return new WaitForSeconds(1.3f);
        //Debug.Log("delay end");

        //Debug.Log("rolling second bar");
        StartCoroutine(HealthBarRolldownPart2(healthBar, oV2, nV2));
        StopCoroutine(HealthBarRolldownPart2(healthBar, oV2, nV2));
    }

    private IEnumerator HealthBarRolldownPart2(DepletingBar hp, float originalValue, float newValue)
    {
        while (originalValue != newValue) //second bar depletion
        {
            originalValue = Mathf.MoveTowards(originalValue, newValue, Time.fixedDeltaTime * 7f);
            hp.SetFillB(originalValue / maxHP); //needs to be fixed later !!TODO: make a formula that clamps our health to an equivalent value between 0 and 1 (fillamount only work with values between 0 and 1)!!
            yield return null;
        }  
    }
    public void UpdateMaxHP() //this resizes the health bar according to the 'maxHP', we should make a getset for maxhp in playerstats 
    {
        //Debug.Log("UpdateMaxHP() tripped");
        if(maxHP > 0) //these change the rectTransform 'width'
        {
            healthBarRT.sizeDelta = new Vector2(maxHP, healthBarRT.sizeDelta.y);
            healthDepleteRT.sizeDelta = new Vector2(maxHP, healthDepleteRT.sizeDelta.y);
            healthFillRT.sizeDelta = new Vector2(maxHP, healthFillRT.sizeDelta.y);
        }
        else if (maxHP < 1) //do not display a maxHP value lower than 1
        {
            healthBarRT.sizeDelta = new Vector2(1, healthBarRT.sizeDelta.y);
            healthDepleteRT.sizeDelta = new Vector2(1, healthDepleteRT.sizeDelta.y);
            healthFillRT.sizeDelta = new Vector2(1, healthFillRT.sizeDelta.y);
        }
        //there was old repositioning logic here, but as it turns out, you can set a pivot so that increases in width only apply in one direction, all you need to do is set the x pivot to 0 for a left pivot, and 1 for a right pivot
    }
}
