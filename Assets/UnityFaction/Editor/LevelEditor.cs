using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UFLevel))]
public class LevelEditor : Editor {

    private static int findID;
    private static bool drawLinks;

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

            drawLinks = GUILayout.Toggle(drawLinks, "Draw links");
        }
    }

    private void OnSceneGUI() {
        if(drawLinks)
            DrawLinks();
    }

    private void DrawLinks() {
        UFEvent[] events = level.GetComponentsInChildren<UFEvent>();
        UFTrigger[] triggers = level.GetComponentsInChildren<UFTrigger>();
        UFMover[] movers = level.GetComponentsInChildren<UFMover>();

        for(int i = 0; i < level.idDictionary.Count; i++) {
            IDRef idRef = level.idDictionary[i];
            if(idRef == null)
                continue;

            switch(idRef.type) {

            case IDRef.Type.Event:
            foreach(int j in idRef.objectRef.GetComponent<UFEvent>().links)
                DrawLink(i, j);
            break;

            case IDRef.Type.Trigger:
            foreach(int j in idRef.objectRef.GetComponent<UFTrigger>().links)
                DrawLink(i, j);
            break;

            case IDRef.Type.Keyframe:
            foreach(UFLevelStructure.Keyframe k in idRef.objectRef.GetComponent<UFMover>().keys) {
                if(k.triggerID >= 0)
                    DrawLink(i, k.triggerID);
            }
            break;

            }
        }
    }

    private void DrawLink(int srcID, int dstID) {
        Handles.color = Color.blue;

        IDRef srcIDRef = level.idDictionary[srcID];
        IDRef dstIDRef = level.idDictionary[dstID];
        if(srcIDRef == null || srcIDRef.objectRef == null)
            return;
        if(dstIDRef == null || dstIDRef.objectRef == null)
            return;

        //draw main line
        Vector3 src = srcIDRef.objectRef.transform.position;
        Vector3 dst = dstIDRef.objectRef.transform.position;
        Handles.DrawLine(src, dst);

        //draw arrow tip to indicate direction
        Vector3 center = (src + dst) / 2f;
        Quaternion q = Quaternion.LookRotation(dst - src);
        Vector3 leftArrow = new Vector3(-1f, 0f, -1f);
        Vector3 rightArrow = new Vector3(1f, 0f, -1f);
        float arrowSize = (dst - src).magnitude / 20f;

        Handles.DrawLine(center, center + q * leftArrow * arrowSize);
        Handles.DrawLine(center, center + q * rightArrow * arrowSize);
    }
}
