using UnityEngine;

namespace AI.StateFactory
{
    public class FollowState : BaseState
    {
        private const float LeashRange = 40f;
        private const float StopRange = 2f;
        private const float AttackRange = 2.2f;
        public Transform Target => follow;
        public FollowState(StateMachine sm, Transform follow) : base(sm, follow) {}

        public override void Update(float delta)
        {
            base.Update(delta);

            if (follow == null)
            {
                sm.SetState(sm.factory.Idle());
                return;
            }

            Vector3 toPlayer = follow.position - sm.transform.position;
            toPlayer.y = 0f;
            float sqDist = toPlayer.sqrMagnitude;

            if (sqDist > LeashRange * LeashRange)
            {
                sm.SetState(sm.factory.Idle());
                return;
            }
            if(sqDist <= AttackRange * AttackRange)
            {
                sm.SetState(sm.factory.Attack(follow));
                return;
            }
            if (sqDist <= StopRange * StopRange)
            {
                sm.agent.SetDestination(sm.transform.position);
                return;
            }

            sm.agent.speed = 5f;
            sm.agent.SetDestination(follow.position);
        }
    }
}