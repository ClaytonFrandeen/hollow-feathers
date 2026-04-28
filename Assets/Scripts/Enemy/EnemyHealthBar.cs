using NUnit.Framework;
using Unity.VisualScripting;
using System.Collections;
using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private DepletingBar healthBar;
    //private GameObject clonebar;
    [SerializeField] private EnemyDamageable ed;
   //[SerializeField] private BossDamageable bd;
    [SerializeField] private RectTransform canvas;
    [SerializeField] private Transform enemyObject;
    private RectTransform healthBarRT;
    [SerializeField] private RectTransform healthFillRT; //!!getcomponentsofchildren() returns parent too for some reason, so i just split these all up and made them serialize fields for my convenience
    [SerializeField] private RectTransform healthDepleteRT;
    private float maxHP;
    private float currentHP;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBarRT = gameObject.GetComponent<RectTransform>(); //get the parent game objects rectTransform         
        gameObject.GetComponent<EnemyDamageable>();
        if(!gameObject.GetComponent<EnemyDamageable>()) { Debug.Log("ed not found, attempting to use bd instead (enemyhealthbar.cs), if theres no bd error after this, you can ignore this."); gameObject.GetComponent<BossDamageable>(); }
        //Debug.Assert(ed, "ed not found, enemyhealthbar.cs");
        //Debug.Assert(bd, "bd not found, enemyhealthbar.cs");
        Debug.Assert(canvas, "canvas not found, enemyhealthbar.cs");
        Debug.Assert(healthBar, "healthbar not found, enemyhealthbar.cs");
        Debug.Assert(healthBarRT, "a rect transform for health bar is missing! enemyhealthbar.cs");
        Debug.Assert(healthFillRT, "a rect transform for health bar fill is missing! enemyhealthbar.cs");
        Debug.Assert(healthDepleteRT, "a rect transform for health bar deplete is missing! enemyhealthbar.cs");
        //clonebar = Instantiate(healthbar);

        if(gameObject.GetComponent<EnemyDamageable>())
        {
            maxHP = ed.maxHealth;
            currentHP = ed.currentHealth;
            ed.OnHealthChanged += HandleHealthChanged;
        }
        else
        {
            //maxHP = bd.maxHealth;
           // currentHP = bd.currentHealth; 
            //bd.OnHealthChanged += HandleHealthChanged;
        }
    }

    // Update is called once per frame
    void Update()
    {
        canvas.transform.LookAt(Camera.main.transform.position, Vector3.up); //method one of enemy health bar looking towards us
        //canvas.transform.forward = Camera.main.transform.forward; //method two of enemy health bar looking towards us
    }

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
            hp.SetFillA(originalValue * .01f); //needs to be fixed later !!TODO: make a formula that clamps our health to an equivalent value between 0 and 1 (fillamount only work with values between 0 and 1)!!
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
            hp.SetFillB(originalValue * .01f); //needs to be fixed later !!TODO: make a formula that clamps our health to an equivalent value between 0 and 1 (fillamount only work with values between 0 and 1)!!
            yield return null;
        }  
    }
}
