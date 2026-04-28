using UnityEngine;

public class State_Death : BossState
{
    private float deathTimer = 0f;
    private float timeBeforeDespawn = 4f;

    public State_Death(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        // Stop moving immediately
        brain.desiredMovementDirection = Vector3.zero;
        if (brain.bossWeapon != null) brain.bossWeapon.DisableHitbox();

        BossController body = brain.GetComponent<BossController>();
        // Ensure the sword is turned off so a dead boss doesn't kill the player
        if (brain.bossWeapon != null) brain.bossWeapon.DisableHitbox();
        if (body != null)
        {
            body.TriggerDeathVisuals();
        }
        brain.PlayAnimation("boss_death", 0.1f);
    }

    private bool animationFinished = false;
    private float despawnTimer = 0f;

    public override void Tick()
    {
        if (animationFinished)
        {
            despawnTimer += Time.deltaTime;
            if (despawnTimer >= timeBeforeDespawn)
            {
                Object.Destroy(brain.gameObject); 
            }
        }
    }

    public override void OnAnimationEvent(string eventName)
    {
        if (eventName == "OnActionComplete")
        {
            animationFinished = true;
            Debug.Log("[State_Death] Death animation complete. Starting despawn countdown.");
        }
    }

    public override Color GetStateColor() { return Color.black; } // Fade to black
    public override string GetStateName() { return "DEAD"; }
}