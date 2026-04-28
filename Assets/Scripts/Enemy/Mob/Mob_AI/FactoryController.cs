using UnityEngine;
using UnityEngine.AI;

namespace AI.StateFactory
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class FactoryController : MonoBehaviour
    {
        [SerializeField] private string currentState;

        private StateMachine sm;
        private Animator animator;
        private NavMeshAgent agent;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            sm = new StateMachine(agent, animator);
            sm.SetState(sm.factory.Idle());
        }

        void Update()
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
#if UNITY_EDITOR
            currentState = sm.debugName;
#endif
            sm.Update(Time.deltaTime);
        }
    }
}