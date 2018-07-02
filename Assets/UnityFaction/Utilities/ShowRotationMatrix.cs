using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ShowRotationMatrix : MonoBehaviour {

	
}

#if UNITY_EDITOR
[CustomEditor(typeof(ShowRotationMatrix))]
public class ShowRotationMatrixEditor : Editor {

    private static int findID;

    private UFLevel level { get { return (UFLevel)target; } }
    public override void OnInspectorGUI() {

        DrawDefaultInspector();

        Quaternion rot = ((ShowRotationMatrix)target).transform.rotation;
        Matrix4x4 mat = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
        GUILayout.Label(mat.ToString());
        
    }
}


#endif
