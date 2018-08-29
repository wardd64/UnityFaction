using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFTriggerSensor : MonoBehaviour {

    private Vector3 localPosition;
    public Type type;

    public enum Type {
        None, Player, Vehicle
    }

	public bool IsPlayer() {
        return type == Type.Player;
    }

    private void Start() {
        localPosition = transform.localPosition;
    }

    private void Update() {
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
    }
}
