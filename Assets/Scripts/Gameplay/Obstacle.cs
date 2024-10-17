using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle
{
    public SphericalPoint Center { get; }
    public float Radius { get; }

    public Obstacle(SphericalPoint center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool IsColliding(SphericalPoint point) => point.GetDistance(Center) < (Radius + 1);
}
