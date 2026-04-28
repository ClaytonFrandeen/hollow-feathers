using UnityEngine;

namespace AI.StateFactory 
{
    public interface IState {
        void Update(float delta);
        void Reset();
    }
}