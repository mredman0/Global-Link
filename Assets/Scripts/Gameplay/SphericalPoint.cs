using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalPoint : MonoBehaviour
{
    public float Latitude { get; set; }  // In degrees
    public float Longitude { get; set;  } // In degrees

    public float GetDistance(SphericalPoint other)
    {
        float lat1 = Latitude * Mathf.Deg2Rad;
        float lon1 = Longitude * Mathf.Deg2Rad;
        float lat2 = other.Latitude * Mathf.Deg2Rad;
        float lon2 = other.Longitude * Mathf.Deg2Rad;

        float dlon = lon2 - lon1;
        float dlat = lat2 - lat1;

        float a = Mathf.Sin(dlat / 2) * Mathf.Sin(dlat / 2) +
                  Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                  Mathf.Sin(dlon / 2) * Mathf.Sin(dlon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return c;
    }

    public Vector3 ToCartesian()
    {
        float latRad = Latitude * Mathf.Deg2Rad;
        float lonRad = Longitude * Mathf.Deg2Rad;
        float x = Mathf.Sin(latRad) * Mathf.Cos(lonRad);
        float y = Mathf.Sin(latRad) * Mathf.Sin(lonRad);
        float z = Mathf.Cos(latRad);
        return new Vector3(x, y, z);
    }
}
