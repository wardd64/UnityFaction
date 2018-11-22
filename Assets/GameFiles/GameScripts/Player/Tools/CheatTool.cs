using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatTool : PlayerTool {

    public TextMesh text;

    private int mode;
    private const int nbCheatModes = 2;

    public override void DoUpdate(bool mainFire, bool alt) {
        if(alt) {
            mode++;
            GetComponent<AudioSource>().Play();
        }
        if(mode >= nbCheatModes)
            mode = 0;

        Cheat(mainFire);
    }

    private void Cheat(bool active) {
        switch(mode) {

        case 0:
        text.text = "No clip\nHold fire to\nfly and phase\ntrough terrain";
        player.SetNoClip(active);
        break;

        case 1:
        text.text = "Infinite\nJumping";
        player.ResetDoubleJump();
        break;
        }

        if(mode != 0)
            player.SetNoClip(false);
    }

    private void OnDisable() {
        if(player != null)
            player.SetNoClip(false);
    }
}
