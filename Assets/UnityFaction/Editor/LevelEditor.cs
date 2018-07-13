using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UFLevel))]
public class LevelEditor : Editor {

    private static int findID;

    private UFLevel level { get { return (UFLevel)target; } }
    public override void OnInspectorGUI() {

        DrawDefaultInspector();

        if(UFLevel.singleton != level)
            GUILayout.Label("This level is inactive.");
        else if(level.idDictionary == null)
            GUILayout.Label("ID Dictionary not initialized. ");
        else {
            GUILayout.Label("Number of available IDs: " + level.idDictionary.Count);

            findID = EditorGUILayout.IntField("Search ID: ", findID);

            IDRef obj = UFLevel.GetByID(findID);
            if(obj == null)
                GUILayout.Label("Invalid ID");
            else {
                GUILayout.Label("Found reference type: " + obj.type.ToString());
                if(obj.objectRef != null) {
                    GUILayout.Label(obj.objectRef.name);
                    if(GUILayout.Button("Select game object"))
                        Selection.activeGameObject = obj.objectRef;
                }
                else
                    GUILayout.Label("Object is not in scene.");
            }
        }
    }
}
