using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFClutter : MonoBehaviour {

    public bool isSwitch;

    public void Set(Clutter clutter) {
        string name = clutter.name.ToLower();
        isSwitch = name.Contains("switch") || name.Contains("console button");

        bool hasPrefabColliders = GetComponentInChildren<Collider>() != null;
        if(!hasPrefabColliders) {
            foreach(MeshFilter mf in GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>();
        }
    }

	public void Activate(bool positive) {
        if(!isSwitch) {
            Debug.LogError("Trying to activate clutter which is not a switch: " + name);
            return;
        }

        //TODO switcheroo
    }

}
