using System.Collections.Generic;
using System;
using UnityEngine;

namespace AI.StateFactory
{
    public class StateFactory
    {
        private StateMachine sm;
        private Dictionary<string, IState> states;

        private Animator animator;

        public StateFactory(StateMachine sm, Animator animator)
        {
            this.sm = sm;
            this.animator = animator;
            this.states = new Dictionary<string, IState>();
        }

        private IState FetchOrCreate(string key, Func<IState> constructor)
        {
            if (!states.ContainsKey(key))
                states.Add(key, constructor());
            return states[key];
        }

        public IState Idle()
        {
            return FetchOrCreate("Idle", () => new IdleState(sm));
        }

        public IState Follow(Transform target)
        {
            // Follow is NOT cached by key alone because the target can differ.
            // For a single player target this is fine, but we create fresh if target changes.
            if (states.TryGetValue("Follow", out IState existing))
            {
                if (existing is FollowState fs && fs.Target == target)
                    return existing;
                states.Remove("Follow");
            }
            FollowState newFollow = new FollowState(sm, target);
            states["Follow"] = newFollow;
            return newFollow;
        }

        public IState Attack(Transform target)
        {
            if (states.TryGetValue("Attack", out IState existing))
            {
                if(existing is AttackState atk && atk.Target == target)
                {
                    return existing;
                }
                states.Remove("Attack");
            }
            var newAtk = new AttackState(sm, target, animator);
            states["Attack"] = newAtk;
            return newAtk;
        }
    }
}