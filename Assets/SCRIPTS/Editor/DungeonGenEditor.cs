using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (DungeonGenerator))]
public class DungeonGenEditor : Editor {

	public override void OnInspectorGUI() {
		DungeonGenerator mapGen = (DungeonGenerator)target;

		DrawDefaultInspector();

		if (GUILayout.Button ("Generate")) {
			FindObjectOfType<seed>().currentSeed = System.Guid.NewGuid().GetHashCode();
			mapGen.CreateDungeons ();
		}

		if (GUILayout.Button("Clear"))
		{
			mapGen.ClearDungeons();
		}
		/*
		GUILayout.Label("River");
        
		if (GUILayout.Button("Set River"))
		{
			mapGen.SetRiverData();
		}
        if (GUILayout.Button("Load River"))
        {
            mapGen.LoadRiver();
        }
        if (GUILayout.Button("Clear River Tilemap"))
        {
            mapGen.ClearRiver();
        }
		*/
    }
}
