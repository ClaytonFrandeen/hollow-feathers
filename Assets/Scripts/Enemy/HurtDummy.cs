using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using NUnit.Framework;

public class HurtDummy : MonoBehaviour
{
    [SerializeField] Rigidbody rd;
    PlayerDamageable pd;
    public UnityEvent dummyHit = new UnityEvent();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Assert(rd, "rigidbody doesnt exist on the hurt dummy");
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("hurtdummy hitbox entered");
        if (other.CompareTag("Player"))
        {
            //pd.TakeDamage(10f, other.gameObject.transform.position); //TODO playerdamageable integration
            dummyHit?.Invoke();
        }
    }
}
