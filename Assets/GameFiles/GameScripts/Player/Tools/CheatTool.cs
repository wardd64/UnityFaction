using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatTool : PlayerTool {

    public TextMesh text;

    private float holdingMain;
    private int mode;
    private Transform playerTarget;
    private const int nbCheatModes = 3;

    public override void DoUpdate(bool mainFire, bool alt) {
        if(alt) {
            mode++;
            GetComponent<AudioSource>().Play();
        }
        if(mode >= nbCheatModes)
            mode = 0;

        Cheat(mainFire);

        if(mainFire)
            holdingMain += Time.deltaTime;
        else
            holdingMain = 0f;
    }

    private void Cheat(bool active) {
        bool trigger = !active && holdingMain > 0 && holdingMain <= 2;

        switch(mode) {

        case 0:
        text.text = "No clip\nHold fire to\nfly and phase\ntrough terrain";
        player.SetNoClip(active);
        break;

        case 1:
        text.text = "Infinite\nJumping";
        player.ResetDoubleJump();
        break;

        case 2:
        if(playerTarget == null || trigger)
            playerTarget = FindNextPlayer();
        string targetName = "null";
        if(playerTarget != null) {
            targetName = playerTarget.GetComponent<PhotonView>().owner.NickName;
            text.text = "Teleport\nto Player\n" + targetName + "\n";
            int cpl = Mathf.CeilToInt(holdingMain * 15f);
            for(int i = 0; i < 15; i++)
                text.text += i < cpl ? '|' : '-';
            if(holdingMain > 1f) {
                UFLevel.player.transform.position = playerTarget.transform.position;
                holdingMain = 0f;
            }
        }
        else
            text.text = "No players\navailable";
        break;
        }

        if(mode != 0)
            player.SetNoClip(false);
    }

    private Transform FindNextPlayer() {
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
        bool useNext = playerTarget == null;

        foreach(PlayerMovement p in players) {
            if(p == UFLevel.player)
                useNext = true;
            else if(useNext)
                return p.transform;
        }
        return playerTarget;
    }

    private void OnDisable() {
        if(player != null)
            player.SetNoClip(false);
    }
}
