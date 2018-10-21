using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXHolder : MonoBehaviour {

    //singleton structure
    public static SFXHolder singleton
    {
        get
        {
            if(instance == null)
                instance = FindObjectOfType<SFXHolder>();
            if(instance == null) {
                GameObject go = new GameObject("SFXHolder");
                instance = go.AddComponent<SFXHolder>();
            }
            return instance;
        }
    }

    private static SFXHolder instance;

    public static GameObject Spawn(GameObject effect, float timeOut = 0f) {
        return Spawn(effect, Vector3.zero, timeOut);
    }

    public static GameObject Spawn(GameObject effect, Vector3 position, float timeOut = 0f) {
        return Spawn(effect, position, Quaternion.identity, timeOut);
    }

    public static GameObject Spawn(GameObject effect, Vector3 position, Quaternion rotation, float timeOut = 0f) {
        GameObject go = Instantiate(effect, position, rotation, singleton.transform);
        if(timeOut > 0f && timeOut < float.PositiveInfinity) {
            SelfDestruct sd = go.AddComponent<SelfDestruct>();
            sd.time = timeOut;
        }
        return go;
    }

    public static GameObject Spawn(GameObject effect, Vector3 position, Quaternion rotation, Rigidbody rb, float timeOut = 0f) {
        GameObject go = Spawn(effect, position, rotation, timeOut);
        if(rb != null)
            go.transform.SetParent(rb.transform);
        return go;
    }

    public static void Clear() {
        for(int i = 0; i < singleton.transform.childCount; i++)
            Destroy(singleton.transform.GetChild(i));
    }
}
