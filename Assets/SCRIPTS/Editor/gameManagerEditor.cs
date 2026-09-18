using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (gameManager))]
public class gameManagerEditor : Editor {

	public override void OnInspectorGUI() {
		gameManager game = (gameManager)target;

		DrawDefaultInspector();


		if (GUILayout.Button ("Generate Item IDs")) {
			game.GenerateItemIds();
		}
        if (GUILayout.Button("Load Recipes"))
        {
            game.LoadRecipes();
        }

    }
}
