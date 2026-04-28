using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class LockOnTarget : MonoBehaviour
{
    private List<Collider> overlapping;
    private bool shouldLockOn = false;

    [SerializeField] private CinemachineCamera LockOnCamera;
    
    [SerializeField] private Transform playerLocator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        overlapping = new List<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldLockOn)
        {
            Collider closest = GetClosestEnemy();
            if (closest != null)
            {
                this.transform.position = closest.transform.position;
            }
        }
        else
        {
            shouldLockOn = false;
            LockOnCamera.Priority = 0;
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
        }
        
    }

    public void LockOn()
    {
        if (!shouldLockOn)
        {
            // Trying to lock on — check if there's a target first
            Collider closest = GetClosestEnemy();
            if (closest == null) return;
        }

        shouldLockOn = !shouldLockOn;
        LockOnCamera.Priority = shouldLockOn ? 10 : 0;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        overlapping.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        overlapping.Remove(other);
    }
    
    private Collider GetClosestEnemy()
    {
        Collider closest = null;
        float closestDist = Mathf.Infinity;
        foreach (var col in overlapping)
        {
            if (col != null && col.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col;
                }
            }
        }
        return closest;
    }
}
