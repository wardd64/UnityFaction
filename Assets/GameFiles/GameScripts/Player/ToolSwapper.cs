using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSwapper : MonoBehaviour {

    public PlayerTool[] tools;
    private int toolIdx;
    private int scroll;

    private PlayerTool activeTool { get { return tools[toolIdx]; } }
    private PlayerLife player { get { return GetComponentInParent<PlayerLife>(); } }

    private void Update() {
        bool inputAllowed = !Global.igMenu.isOpen;
        inputAllowed &= !player.isDead;
        bool mainFire = inputAllowed && Global.input.GetKey("Fire");
        bool alt = inputAllowed && Global.input.GetKeyDown("AltFire");
        scroll += inputAllowed ? Mathf.FloorToInt(Input.mouseScrollDelta.y) : 0;
        int effectiveScroll = scroll;

        if(Global.save.slowScroll) {
            effectiveScroll /= 5;
            scroll %= 5;
        }
        else
            scroll = 0;

        ChangeTool(effectiveScroll);

        activeTool.DoUpdate(mainFire, alt);

        GetComponent<Camera>().enabled ^= Input.GetKeyDown(KeyCode.Alpha1);
    }

    private void ChangeTool(int amount) {
        if(amount == 0 && !activeTool.CanUse())
            amount = 1;
        for(int i = 0; i < Mathf.Abs(amount); i++)
            ChangeTool(amount < 0);

        bool active = !player.isDead;

        for(int i = 0; i < tools.Length; i++)
            tools[i].gameObject.SetActive(active && toolIdx == i);

        if(amount != 0)
            Global.hud.ChangeTool(tools, toolIdx);
    }

    private void ChangeTool(bool forward) {
        for(int i = 0; i < tools.Length; i++) {
            if(forward) {
                toolIdx += 1;
                if(toolIdx >= tools.Length)
                    toolIdx = 0;
            }
            else {
                toolIdx -= 1;
                if(toolIdx < 0)
                    toolIdx = tools.Length - 1;
            }
           
            if(activeTool.CanUse())
                return;
        }
        
    }
}
