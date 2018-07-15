using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFTriggerSensor : MonoBehaviour {

    public Type type;

    public enum Type {
        None, Player, Vehicle
    }

	public bool IsPlayer() {
        return type == Type.Player;
    }
}
