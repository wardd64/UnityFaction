using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFPlayerWeapons : MonoBehaviour {

    public Camera liquidVisionCamera;
    public Renderer liquidVisionVignette;

    //called when player respawns and should lose all non-default weapons and ammo
    public virtual void Reset() {}

	public virtual bool PickupWeapon(UFItem item) {
        return false;
    }

    public virtual bool PickupAmmo(UFItem item) {
        return false;
    }

    public virtual void DamageAmp() {}

    public void SetLiquidVision(bool active, Color color) {
        if(liquidVisionCamera == null || liquidVisionVignette == null)
            return;

        float dist = liquidVisionCamera.nearClipPlane + 0.001f;
        liquidVisionVignette.gameObject.SetActive(active);
        liquidVisionVignette.material.SetColor("_TintColor", color);
        Vector2 clipExtents = UFUtils.GetNearClipExtents(liquidVisionCamera);
        liquidVisionVignette.transform.localScale = 2f * dist * new Vector3(clipExtents.x, clipExtents.y, 1f);
        liquidVisionVignette.transform.localPosition = new Vector3(0f, 0f, dist);
    }
}
