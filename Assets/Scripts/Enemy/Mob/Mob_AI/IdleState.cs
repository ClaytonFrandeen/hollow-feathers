using UnityEngine;

namespace AI.StateFactory
{
    public class IdleState : BaseState
    {
        private const float AggroRange = 15f;
        private const float PollInterval = 1f;

        public IdleState(StateMachine sm) : base(sm, null) {}

        public override void Update(float delta)
        {
            base.Update(delta);

            sm.agent.SetDestination(sm.transform.position);

            if (rethink >= PollInterval)
            {
                rethink = 0f;

                GameObject player = GameObject.FindWithTag("Player");
                if (player == null) return;

                float sqDist = (player.transform.position - sm.transform.position).sqrMagnitude;
                if (sqDist <= AggroRange * AggroRange)
                {
                    sm.SetState(sm.factory.Follow(player.transform));
                }
            }
        }
    }
}