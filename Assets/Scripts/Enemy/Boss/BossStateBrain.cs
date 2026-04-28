using System.Collections.Generic;
using UnityEngine;

public class BossStateBrain : MonoBehaviour, IAIBrain
{
    private BossState currentState;
    private bool isStateTransitioning = false;
    public IPlayerState currentTarget; // The states will read from this
    public Vector3 desiredMovementDirection = Vector3.zero; // The states will write to this
    public bool isAttackingFlag = false;
    public bool isPlayerInArena = false;
    public bool hasPlayerEverEnteredArena = false;
    public bool hasStartedCombat = false; // Initial startup state tracking
    public Animator animator;

    public BossWeaponHitbox bossWeapon { get; private set; }
    public BossSensors sensors { get; private set; }

    public Collider arenaCollider; // Drag the arena's floor/trigger collider here in the Editor

    private Dictionary<System.Type, float> stateCooldowns = new Dictionary<System.Type, float>();

    public LayerMask groundLayer;
    
    public Vector3 originalPosition; // Where the boss spawned
    public Quaternion originalRotation;

    [Header("Arena Reposition Waypoints")]
    [Tooltip("Drag the group of temporary cubes here, then click the gear icon to Bake them.")]
    public Transform waypointSetupRoot;
    [Tooltip("The ScriptableObject that stores the baked NavMesh coordinates.")]
    public BossArenaWaypoints arenaWaypoints;

    [Header("Sequence Settings")]
    public Transform headTransform;
    public bool isInArenaSequence = false;

    // Helper methods for dynamic arena knowledge
    public bool IsPointInArena(Vector3 point) 
    { 
        if (arenaCollider == null) return true; // Fallback if not assigned
        return arenaCollider.bounds.Contains(point); 
    }
    public Vector3 GetClosestArenaPoint(Vector3 point) 
    { 
        if (arenaCollider == null) return point; // Fallback if not assigned
        return arenaCollider.ClosestPoint(point); 
    }



    public float GetGroundHeight(Vector3 position) 
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 100.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position.y;
        }
        return position.y; // Fallback
    }


    // Hit Counter Interrupt
    public int consecutiveHits = 0;
    private int consecutiveHitsThreshold = 3;

    // Progression mechanics
    public int headSmashUsageCount = 0;

    // Retrieval flag for player out of arena
    public bool isRetrievingPlayer = false;
    
    void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        bossWeapon = GetComponentInChildren<BossWeaponHitbox>();
        sensors = gameObject.AddComponent<BossSensors>();
        
        // Prioritize the Animator on the root object (where the brain logic is)
        animator = GetComponent<Animator>();
        if (animator == null) 
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Debug.Log($"[Boss AI] Found Animator on child object: {animator.gameObject.name}. Consider moving it to the root Boss_Bird object.");
            }
        }

        // Dynamically find and assign the arena collider
        if (arenaCollider == null)
        {
            GameObject fightZone = GameObject.Find("Boss_Fight_Zone");
            if (fightZone != null)
            {
                arenaCollider = fightZone.GetComponent<Collider>();
                if (arenaCollider != null)
                {
                    Debug.Log("[Boss AI] Dynamically assigned arena collider from Boss_Fight_Zone.");
                }
                else
                {
                    Debug.LogWarning("[Boss AI] Boss_Fight_Zone found but no Collider component!");
                }
            }
            else
            {
                Debug.LogWarning("[Boss AI] Could not find Boss_Fight_Zone GameObject for arena collider assignment.");
            }
        }

        // Start the boss in the Idle state for several seconds initially
        ChangeState(new State_Idle(this, 10.0f));

        // Try to find the head transform dynamically
        if (headTransform == null)
        {
            headTransform = FindTransformDeep(transform, "C_head");
            if (headTransform != null) Debug.Log("[Boss AI] Found C_head via deep search.");
        }

        BossDamageable damageable = GetComponentInChildren<BossDamageable>();
        if (damageable != null)
        {
            // When OnBossDeath fires, change to the Death state
            damageable.OnBossDeath += () => ChangeState(new State_Death(this));
            damageable.OnTakeDamage += HandleHit;
        }

        consecutiveHitsThreshold = Random.Range(3, 6);
    }

    private void HandleHit()
    {
        if (currentState is State_Death) return;
        
        consecutiveHits++;
        if (consecutiveHits >= consecutiveHitsThreshold)
        {
            TriggerHitInterrupt();
        }
    }

    private void TriggerHitInterrupt()
    {
        if (isStateTransitioning) return;
        
        consecutiveHits = 0;
        consecutiveHitsThreshold = Random.Range(3, 6);

        int choice = Random.Range(0, 2);
        switch (choice)
        {
            case 0:
                ChangeState(new State_WavePushBack(this));
                break;
            case 1:
                ChangeState(new State_WingAttack(this, true)); // Fast windup flag overload!
                break;
        }
    }

    public static Transform FindTransformDeep(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindTransformDeep(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    void Update()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            return;
        }

        sensors.UpdateSensors(currentTarget);

        // --- ARENA BOUNDARY CHECK (BOSS TOO FAR OUT) ---
        // REMOVED entirely per user request to disable forced jumps stopping the boss

        // --- ARENA BOUNDARY CHECK (PLAYER LEFT ARENA) ---
        if (hasPlayerEverEnteredArena && !isPlayerInArena && !(currentState is State_EdgeWatch) && !(currentState is State_ReturnToSpawn) && !(currentState is State_Death))
        {
            Debug.Log("[Boss AI] Player has left arena. Entering EdgeWatch (staredown/grapple) behavior.");
            isRetrievingPlayer = false;
            ChangeState(new State_EdgeWatch(this));
            return;
        }


        // Run the current state's logic every frame
        currentState?.Tick();
    }

    public bool IsPathToPlayerValid()
    {
        if (currentTarget == null) return true; // Don't trigger rescue if no player geometry
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null || !agent.enabled) return true; // Don't trigger rescue while mathematically jumping natively
        
        if (!agent.isOnNavMesh) return false; // Stranded in the air or off-graph!

        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        if (agent.CalculatePath(currentTarget.GetPosition(), path))
        {
            return path.status != UnityEngine.AI.NavMeshPathStatus.PathInvalid; // Accept PathComplete and PathPartial
        }
        return false;
    }

    public void ChooseNextState()
    {
        if (!hasStartedCombat)
        {
            hasStartedCombat = true;
            Debug.Log("[Boss AI] Initial idle complete. Forcing initial CHASE state!");
            ChangeState(new State_Chase(this));
            return;
        }

        if (currentTarget == null || !currentTarget.IsAlive())
        {
            ChangeState(new State_Idle(this, 1.5f));
            return;
        }

        // --- SENIOR AI PATH VALIDATION CHECK ---
        // RELAXED entirely to prevent panic rescue leaps. Boss will now just resort to ranged or localized attacks if pathing fails.

        float dist = sensors.DistanceToTarget;
        float r = Random.value;
        BossState selectedState = null;

        consecutiveHits = 0; // Reset passive counter if we successfully choose a state naturally

        if (dist < 8.0f)
        {
            // Close Range (< 8.0f)
            if (r < 0.30f) selectedState = new State_WingAttack(this, false);
            else if (r < 0.58f) selectedState = new State_WavePushBack(this);
            else if (r < 0.80f) selectedState = new State_HeadSmashAttack(this);
            else selectedState = new State_PreciseProjectileAttack(this);
        }
        else if (dist <= 18.0f)
        {
            // Mid Range (8.0f - 18.0f)
            if (r < 0.18f) selectedState = new State_Chase(this);
            else if (r < 0.32f) selectedState = new State_DashAttack(this);
            else if (r < 0.46f) selectedState = new State_RangedAttack(this);           // Straight shots
            else if (r < 0.60f) selectedState = new State_PreciseProjectileAttack(this);
            else if (r < 0.73f) selectedState = new State_HeadSmashAttack(this);         // Ground shockwave at this range!
            else if (r < 0.84f) selectedState = new State_UpDownAttack(this);
            else if (r < 0.93f) selectedState = new State_ArenaReposition(this);
            else selectedState = new State_Idle(this, 2.0f);
        }
        else
        {
            // Long Range (> 18.0f) — heavily projectile + arena jumps
            if (r < 0.12f) selectedState = new State_Chase(this);
            else if (r < 0.20f) selectedState = new State_DashAttack(this);
            else if (r < 0.38f) selectedState = new State_ArenaReposition(this);         // Jump to high point + ring drop!
            else if (r < 0.56f) selectedState = new State_RangedAttack(this, true);      // Parabola barrage (from ground)
            else if (r < 0.72f) selectedState = new State_RangedAttack(this);            // Straight shots
            else selectedState = new State_PreciseProjectileAttack(this);
        }

        // Cooldown fallback — go aggressive instead of idle
        if (selectedState == null || IsOnCooldown(selectedState.GetType()))
        {
            // 50/50 between chasing and pushing back instead of standing around
            if (Random.value < 0.5f && !IsOnCooldown(typeof(State_Chase)))
                ChangeState(new State_Chase(this));
            else if (!IsOnCooldown(typeof(State_WavePushBack)))
                ChangeState(new State_WavePushBack(this));
            else
                ChangeState(new State_Idle(this, 1.5f)); // True last resort
        }
        else
        {
            ChangeState(selectedState);
        }
    }

    public void ChangeState(BossState newState)
    {
        if (isStateTransitioning)
        {
            Debug.Log("[Boss AI] Transition request ignored because another transition is in progress.");
            return;
        }

        isStateTransitioning = true;

        currentState?.Exit();

        if (currentState != null)
        {
            if (!IsOnCooldown(currentState.GetType()))
            {
                SetCooldown(currentState.GetType(), 1.0f);
            }
        }

        string oldName = currentState?.GetStateName() ?? "None";
        currentState = newState;
        Debug.Log($"[BRAIN DEBUG] {gameObject.name} State Change: {oldName} -> {newState.GetStateName()}");
        currentState?.Enter();

        isStateTransitioning = false;
    }

    public void PlayAnimation(string stateName, float crossFade = 0.2f)
    {
        if (animator != null)
        {
            if (animator.HasState(0, Animator.StringToHash(stateName)))
            {
                AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);

                if (currentInfo.IsName(stateName) || nextInfo.IsName(stateName))
                {
                    return;
                }

                Debug.Log($"[Anim Debug] {gameObject.name} Triggering Transition on {animator.gameObject.name}: -> {stateName}");
                animator.CrossFadeInFixedTime(stateName, crossFade);
            }
            else
            {
                Debug.LogWarning($"[Anim Error] The state '{stateName}' was not found in the Animator on {animator.gameObject.name}!");
            }
        }
        else
        {
            Debug.LogError($"[Anim Error] No Animator found for {gameObject.name} to play '{stateName}'!");
        }
    }

    // These methods are called by Unity Animation Events.
    // Ensure you name your events "OnFlap", "OnSmash", or "OnFire" in the Animation window.
    
    public void OnFlap() { currentState?.OnAnimationEvent("OnFlap"); }
    public void OnSmash() { currentState?.OnAnimationEvent("OnSmash"); }
    public void OnFire() { currentState?.OnAnimationEvent("OnFire"); }
    
    // Compatibility placeholders
    public void Hit() { currentState?.OnAnimationEvent("Hit"); }
    public void Footstep() { currentState?.OnAnimationEvent("Footstep"); }
    public void OnActionComplete() { currentState?.OnAnimationEvent("OnActionComplete"); }

    public void SetTarget(IPlayerState target)
    {
        currentTarget = target;
    }

    // --- COOLDOWN METHODS ---
    public void SetCooldown(System.Type stateType, float duration)
    {
        stateCooldowns[stateType] = Time.time + duration;
    }

    public bool IsOnCooldown(System.Type stateType)
    {
        if (stateCooldowns.TryGetValue(stateType, out float readyTime))
        {
            return Time.time < readyTime;
        }
        return false;
    }

    // --- HOP TRACKING (for grapple mechanic) ---
    public int consecutiveHopCount = 0;
    public int maxHopsBeforeGrapple = 5;

    public void IncrementHopCount()
    {
        consecutiveHopCount++;
    }

    public void ResetHopCount()
    {
        consecutiveHopCount = 0;
    }

    public bool ShouldUseGrapple()
    {
        return consecutiveHopCount >= maxHopsBeforeGrapple || isRetrievingPlayer;
    }

    // --- IAIBrain Implementation ---
    public Vector3 GetMovementDirection() { return desiredMovementDirection; }
    public bool WantsToAttack() { return isAttackingFlag; }
    public Color GetDebugColor() { return currentState?.GetStateColor() ?? Color.white; }

#if UNITY_EDITOR
    [ContextMenu("Bake Cubes to ScriptableObject")]
    public void BakeWaypointsToSO()
    {
        if (arenaWaypoints == null)
        {
            Debug.LogError("Please assign a BossArenaWaypoints ScriptableObject to the arenaWaypoints field first!");
            return;
        }

        if (waypointSetupRoot == null || waypointSetupRoot.childCount == 0)
        {
            Debug.LogError("Please assign the waypointSetupRoot with at least one child cube!");
            return;
        }

        List<BossArenaWaypoints.WaypointData> bakedPoints = new List<BossArenaWaypoints.WaypointData>();

        foreach (Transform child in waypointSetupRoot)
        {
            // Massive 50-foot robust downward search
            if (UnityEngine.AI.NavMesh.SamplePosition(child.position, out UnityEngine.AI.NavMeshHit hit, 50.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                BossArenaWaypoints.WaypointData data = new BossArenaWaypoints.WaypointData();
                data.pointName = child.name;
                data.position = hit.position;
                
                // Categorize by name
                if (child.name.ToLower().Contains("center")) 
                    data.type = BossArenaWaypoints.WaypointType.Center;
                else if (child.name.ToLower().Contains("high")) 
                    data.type = BossArenaWaypoints.WaypointType.High;
                else 
                    data.type = BossArenaWaypoints.WaypointType.Bottom;

                // Derive group name (e.g. "Middle_Side1" from "Middle_Side1_HighPoint")
                string[] parts = child.name.Split('_');
                if (parts.Length >= 2)
                {
                    data.groupName = parts[0] + "_" + parts[1];
                }

                bakedPoints.Add(data);
            }
            else
            {
                Debug.LogWarning($"Failed to find NavMesh surface near cube at {child.position}. It might be completely off the map.");
            }
        }

        arenaWaypoints.waypoints = bakedPoints.ToArray();
        UnityEditor.EditorUtility.SetDirty(arenaWaypoints);
        Debug.Log($"[Boss AI] Successfully baked {bakedPoints.Count} named/typed waypoints into the ScriptableObject!");
    }
#endif
}
