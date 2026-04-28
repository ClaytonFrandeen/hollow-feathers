using UnityEngine;
using UnityEngine.AI;

namespace AI.StateFactory
{
    public class StateMachine
    {
        IState current;
        public readonly NavMeshAgent agent;
        public readonly Transform transform;
        public readonly StateFactory factory;
        public string debugName = "None";

        public StateMachine(NavMeshAgent agent, Animator animator)
        {
            this.agent = agent;
            this.transform = agent.transform;
            this.factory = new StateFactory(this, animator);
        }

        public void SetState(IState next)
        {
            current = next;
            current.Reset();
#if UNITY_EDITOR
            debugName = current.GetType().Name;
#endif
        }

        public void Update(float delta)
        {
            current?.Update(delta);
        }
    }
}