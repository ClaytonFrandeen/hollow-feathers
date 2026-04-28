using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public PlayerAttributes attributes;
    public float maxHealth => attributes.constitution *5f;
    public float maxStamina => attributes.endurance *5f;
}
