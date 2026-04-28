// Team.cs courtesy of Professor Hortman
using UnityEngine;
    public class Team : MonoBehaviour
    {
        public enum Group { Player, Enemy }
        public Group group;

        // AI Generated operator overloads
        public static bool operator ==(Team a, Team b)
        {
            // What would happen if we didn't use ReferenceEquals when checking for null?
            if (object.ReferenceEquals(a, b)) return true;
            if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
            
            return a.group == b.group;
        }

        public static bool operator !=(Team a, Team b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Team);
        }

        // Implement IEquatable<T> for performance
        public bool Equals(Team other)
        {
            if (other is null) return false;
            return this.group == other.group;
        }

        // Override GetHashCode for use in Maps
        public override int GetHashCode()
        {
            return group.GetHashCode();
        }
    }
