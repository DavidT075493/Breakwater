using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(RiverGenerator))]
public class RiverGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector for the RiverGenerator script
        DrawDefaultInspector();

        // Get the RiverGenerator script
        RiverGenerator riverGenerator = (RiverGenerator)target;

        // Add a button to generate the river
        if (GUILayout.Button("Generate River"))
        {
            // Generate the river on the Tilemap
            riverGenerator.Clear();
            riverGenerator.GenerateRiver();
        }
        if (GUILayout.Button("Remove River"))
        {
            // Generate the river on the Tilemap
            riverGenerator.Clear();
        }

    }
}
