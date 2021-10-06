
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class VRCCheckpoint : UdonSharpBehaviour {
    
    public VRCCheckpoint prev;
    private Animator anim;
    private bool activated;
    private Transform target;
    private AudioSource sound;

    private void Start() {
        anim = GetComponent<Animator>();
        sound = GetComponent<AudioSource>();

        activated = false;
        target = null;

        if(prev == null) {
            activated = true;
            anim.SetInteger("State", 1);
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
        if(!player.isLocal)
            return;

        if(target != null)
            player.TeleportTo(target.position, target.rotation);
        else if(!activated)
            Activate();
    }

    private void Activate() {
        activated = true;
        anim.SetInteger("State", 1);
        prev.SetNext(transform);
        sound.Play();
    }

    public void SetNext(Transform tg) {
        anim.SetInteger("State", 2);
        target = tg;
        if(prev != null)
            prev.SetNext(tg);
    }
}
