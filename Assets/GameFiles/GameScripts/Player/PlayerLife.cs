using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLife : UFPlayerLife {

    public GameObject livingBody, deadBody;

    private Camera playerCamera;
    private Vector3 localCameraPos;
    private float deadTimer;

    private const float AUTO_FINISH_DEATH = 3f;

    private PlayerMovement mov { get { return GetComponent<PlayerMovement>(); } }

    private void Awake() {
        playerCamera = this.GetComponentInChildren<Camera>();
        localCameraPos = playerCamera.transform.localPosition;
        Global.hud.gameObject.SetActive(true);

        if(!FindObjectOfType<MapFinish>() && !Global.InMainMenu())
            Debug.LogWarning("Current map does not contain finish!");
    }

    protected override void Update() {
        base.Update();

        if(!isDead)
            return;

        //dead timer
        if(deadTimer > 0f) {
            deadTimer += Time.deltaTime;

            bool finish = deadTimer >= AUTO_FINISH_DEATH;
            finish |= Global.input.GetKeyDown("jump");
            finish |= Global.input.GetKeyDown("UseCCP");
            finish |= Global.input.GetKeyDown("PlaceCCP");
            if(finish)
                FinishDeath();

            mov.RagdollUpdate();
        }
    }

    private void LateUpdate() {
        if(isDead && (deadTimer > 0f || deadBody.activeSelf))
            OrbitCameraUpdate();
    }

    private void OrbitCameraUpdate() {
        Vector2 rotXY = mov.MouseRotate();
        playerCamera.transform.rotation = Quaternion.Euler(-rotXY.y, rotXY.x, 0f);
        Vector3 basePoint = transform.TransformPoint(localCameraPos);
        Vector3 offDirection = playerCamera.transform.rotation * Vector3.back;

        Ray offRay = new Ray(basePoint, offDirection);
        RaycastHit hit;
        LayerMask rayMask = UFLevel.playerInfo.levelMask;
        rayMask &= ~UFLevel.playerInfo.playerMask;
        Physics.SphereCast(offRay, .25f, out hit, 5f, rayMask);
        float offDist = hit.collider != null ? hit.distance : 5f;

        playerCamera.transform.position = basePoint + offDirection * offDist;
    }

    /// <summary>
    /// Trigger death to continue to CCP mode
    /// </summary>
    public void CCPDie() {
        Die(DamageType.ccp);
        FinishDeath();
    }

    /// <summary>
    /// Trigger to death to respawn
    /// </summary>
    public void RespawnDie() {
        Die(DamageType.respawn);
        Respawn();
    }

    protected override void Die(DamageType type) {
        mov.SetRagdoll(true);
        health = 0f; armor = 0f;
        Global.hud.gameObject.SetActive(false);
        deadTimer = Time.deltaTime;
        GetComponentInChildren<UFPlayerMoveSounds>().Die();
        Global.hud.chat.DeathMessage(type);
        
    }

    private void FinishDeath() {
        deadTimer = 0f;

        if(Global.save.ccpAllowed) {
            livingBody.SetActive(false);
            deadBody.SetActive(true);
            GetComponent<CCPPlacer>().StartUsingCCP();
        }
        else
            Respawn();
    }

    public void Continue() {
        playerCamera.transform.localPosition = localCameraPos;

        livingBody.SetActive(true);
        deadBody.SetActive(false);
        mov.SetRagdoll(false);
        mov.Reset();

        SetBaseHealth();
        Global.hud.gameObject.SetActive(true);
        Global.match.CountReset();
    }

    /// <summary>
    /// Instantly puts player back at level spawn, alive.
    /// </summary>
    public void Respawn() {
        mov.Spawn();
        Continue();
    }
}
