using UnityEngine;

public class BossAnimationProxy : MonoBehaviour
{
    private BossStateBrain brain;

    void Awake()
    {
        // Find the brain on the parent object
        brain = GetComponentInParent<BossStateBrain>();
    }

    // These methods catch the events from the Animator on this GameObject
    // and pass them up to the BossStateBrain on the parent.
    
    public void OnFlap() 
    { 
        if (brain != null) brain.OnFlap(); 
    }

    public void OnSmash() 
    { 
        if (brain != null) brain.OnSmash(); 
    }

    public void OnFire() 
    { 
        if (brain != null) brain.OnFire(); 
    }

    public void Hit() 
    { 
        if (brain != null) brain.Hit(); 
    }

    public void Footstep() 
    { 
        if (brain != null) brain.Footstep(); 
    }

    public void OnActionComplete() 
    { 
        if (brain != null) brain.OnActionComplete(); 
    }
}
