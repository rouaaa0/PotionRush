using System.Collections.Generic;
using UnityEngine;

// A frozen patch of floor. The player slides while standing on it (see IceSlide).
// The zone is the oval covered by the object's X and Z scale, so it matches a flattened cylinder.
public class IceZone : MonoBehaviour
{
    static readonly List<IceZone> zones = new List<IceZone>();

    void OnEnable()
    {
        zones.Add(this);
    }

    void OnDisable()
    {
        zones.Remove(this);
    }

    public static bool Contains(Vector3 position)
    {
        foreach (IceZone zone in zones)
        {
            Vector3 local = zone.transform.InverseTransformPoint(position);
            if (local.x * local.x + local.z * local.z <= 0.25f) return true; // a unit cylinder has a radius of 0.5
        }
        return false;
    }
}
