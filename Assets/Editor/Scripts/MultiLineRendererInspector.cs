using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MultiLineRenderer))]
public class MultiLineRendererInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Reference to the target object
        MultiLineRenderer multiLineRenderer = (MultiLineRenderer)target;

        EditorGUILayout.Separator();

        // Add a button to the inspector
        if (GUILayout.Button("Positions"))
        {
            // Get all the positions from the MultiLineRenderer
            Vector3[] positions = new Vector3[multiLineRenderer.PositionCount];
            multiLineRenderer.GetPositions(positions);

            // Print each position to the console
            Debug.Log("Points in MultiLineRenderer:");
            for (int i = 0; i < positions.Length; i++)
            {
                Debug.Log($"Point {i}: {positions[i]}");
            }
        }
        if (GUILayout.Button("Angles"))
        {
            Vector3[] positions = new Vector3[multiLineRenderer.PositionCount];
            multiLineRenderer.GetPositions(positions);

            for (int i = 1; i < positions.Length - 1; i++)
            {
                Vector3 diff1 = positions[i] - positions[i - 1];
                Vector3 diff2 = positions[i + 1] - positions[i];
                float angle = Vector3.Angle(diff1, diff2);
                Debug.Log($"Angle at point {i}: {angle} degrees");
            }
        }
        if (GUILayout.Button("Unusual Point Distances"))
        {
            Vector3[] positions = new Vector3[multiLineRenderer.PositionCount];
            multiLineRenderer.GetPositions(positions);

            for (int i = 0; i < positions.Length - 2; i++)
            {
                float distanceToNext = Vector3.Distance(positions[i], positions[i + 1]);
                float distanceToTwoAhead = Vector3.Distance(positions[i], positions[i + 2]);

                if (distanceToTwoAhead < distanceToNext)
                {
                    Debug.LogWarning($"Point {i} is closer to point {i + 2} ({distanceToTwoAhead:F2}) than to point {i + 1} ({distanceToNext:F2}).");
                }
            }
        }
        if (GUILayout.Button("FIX Unusual Point Distances"))
        {
            multiLineRenderer.CleanupPoints();
        }
    }
}