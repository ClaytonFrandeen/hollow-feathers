using UnityEngine;

namespace AI.StateFactory
{
    public class AttackState : BaseState
    {
        private const float AttackRange = 2.2f;
        private const float AttackCooldown = 1.5f; // match your animation length
        public Transform Target => follow;
        private Animator animator;


        public AttackState(StateMachine sm, Transform follow, Animator animator)
            : base(sm, follow)
        {
            this.animator = animator;
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            if (follow == null)
            {
                sm.SetState(sm.factory.Idle());
                return;
            }

            // Stop moving while attacking
            sm.agent.SetDestination(sm.transform.position);

            // Face the player
            Vector3 dir = (follow.position - sm.transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
                sm.agent.transform.rotation = Quaternion.LookRotation(dir);

            float sqDist = dir.sqrMagnitude;

            // Left attack range → go back to chasing
            if (sqDist > AttackRange * AttackRange)
            {
                sm.SetState(sm.factory.Follow(follow));
                return;
            }

            // Attack on cooldown
            if (rethink >= AttackCooldown)
            {
                rethink = 0f;
                animator.SetTrigger("Attack");
            }
        }
    }
}