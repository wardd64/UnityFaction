using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : UFPlayerMovement {

    public Animator playerAnim;
    private Ragdoll rag;

    public void RagdollUpdate() {
        Rigidbody ragHip = playerAnim.GetComponentInChildren<Rigidbody>();
        UFRoom room = UFLevel.GetRoom(ragHip.position);
        if(room != null && room.InLiquid(ragHip.position)) {
            foreach(Rigidbody rb in playerAnim.GetComponentsInChildren<Rigidbody>()) {
                rb.AddForce(10f * Vector3.up);
                rb.velocity *= Mathf.Exp(-2f * Time.deltaTime);
            }
        }
    }

    public override void InButtonRange(KeyCode useKey) {
        Global.hud.InButtonRange(useKey);
    }

    protected override bool IgnoreInput() {
        return Global.igMenu.isOpen || Global.hud.chat.listenToInput;
    }

    protected override bool AllowShortJump() {
        return Global.save.allowShortJump;
    }

    protected override float GetSensitivity() {
        return Global.save.mouseSensFactor;
    }

    public override void Spawn() {
        base.Spawn();
        Global.save.SetPlayerStats(this);
        SetRagdoll(false);
        Global.match.CountReset();
    }

    public void SetRagdoll(bool active) {
        playerAnim.enabled = !active;
        this.GetComponent<CharacterController>().enabled = !active;

        foreach(Rigidbody rb in playerAnim.GetComponentsInChildren<Rigidbody>()) {
            rb.isKinematic = !active;
            rb.GetComponent<Collider>().enabled = active;
        }

        UnityEngine.Rendering.ShadowCastingMode mode = active ? 
            UnityEngine.Rendering.ShadowCastingMode.On :
            UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        playerAnim.GetComponentInChildren<SkinnedMeshRenderer>().shadowCastingMode = mode;

        this.enabled = !active;
    }

    protected override bool AllowDoubleJump() {
        if(!Global.save.doubleJumpAllowed)
            return false;
        return !doubleJumped;
    }

    protected override bool StabilizeDoubleJump() {
        return Global.save.stableJump;
    }

    protected override void SetAnimation(string name, bool boolValue = false, float floatValue = 0f) {

        if(!playerAnim.isActiveAndEnabled)
            return;

        switch(name) {

        case "Airborne":
        case "Crouch":
        case "Moving":
        playerAnim.SetBool(name, boolValue);
        break;

        case "MoveAngle":
        playerAnim.SetFloat(name, floatValue);
        break;

        case "Jump":
        playerAnim.SetTrigger(name);
        break;

        }
    }

    public void ResetDoubleJump() {
        doubleJumped = false;
    }
}
