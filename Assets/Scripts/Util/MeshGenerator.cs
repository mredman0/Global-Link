using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public static Mesh GenerateSphereSector(float minLatitude, float maxLatitude, float minLongitude, float maxLongitude,
        float radius = 1f, int latitudeSegments = 10, int longitudeSegments = 10)
    {
        Mesh mesh = new Mesh();

        // Lists for vertices, triangles, and normals
        Vector3[] vertices;
        int[] triangles;

        int latCount = latitudeSegments + 1; // +1 for the top vertex
        int lonCount = longitudeSegments + 1; // +1 for the last vertex in each ring

        int vertexCount = latCount * lonCount;
        vertices = new Vector3[vertexCount];
        int triangleCount = latitudeSegments * longitudeSegments * 6; // 2 triangles per quad
        triangles = new int[triangleCount];

        float latStep = (maxLatitude - minLatitude) / latitudeSegments;
        float lonStep = (maxLongitude - minLongitude) / longitudeSegments;

        int vertexIndex = 0;
        int triangleIndex = 0;

        // Generate vertices
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float latitude = minLatitude + lat * latStep;
            float latRad = latitude * Mathf.Deg2Rad;

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float longitude = minLongitude + lon * lonStep;
                float lonRad = longitude * Mathf.Deg2Rad;

                // Calculate the position on the sphere
                float x = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
                float y = radius * Mathf.Sin(latRad);
                float z = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);

                vertices[vertexIndex] = new Vector3(x, y, z);
                vertexIndex++;

                // Generate triangles (skip the last row and last column)
                if (lat < latitudeSegments && lon < longitudeSegments)
                {
                    int current = lat * lonCount + lon;
                    int next = current + lonCount;

                    // First triangle
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;

                    // Second triangle
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = current + 1;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Calculate normals for lighting
        mesh.RecalculateNormals();

        return mesh;
    }
}
