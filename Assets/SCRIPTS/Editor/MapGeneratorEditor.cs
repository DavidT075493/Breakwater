using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor {

	public override void OnInspectorGUI() {
		MapGenerator mapGen = (MapGenerator)target;

		if (DrawDefaultInspector ()) {
			if (mapGen.autoUpdate) {
				mapGen.DrawMapInEditor ();
			}
		}

		if (GUILayout.Button ("Generate")) {
			mapGen.DrawMapInEditor ();
		}

		if (GUILayout.Button("Clear Tiles"))
		{
			FindAnyObjectByType<MapDisplay>().ClearTiles();
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
