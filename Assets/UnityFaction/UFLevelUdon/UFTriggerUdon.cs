
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UFTriggerUdon : UdonSharpBehaviour
{

    public bool requireUseKey, oneWay, auto;
    public int resetsRemaining;
    public float resetDelay;

    public float insideDelay, buttonDelay;
    public GameObject[] links;
    public GameObject switchObject;

    //dynamic variables
    public int signal;
    private bool permanent;
    private float insideTime, buttonTime;
    private bool inside, permanentTriggered;

    private void Start() {
        permanent = resetsRemaining == 1; //if true, press should sync with multiplayer
        if(auto)
            Trigger();
    }

    private void Update() {
        if(signal > 0) {
            Trigger();
            signal = 0;
        }

        if(inside) {
            insideTime += Time.deltaTime;
            if(insideTime >= insideDelay) {
                if(requireUseKey) {
                    buttonTime += Time.deltaTime;
                    if(switchObject != null)
                        FlagUdon(switchObject, "playerInRange", buttonTime >= buttonDelay);
                    else if(buttonTime >= buttonDelay)
                        Trigger();
                }
                else
                    Trigger();
            }
        }
        else{
            insideTime = 0f;
            buttonTime = 0f;
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if(!player.isLocal)
            return;
        inside = true;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player) {
        if(!player.isLocal)
            return;
        inside = false;
        UnTrigger();
    }

    private bool IsValid(Collider c) {
        return true;
        /*
        if(triggeredByWeapon)
            return false;
        if(triggeredByVehicle)
            return IsVehicle(c);
        return IsPlayer(c);
        */
    }

    /*
    private bool IsPlayer(Collider c) {
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        return uts != null && uts.IsPlayer();
    }

    private bool IsVehicle(Collider c) {
        UFTriggerSensor uts = c.GetComponent<UFTriggerSensor>();
        return uts != null && uts.type == UFTriggerSensor.Type.Vehicle;
    }
    */

    private void Trigger() {
        //sync this trigger press over the network if necessary
        if(permanent && !permanentTriggered) {
            permanentTriggered = true;
        }

        if(resetsRemaining > 0)
            resetsRemaining--;
        else if(resetsRemaining == 0)
            return;

        insideTime = -resetDelay;
        buttonTime = 0f;

        foreach(GameObject link in links)
            TriggerUdon(link, "signal", true);
    }

    private void UnTrigger() {
        insideTime = 0f;
        buttonTime = 0f;
        foreach(GameObject link in links)
            TriggerUdon(link, "deactivate", true);

        if(switchObject != null)
            FlagUdon(switchObject, "playerInRange", false);
    }

    private void TriggerUdon(GameObject g, string signal, bool positive) {
        UdonBehaviour ub = (UdonBehaviour)g.GetComponent(typeof(UdonBehaviour));
        ub.SetProgramVariable(signal, positive ? 1 : -1);
    }

    private void FlagUdon(GameObject g, string signal, bool positive) {
        UdonBehaviour ub = (UdonBehaviour)g.GetComponent(typeof(UdonBehaviour));
        ub.SetProgramVariable(signal, positive);
    }
}
