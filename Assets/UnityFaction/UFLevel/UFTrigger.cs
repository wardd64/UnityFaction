using System.Collections;
using System.Collections.Generic;
using UFLevelStructure;
using UnityEngine;

public class UFTrigger : MonoBehaviour {

    public bool triggeredByWeapon, triggeredByVehicle,
        requireUseKey, permanent, oneWay, auto;

    public KeyCode useKey;

    public int resetsRemaining;
    public float resetDelay;

    public float insideDelay, buttonDelay;
    public int[] links;
    public int switchRef;

    //dynamic variables
    float insideTime, buttonTime;
    bool inside;

	public void Set(Trigger trigger) {

        triggeredByWeapon = trigger.weaponActivates;
        requireUseKey = trigger.useKey;
        switchRef = trigger.useClutter;
        resetsRemaining = trigger.resets;
        resetDelay = trigger.resetDelay;
        oneWay = trigger.oneWay;
        links = trigger.links;
        auto = trigger.isAuto;
        triggeredByVehicle = trigger.inVehicle;
        insideDelay = Mathf.Max(0f, trigger.insideTime);
        buttonDelay = Mathf.Max(0f, trigger.buttonActiveTime);

        useKey = KeyCode.E;

        this.enabled = !trigger.disabled;

        if(!auto) {
            if(trigger.box) {
                BoxCollider bc = gameObject.AddComponent<BoxCollider>();
                bc.size = trigger.extents;
            }
            else {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = trigger.sphereRadius;
            }

            GetComponent<Collider>().isTrigger = true;
        }
    }

    private void Start() {
        permanent = resetsRemaining == 1; //if true, press should sync with multiplayer
        if(auto)
            Trigger();
    }

    private void Update() {
        if(inside) {
            insideTime += Time.deltaTime;
            if(insideTime >= insideDelay) {
                if(requireUseKey) {
                    if(Input.GetKey(useKey)) {
                        buttonTime += Time.deltaTime;
                        if(buttonTime >= buttonDelay)
                            Trigger();
                    }
                    else
                        buttonTime = 0f;
                }
                else
                    Trigger();
            }
        }
        else
            insideTime = 0f;
    }

    private void OnTriggerEnter(Collider other) {
        if(!IsValid(other))
            return;
        inside = true;

    }

    private void OnTriggerExit(Collider other) {
        if(!IsValid(other))
            return;
        inside = false;
    }

    private bool IsValid(Collider c) {
        if(triggeredByWeapon)
            return false;
        if(triggeredByVehicle)
            return IsVehicle(c);
        return IsPlayer(c);
    }

    private bool IsPlayer(Collider c) {
        return true; //TODO
    }

    private bool IsVehicle(Collider c) {
        return false; //TODO
    }

    private void Trigger() {
        insideTime = 0f;
        buttonTime = 0f;

        foreach(int link in links)
            Activate(link);

        if(switchRef >= 0) {
            IDRef swtch = UFLevel.GetByID(switchRef);
            UFClutter s = swtch.objectRef.GetComponent<UFClutter>();
            if(permanent)
                s.ActivatePermanent();
            else
                s.Activate();
        }
    }

    /// <summary>
    /// Tries to activate an object in the scene with the given ID.
    /// Logs warnings if the ID is invalid for any reason.
    /// </summary>
    public static void Activate(int id) {
        IDRef obj = UFLevel.GetByID(id);
        if(obj == null)
            Debug.LogWarning("Tried activating non existant ID: " + id);
        if(obj.objectRef == null)
            Debug.LogWarning("Tried activating ID that is not in the scene: " + obj.id + ", of type " + obj.type);


        switch(obj.type) {

        case IDRef.Type.Keyframe:
        obj.objectRef.GetComponentInParent<UFMover>().Activate();
        break;

        default:
        Debug.LogWarning("Tried activating object with unkown funcionality: " + obj + ", of type " + obj.type);
        break;
        }
    }
}
