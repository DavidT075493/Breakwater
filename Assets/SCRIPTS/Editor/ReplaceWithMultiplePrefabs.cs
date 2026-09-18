using UnityEngine;
using UnityEditor;

public class ReplaceWithMultiplePrefabs : EditorWindow
{
    bool parentNew = false;
    bool deleteOld = true;
    bool rotateX = false;
    bool rotateY = false;
    bool rotateZ = false;
    bool randomScale = false;
    float rx;
    float ry;
    float rz;
    float rs;
    Vector2 scrollPosition;

    [SerializeField] private GameObject[] theNewPrefabList;
    private int randomPrefab = 0;

    [MenuItem("Tools/Morne Tools/Replace With Multiple Prefabs")]
    static void CreateReplaceWithPrefab()
    {
        EditorWindow.GetWindow<ReplaceWithMultiplePrefabs>();
    }

    private void OnGUI()
    {
        ScriptableObject scriptableObj = this;
        SerializedObject serialObj = new SerializedObject(scriptableObj);
        SerializedProperty serialProp = serialObj.FindProperty("theNewPrefabList");

        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        //Step1
        GUILayout.BeginVertical("box");
        GUILayout.Label("Step1:", EditorStyles.boldLabel);
        GUILayout.Label("=====", EditorStyles.boldLabel);
        GUILayout.Label("From Assets, drag the new prefab(s) into the prefab list counter");
        GUILayout.Space(5);

        EditorGUILayout.PropertyField(serialProp, true);
        serialObj.ApplyModifiedProperties();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        //Step2
        GUILayout.BeginVertical("box");
        GUILayout.Label("Step2:", EditorStyles.boldLabel);
        GUILayout.Label("=====", EditorStyles.boldLabel);
        GUILayout.Label("In Scene/Hierarchy, select the old objects you want to replace");
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length, EditorStyles.boldLabel);
        GUILayout.EndVertical();

        GUILayout.Space(10);

        //Step3
        GUILayout.BeginVertical("box");
        GUILayout.Label("Step3:", EditorStyles.boldLabel);
        GUILayout.Label("=====", EditorStyles.boldLabel);
        GUILayout.Label("Options", EditorStyles.boldLabel);
        GUILayout.Space(10);
        parentNew = GUILayout.Toggle(parentNew, "Create parent object for new prefab(s)");
        GUILayout.Space(5);
        deleteOld = GUILayout.Toggle(deleteOld, "Delete old objects if possible");
        GUILayout.Space(5);
        randomScale = GUILayout.Toggle(randomScale, "Randomly scale 90% to 110%");
        GUILayout.Space(10);
        GUILayout.Label("Randomly rotate on axis:");
        GUILayout.BeginHorizontal("box");
        rotateX = GUILayout.Toggle(rotateX, "X");
        rotateY = GUILayout.Toggle(rotateY, "Y");
        rotateZ = GUILayout.Toggle(rotateZ, "Z");
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        //Step4
        GUILayout.BeginVertical("box");
        GUILayout.Label("Step4:", EditorStyles.boldLabel);
        GUILayout.Label("=====", EditorStyles.boldLabel);
        GUILayout.Label("Let's go!");

        GUILayout.Space(5);

        if (GUILayout.Button("Replace"))
        {
            GameObject prefabParent;

            prefabParent = new GameObject("PrefabHolder");
            if (parentNew == true)
            {
                Undo.RegisterCreatedObjectUndo(prefabParent, "Parent Replace With Prefabs");
            }

            prefabParent.name = "NewPrefabHolder";
            prefabParent.transform.position = new Vector3(0, 0, 0);

            var selection = Selection.gameObjects;

            if (selection.Length == 0)
            {
                Debug.LogError("No objects Selected in Step2");
                DestroyImmediate(prefabParent);
            }

            for (var i = selection.Length - 1; i >= 0; --i)
            {
                var selected = selection[i];

                GameObject newObject;

                if (theNewPrefabList.Length == 0)
                {
                    Debug.LogError("The New Prefab List is Empty in Step1");
                    DestroyImmediate(prefabParent);
                    break;
                }

                randomPrefab = Random.Range(0, theNewPrefabList.Length);
                newObject = (GameObject)PrefabUtility.InstantiatePrefab(theNewPrefabList[randomPrefab]);

                if (newObject == null)
                {
                    Debug.LogError("Error instantiating prefab");
                    break;
                }

                if (parentNew == false)
                {
                    Undo.RegisterCreatedObjectUndo(newObject, "Replace With Multiple Prefabs");
                }

                newObject.name = newObject.name + " (" + i + ")";

                if (parentNew)
                {

                    newObject.transform.parent = prefabParent.transform;
                }
                else
                {
                    newObject.transform.parent = selected.transform.parent;
                }

                newObject.transform.position = selected.transform.position;

                rx = rotateX ? Random.Range(0f, 360) : newObject.transform.eulerAngles.x;
                ry = rotateY ? Random.Range(0f, 360) : selected.transform.eulerAngles.y;
                rz = rotateZ ? Random.Range(0f, 360) : newObject.transform.eulerAngles.z;
                rs = randomScale ? Random.Range(0.9f, 1.1f) : 1;

                //Original rotation code before random check was added: //newObject.transform.eulerAngles = new Vector3 (newObject.transform.eulerAngles.x, selected.transform.eulerAngles.y, newObject.transform.eulerAngles.z);
                newObject.transform.eulerAngles = new Vector3(rx, ry, rz);

                //Original scale code before random check was added: //newObject.transform.localScale = selected.transform.localScale;
                newObject.transform.localScale = new Vector3(rs, rs, rs);
                newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());

                if (deleteOld)
                {
                    Undo.DestroyObjectImmediate(selected);
                }

            }

            if (parentNew == false)
            {
                DestroyImmediate(prefabParent);
            }
        }
        GUILayout.EndVertical();

        GUI.enabled = false;

        GUILayout.EndScrollView();
    }
}