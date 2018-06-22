using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFMover : MonoBehaviour {

    private AudioSource sound;

    public UFLevelStructure.Keyframe[] keys;

    public bool isDoor, startsBackwards, rotateInPlace,
        useTravTimeAsSpd, forceOrient, noPlayerCollide;

    public MovingGroup.MovementType type;

    public AudioClip startClip, loopClip, stopClip, closeClip;
    public float startVol, loopVol, stopVol, closeVol;

    public void Set(MovingGroup group) {
        
        keys = group.keys;
        type = group.type;

        //flags
        isDoor = group.isDoor;
        startsBackwards = group.startsBackwards;
        rotateInPlace = group.rotateInPlace;
        useTravTimeAsSpd = group.useTravTimeAsSpd;
        forceOrient = group.forceOrient;
        noPlayerCollide = group.noPlayerCollide;        

        //audio
        startVol = group.startVol;
        loopVol = group.loopVol;
        stopVol = group.stopVol;
        closeVol = group.closeVol;
        // (clips themselves are assigned by level builder)

        sound = gameObject.AddComponent<AudioSource>();
    }

    public void PlayClip(AudioClip clip, float volume) {
        sound.clip = clip;
        sound.volume = volume;
        sound.Play();
    }
}
