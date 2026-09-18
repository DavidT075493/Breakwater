using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (Chat))]
public class ChatEditor : Editor {

	public override void OnInspectorGUI() {
		Chat game = (Chat)target;

		DrawDefaultInspector();


		if (GUILayout.Button ("Set Kit")) {
			game.EditorSetKit();
		}

    }
}
