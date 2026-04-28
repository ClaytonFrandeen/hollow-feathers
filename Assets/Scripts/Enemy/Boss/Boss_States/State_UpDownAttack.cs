using UnityEngine;

public class State_UpDownAttack : BossState
{
    private float stateTimer = 0f;
    private float windupDuration = 2.0f;
    private float actionDuration = 3.0f;
    
    private bool actionStarted = false;
    private float initialY = 0f;
    private float jumpHeight = 15f;
    
    private UnityEngine.AI.NavMeshAgent navAgent;

    public State_UpDownAttack(BossStateBrain brain) : base(brain) { }

    public override void Enter()
    {
        stateTimer = 0f;
        actionStarted = false;
        
        initialY = brain.GetGroundHeight(brain.transform.position);
        
        navAgent = brain.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = false;
        
        brain.desiredMovementDirection = Vector3.zero;
        brain.PlayAnimation("boss_windUp");
        Debug.Log("[State_UpDownAttack] NavAgent disabled. Executing vertical leap.");
    }

    public override void Tick()
    {
        stateTimer += Time.deltaTime;
        brain.desiredMovementDirection = Vector3.zero;

        if (stateTimer < windupDuration)
        {
            // --- WINDUP PHASE ---
            if (brain.currentTarget != null)
            {
                // Slowly look at player
                Vector3 targetPos = brain.currentTarget.GetPosition();
                targetPos.y = brain.transform.position.y;
                Vector3 directionToPlayer = (targetPos - brain.transform.position).normalized;
                if (directionToPlayer != Vector3.zero)
                {
                    brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, Quaternion.LookRotation(directionToPlayer), Time.deltaTime * 2f);
                }
            }

            // Ascend
            float t = stateTimer / windupDuration;
            // Ease out jump
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            Vector3 pos = brain.transform.position;
            pos.y = Mathf.Lerp(initialY, initialY + jumpHeight, easedT);
            brain.transform.position = pos;
        }
        else if (stateTimer < windupDuration + actionDuration)
        {
            // --- ACTION PHASE ---
            float actionTimer = stateTimer - windupDuration;
            
            // Fast drop on the first 0.2 seconds
            if (actionTimer <= 0.2f)
            {
                float dropT = actionTimer / 0.2f;
                // Ease in drop
                float easedDrop = dropT * dropT * dropT;
                
                Vector3 pos = brain.transform.position;
                pos.y = Mathf.Lerp(initialY + jumpHeight, initialY, easedDrop);
                brain.transform.position = pos;
            }
            else
            {
                // Ensure grounded
                Vector3 pos = brain.transform.position;
                pos.y = initialY;
                brain.transform.position = pos;

                // Fire the rocks once grounded
                if (!actionStarted)
                {
                    actionStarted = true;
                    brain.PlayAnimation("boss_headSmash", 0.1f);
                    SpawnRockWaves();
                }
            }
        }
        else
        {
            brain.ChooseNextState();
        }
    }

    private void SpawnRockWaves()
    {
        int rockCount = Random.Range(10, 16);
        float radius = 3.0f; // Distance from boss to spawn

        for (int i = 0; i < rockCount; i++)
        {
            float angle = i * Mathf.PI * 2 / rockCount;
            Vector3 spawnDir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 spawnPos = brain.transform.position + spawnDir * radius;
            spawnPos.y = initialY + 1.5f; // Slightly elevated so they drop/roll cleanly
            
            // Procedurally create sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = spawnPos;
            sphere.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); // Medium boulder
            
            // Set material color for visual distinction
            Renderer rd = sphere.GetComponent<Renderer>();
            if (rd != null)
            {
                rd.material.color = new Color(0.3f, 0.2f, 0.1f); // Dirty brown
            }

            Rigidbody rb = sphere.AddComponent<Rigidbody>();
            rb.mass = 20f; // lighter so it bursts faster
            rb.angularDamping = 1f;

            sphere.AddComponent<RollingRockDamage>();

            // Push outward much faster
            rb.AddForce(spawnDir * 4000f, ForceMode.Impulse);
        }
        Debug.Log($"[State_UpDownAttack] Spawned {rockCount} procedural rolling boulders.");
    }

    public override void Exit()
    {
        if (navAgent != null)
        {
            // Teleport gently onto NavMesh to avoid bugs
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(brain.transform.position, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                brain.transform.position = hit.position;
            }
            navAgent.enabled = true;
        }

        brain.desiredMovementDirection = Vector3.zero;
        brain.SetCooldown(this.GetType(), 10.0f);
    }

    public override Color GetStateColor() 
    { 
        return stateTimer < windupDuration ? Color.green : new Color(0f, 0.5f, 0f); 
    }
    
    public override string GetStateName() 
    { 
        return stateTimer < windupDuration ? "LEAPING INTO AIR" : "LANDING CRATER!"; 
    }
}
