using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerMoveSounds : MonoBehaviour {

    private UFPlayerMovement move;

    public AudioSource leftFootSound, rightFootSound;
    public AudioClip[] footstepClips_stone, footstepClips_metal,
        footstepClips_concrete, footstepClips_wood, footstepClips_gravel, footstepClips_pane;

    private FootstepContext.Type footstepContext;

    private void Awake() {
        move = transform.GetComponentInParent<UFPlayerMovement>();
        footstepContext = FootstepContext.Type.stone;
    }

    /// <summary>
    /// Update foot step context to match the sound that should be produced by the
    /// given ground collider. This works by searching for a FootstepContext 
    /// script attached to the object's hierarchy.
    /// </summary>
    public void SetLastGroundObject(Collider collider) {
        FootstepContext c = collider.transform.GetComponentInParent<FootstepContext>();
        if(c != null)
            footstepContext = c.type;
        else
            footstepContext = FootstepContext.Type.stone;
    }

    public void FootstepL() {
        PlayFootStep(false);
    }

    public void FootstepR() {
        PlayFootStep(true);
    }

    private void PlayFootStep(bool right, bool maxForce = false) {
        AudioClip[] clips;
        switch(footstepContext) {
        case FootstepContext.Type.stone:
        clips = footstepClips_stone;
        break;
        case FootstepContext.Type.metal:
        clips = footstepClips_metal;
        break;
        case FootstepContext.Type.wood:
        clips = footstepClips_wood;
        break;
        case FootstepContext.Type.concrete:
        clips = footstepClips_concrete;
        break;
        case FootstepContext.Type.gravel:
        clips = footstepClips_gravel;
        break;
        case FootstepContext.Type.pane:
        clips = footstepClips_pane;
        break;
        default:
        Debug.LogError("Encountered unknown footstep context: " + footstepContext);
        return;
        }

        if(clips == null || clips.Length <= 0)
            return;

        int spill = clips.Length / 2;
        int index;
        if(right)
            index = Random.Range(spill, clips.Length);
        else
            index = Random.Range(0, spill);

        AudioSource sound = right ? rightFootSound : leftFootSound;
        if(maxForce)
            sound.volume = 1f;
        else
            sound.volume = 0.3f + 0.7f * Mathf.Clamp01(move.speed / 5f);
        sound.clip = clips[index];
        sound.Play();
    }

    public void Jump() {
        PlayFootStep(false, true);
        PlayFootStep(true, true);
    }

    private void PlayOneShot(AudioSource sound, AudioClip clip) {
        sound.Stop();
        sound.volume = 1f;
        sound.loop = false;
        sound.clip = clip;
        sound.Play();
    }
}
