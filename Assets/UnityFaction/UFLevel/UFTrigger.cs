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
    bool inside, triggeredOnce;

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
                    bool key = buttonTime <= 0f && Input.GetKeyDown(useKey);
                    key |= buttonTime > 0f && Input.GetKey(useKey);
                    if(key) {
                        buttonTime += Time.deltaTime;
                        if(buttonTime >= buttonDelay)
                            Trigger();
                    }
                    else
                        buttonTime = 0f;

                    if(resetsRemaining != 0)
                        UFLevel.GetPlayer<UFPlayerMovement>().InButtonRange(useKey);
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
        UnTrigger();
    }

    private bool IsValid(Collider c) {
        if(triggeredByWeapon)
            return false;
        if(triggeredByVehicle)
            return IsVehicle(c);
        return IsPlayer(c);
    }

    private bool IsPlayer(Collider c) {
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        return uts != null && uts.IsPlayer();
    }

    private bool IsVehicle(Collider c) {
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        return uts != null && uts.type == UFTriggerSensor.Type.Vehicle;
    }

    private void Trigger() {
        if(resetsRemaining > 0)
            resetsRemaining--;
        else if(resetsRemaining == 0)
            return;

        insideTime = -resetDelay;
        buttonTime = 0f;

        foreach(int link in links)
            Activate(link);

        if(switchRef >= 0) {
            IDRef swtch = UFLevel.GetByID(switchRef);
            UFClutter s = swtch.objectRef.GetComponent<UFClutter>();
            s.Activate(true);
        }
    }

    private void UnTrigger() {
        insideTime = 0f;
        buttonTime = 0f;
        foreach(int link in links)
            Deactivate(link);
    }

    /// <summary>
    /// Tries to activate an object in the scene with the given ID.
    /// Logs warnings if the ID is invalid for any reason.
    /// Set positive to false to deactivate.
    /// </summary>
    public static void Activate(int id, bool positive = true) {

        IDRef obj = TryGetObject(id);
        if(obj == null) {
            Debug.LogWarning("Could not activated id " + id + " since it does not exist!");
            return;
        }

        try {
            switch(obj.type) {

            case IDRef.Type.Trigger:
            obj.objectRef.GetComponent<UFTrigger>().Trigger();
            break;

            case IDRef.Type.Keyframe:
            obj.objectRef.GetComponentInParent<UFMover>().Activate(positive);
            break;

            case IDRef.Type.Event:
            obj.objectRef.GetComponent<UFEvent>().Activate(positive);
            break;

            case IDRef.Type.ParticleEmitter:
            obj.objectRef.GetComponent<UFParticleEmitter>().Activate(positive);
            break;

            case IDRef.Type.BoltEmitter:
            obj.objectRef.GetComponent<UFBoltEmitter>().Activate(positive);
            break;

            case IDRef.Type.Clutter:
            obj.objectRef.GetComponent<UFClutter>().Activate(positive);
            break;

            case IDRef.Type.Light:
            obj.objectRef.GetComponent<UnityEngine.Light>().enabled = positive;
            break;

            case IDRef.Type.AmbSound:
            AudioSource sound = obj.objectRef.GetComponent<AudioSource>();
            if(positive)
                sound.Play();
            else
                sound.Stop();
            break;

            case IDRef.Type.Brush:
            obj.objectRef.GetComponent<UFMover>().Activate(positive);
            break;

            default:
            Debug.LogWarning("Tried activating object with unkown funcionality: " 
                + obj + ", of type " + obj.type);
            break;
            }

        }
        catch(System.NullReferenceException) {
            Debug.LogError("Failed to activate id " + id + ", of type "
                + obj.type + ", since it had an unexpected structure.");
        }
    }

    public static void Deactivate(int id) {
        IDRef obj = TryGetObject(id);
        if(obj == null)
            return;

        switch(obj.type) {

        case IDRef.Type.Event:
        obj.objectRef.GetComponent<UFEvent>().Deactivate();
        break;
        }
    }

    private static IDRef TryGetObject(int id) {
        IDRef toReturn = UFLevel.GetByID(id);
        if(toReturn == null) {
            Debug.LogWarning("Tried to link to non existant ID: " + id);
            return null;
        }

        GameObject obj = toReturn.objectRef;
        if(obj == null) {
            Debug.LogWarning("Tried to link to ID that is not in the scene: " + toReturn.id + ", of type " + toReturn.type);
            return null;
        }

        return toReturn;
    }
}
