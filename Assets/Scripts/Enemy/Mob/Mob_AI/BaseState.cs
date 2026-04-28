using UnityEngine;

namespace AI.StateFactory
{
    public class BaseState : IState
    {
        protected StateMachine sm;
        protected float rethink = 0f;
        protected Transform follow;

        public BaseState(StateMachine sm, Transform follow)
        {
            this.sm = sm;
            this.follow = follow;
        }

        public void Reset() { rethink = 0f; }

        public virtual void Update(float delta) { rethink += delta; }
    }
}