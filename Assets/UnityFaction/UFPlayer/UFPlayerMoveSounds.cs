using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerMoveSounds : MonoBehaviour {

    private UFPlayerMovement move;

    public AudioClip weaponClip, powerupClip, invulnClip, damAmpClip;
    public AudioClip dieClip;

    public AudioSource playerSound, itemSound;
    public AudioSource leftFootSound, rightFootSound;
    public AudioClip[] footstepClips_solid, footstepClips_metal,
        footstepClips_water, footstepClips_ice, footstepClips_gravel, 
        footstepClips_glass, footstepClips_brokenGlass;

    private FootstepContext.Type footstepContext;

    private void Awake() {
        move = transform.GetComponentInParent<UFPlayerMovement>();
        footstepContext = FootstepContext.Type.gravel;
    }

    public void PickUpDamageAmp() {
        PlayOneShot(itemSound, damAmpClip);
    }

    public void PickUpInvuln() {
        PlayOneShot(itemSound, invulnClip);
    }

    public void PickUpWeapon() {
        PlayOneShot(itemSound, weaponClip);
    }

    public void PickUpPowerup() {
        PlayOneShot(itemSound, powerupClip);
    }

    public void Die() {
        PlayOneShot(playerSound, dieClip);
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
        else {
            string colName = collider.name.ToLower();
            if(colName.Contains("ice") || colName.Contains("icy"))
                footstepContext = FootstepContext.Type.ice;
            else
                footstepContext = FootstepContext.Type.gravel;
        }
    }

    public void FootstepL() {
        PlayFootStep(false);
    }

    public void FootstepR() {
        PlayFootStep(true);
    }

    private void PlayFootStep(bool right, bool maxForce = false) {
        AudioClip[] clips = GetClipPool(footstepContext);

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

    private AudioClip[] GetClipPool(FootstepContext.Type type) {
        switch(type) {
        case FootstepContext.Type.solid: return footstepClips_solid;
        case FootstepContext.Type.metal: return footstepClips_metal;
        case FootstepContext.Type.water: return footstepClips_water;
        case FootstepContext.Type.ice: return footstepClips_ice;
        case FootstepContext.Type.gravel: return footstepClips_gravel;
        case FootstepContext.Type.glass: return footstepClips_glass;
        case FootstepContext.Type.brokenGlass: return footstepClips_brokenGlass;

        default:
        Debug.LogError("Encountered unknown footstep context: " + type);
        return null;
        }
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
