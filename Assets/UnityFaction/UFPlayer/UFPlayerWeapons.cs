using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerWeapons : MonoBehaviour {

    //called when player respawns and should lose all non-default weapons and ammo
    public virtual void Reset() {}

	public virtual bool PickupWeapon(UFItem item) {
        return false;
    }

    public virtual bool PickupAmmo(UFItem item) {
        return false;
    }

    public virtual void DamageAmp() {}
}
