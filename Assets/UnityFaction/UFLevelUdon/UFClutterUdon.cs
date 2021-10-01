
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UFClutterUdon : UdonSharpBehaviour {
    
    //dynamic
    private AudioSource sound;
    public GameObject trigger;
    public bool playerInRange;

    private void Start() {
        sound = GetComponent<AudioSource>();
    }

    public override void Interact() {
        if(playerInRange) {
            if(sound != null)
                sound.Play();
            UdonBehaviour ub = (UdonBehaviour)trigger.GetComponent(typeof(UdonBehaviour));
            ub.SetProgramVariable("signal", 1);
        }
    }

}
