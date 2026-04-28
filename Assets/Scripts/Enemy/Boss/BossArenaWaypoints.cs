using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBossArenaWaypoints", menuName = "Boss/Arena Waypoints")]
public class BossArenaWaypoints : ScriptableObject
{
    public enum WaypointType { Bottom, High, Center }

    [System.Serializable]
    public struct WaypointData
    {
        public string pointName;
        public Vector3 position;
        public WaypointType type;
        [Tooltip("The name of the linked High/Bottom point prefix (e.g., Middle_Side1)")]
        public string groupName;
    }

    [Tooltip("Pre-baked robust NavMesh coordinates for the boss to jump to.")]
    public WaypointData[] waypoints;

    public WaypointData? GetPointByName(string name)
    {
        foreach (var wp in waypoints)
        {
            if (wp.pointName == name) return wp;
        }
        return null;
    }

    public List<WaypointData> GetPointsByType(WaypointType type)
    {
        List<WaypointData> result = new List<WaypointData>();
        foreach (var wp in waypoints)
        {
            if (wp.type == type) result.Add(wp);
        }
        return result;
    }
}
