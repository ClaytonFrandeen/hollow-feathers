using System;
using Enemy;
using UnityEngine;
using System.Collections.Generic;
using weapon;

public class SkeletonFist : MonoBehaviour, IWeaponMelee
{
    [SerializeField] EnemyData enemyData;
    [SerializeField] private Collider weaponCollider;
    Team team;
    //private EnemyCombatController combatController;

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    public AnimatorOverrideController WeaponAnimator => enemyData.GetWeaponAnimator();

    public void TriggerAttack(){}
    public void EnableHitbox()
    {
        weaponCollider.enabled = true;
        hitTargets.Clear();
    }
    public void DisableHitbox()
    {
        weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Hit: {other.name}");
        Team otherTeam = other.GetComponentInParent<Team>();
        if (otherTeam != null && otherTeam == team) return; // Is friendly

        Debug.Log("OnTrigger");
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        // finds damageable enemy and only hits once per swipe
        if (damageable != null && !hitTargets.Contains(damageable))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            damageable.TakeDamage(enemyData.GetBaseDamage(), hitPoint);
            hitTargets.Add(damageable);
        }
        //TODO: add hit stagger if hit environment (if damageable == null)
    }
}
