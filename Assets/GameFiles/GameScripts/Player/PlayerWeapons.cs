using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapons : UFPlayerWeapons {

    MinerTool minerTool { get { return GetComponentInChildren<MinerTool>(true); } }
    PlayerTool[] allTools { get { return GetComponentsInChildren<PlayerTool>(true); } }

    private float timer;
    private const float AMP_TIME = 12f;

    public override void Reset() {
        minerTool.Reset();
    }

    public override bool PickupWeapon(UFItem item) {
        bool miningWeapon = item.type == UFItem.ItemType.Explosive;
        if(miningWeapon)
            minerTool.FoundWeapon();
        return true;
    }

    public override bool PickupAmmo(UFItem item) {
        bool miningAmmo = item.type == UFItem.ItemType.ExplosiveAmmo;
        if(miningAmmo)
            minerTool.AddAmmo(item);
        return true;
    }

    public override void DamageAmp() {
        foreach(PlayerTool tool in allTools)
            tool.SetAmplified(true);
        timer = AMP_TIME;
    }

    private void Update() {
        if(timer > 0f) {
            timer -= Time.deltaTime;
            if(timer <= 0f) {
                timer = 0f;
                foreach(PlayerTool tool in allTools)
                    tool.SetAmplified(false);
            }
        }
    }

    public float GetAmpFrac() {
        return timer / AMP_TIME;
    }
}
