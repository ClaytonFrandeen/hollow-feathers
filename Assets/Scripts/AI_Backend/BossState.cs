using UnityEngine;

public abstract class BossState
{
    protected BossStateBrain brain; // Reference back to the manager

    public BossState(BossStateBrain brain)
    {
        this.brain = brain;
    }

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }
    public virtual void OnAnimationEvent(string eventName) { }

    // Every state must declare its visual debugging info
    public abstract Color GetStateColor();
    public abstract string GetStateName();

    // Helper for attack windups: increasingly flashes between baseColor and bright red
    protected Color GetFlashingWarningColor(Color baseColor, float currentTimer, float windupDuration)
    {
        if (currentTimer >= windupDuration) return baseColor;
        
        // Progress from 0 to 1
        float progress = currentTimer / windupDuration;
        
        // Increase the frequency of the flash as progress approaches 1
        // Using a built-in curve (x^2) for the time accumulator creates a smoothly increasing frequency
        float flashes = 3f; // base number of flashes
        float flashPhase = (currentTimer * flashes + (progress * progress * 5f)) * Mathf.PI * 2f;
        
        // Sine wave mapped to 0-1
        float blend = (Mathf.Sin(flashPhase) + 1f) * 0.5f;

        // Also increase the intensity/max blend over time, so it gets redder
        blend *= progress; 

        return Color.Lerp(baseColor, Color.red, blend);
    }
}
