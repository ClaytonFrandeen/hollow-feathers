using UnityEngine;
using System.Collections.Generic;
using Character;
using UnityEditor;
using UnityEditor.Rendering;

// basic sword class
// (doesn't have to be sword, any melee weapon can use this)
namespace weapon {
    public class Sword : MonoBehaviour, IWeaponMelee
    {
        [Header("References")]
        [SerializeField] private WeaponData data;
        [SerializeField] private Collider weaponCollider;
        private Rigidbody weaponRigidbody;
        private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        public AnimatorOverrideController WeaponAnimator => data.GetWeaponAnimator();
        Team team;
        private CombatController combatController;


        private void Start()
        {
            weaponCollider.enabled = false; //starts off so it doesn't auto hit
            team = GetComponentInParent<Team>(); //Grabs team from parent
            combatController = GetComponentInParent<CombatController>();

            weaponRigidbody = GetComponent<Rigidbody>();
            if (weaponRigidbody != null)
            {
                weaponRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        public void TriggerAttack()
        {
            // This won't activate weapon hit boxes
            // Animation events will call EnableHitbox / DisableHitbox
            // Clear hit history at the start of every new attack/swing to avoid one-hit-only permanent lockout
            hitTargets.Clear();
        }


        // Called via animation event
        public void EnableHitbox()
        {
            weaponCollider.enabled = true;
            hitTargets.Clear();
        }

        // Called via animation event
        public void DisableHitbox()
        {
            weaponCollider.enabled = false;
            combatController.EndAttack();
        }


        private void OnTriggerEnter(Collider other)
        {
            Team otherTeam = other.GetComponentInParent<Team>();
            if (otherTeam != null && otherTeam == team) return; // Is friendly


            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            // finds damageable enemy and only hits once per swipe
            if (damageable != null && !hitTargets.Contains(damageable))
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                damageable.TakeDamage(data.GetBaseDamage(), hitPoint);
                hitTargets.Add(damageable);
            }
            //TODO: add hit stagger if hit environment (if damageable == null)
        }
    }
}
