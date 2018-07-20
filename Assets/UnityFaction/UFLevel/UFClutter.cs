using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFClutter : MonoBehaviour {

    public bool isSwitch;

    public void Set(Clutter clutter) {
        string name = clutter.name;
        isSwitch = name.Contains("switch") || name.Contains("Console Button");
    }

	public void Activate(bool positive) {

    }

}
