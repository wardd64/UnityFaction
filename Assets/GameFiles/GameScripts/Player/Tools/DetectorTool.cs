using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectorTool : PlayerTool {

    public Transform aimRig;
    public Transform gizmo;
    public TextMesh text;

    public AudioClip gizmoOnClip, gizmoOffCLip;

    public override void DoUpdate(bool mainFire, bool alt) {
        text.text = GetInfo(Raycast(aimRig));

        float scale = aimRig.localScale.z;
        gizmo.parent.localScale = new Vector3(1f, 1f, 1 / scale);
        gizmo.transform.rotation = Quaternion.identity;

        bool prevGizmoState = gizmo.gameObject.activeSelf;
        gizmo.gameObject.SetActive(mainFire);
        if(mainFire && !prevGizmoState)
            sound.PlayOneShot(gizmoOnClip);
        else if(!mainFire && prevGizmoState)
            sound.PlayOneShot(gizmoOffCLip);
    }

    private string GetInfo(RaycastHit hit) {
        if(hit.collider == null)
            return "Void";
        Transform target = hit.collider.transform;

        if(target.GetComponent<Collider>().isTrigger) {

            UFForceRegion force = target.GetComponent<UFForceRegion>();
            if(force != null) {
                switch(force.type) {
                case UFForceRegion.ForceType.AddVel:
                return "Push region";

                case UFForceRegion.ForceType.Climb:
                switch(force.soundType) {
                case UFLevelStructure.ClimbingRegion.ClimbingType.Fence:
                return "Climbable\nFence";
                case UFLevelStructure.ClimbingRegion.ClimbingType.Ladder:
                return "Ladder";
                case UFLevelStructure.ClimbingRegion.ClimbingType.Undefined:
                return "Climbable";
                }
                break;

                case UFForceRegion.ForceType.SetVel:
                return "Jump pad";
                }
            }

            UFLiquid liquid = target.GetComponent<UFLiquid>();
            if(liquid != null) {

                switch(liquid.type) {
                case UFLevelStructure.Room.LiquidProperties.LiquidType.Acid:
                return "Acid";
                case UFLevelStructure.Room.LiquidProperties.LiquidType.Lava:
                return "Lava";
                case UFLevelStructure.Room.LiquidProperties.LiquidType.Water:
                return "Water";
                case UFLevelStructure.Room.LiquidProperties.LiquidType.Undefined:
                return "Unkown\nLiquid";
                }
            }

            UFTrigger trig = target.GetComponent<UFTrigger>();
            if(trig != null) {

                if(trig.requireUseKey) {
                    if(trig.resetsRemaining == 0)
                        return "Spent\nButton\nArea";
                    else
                        return "Button\nArea";
                }

                foreach(int link in trig.links) {
                    IDRef idRef = UFLevel.GetByID(link);
                    switch(idRef.type) {

                    case IDRef.Type.Event:
                        UFEvent eventRef = idRef.objectRef.GetComponent<UFEvent>();

                        switch(eventRef.type) {

                        case UFLevelStructure.Event.EventType.Continuous_Damage:
                        bool insta = eventRef.int1 <= 0 || eventRef.int1 >= 100;
                        if(insta)
                            return "Death\nTrigger";
                        else
                            return "Damage\nArea";

                        case UFLevelStructure.Event.EventType.Teleport_Player:
                        case UFLevelStructure.Event.EventType.Teleport:
                        return "Teleporter";

                        }
                    break;
                    }
                }

                return "Trigger";
            }

            /*
            if(target.GetComponentInParent<UFPlayerInfo>())
                return "Level\nBounds";
            */

            if(target.GetComponentInParent<MapFinish>())
                return "Finish\nTrigger";
        }
        else {
            if(target.name.StartsWith("StaticVisible"))
                return "Solid";

            if(target.name.StartsWith("StaticIcy"))
                return "Slippery";

            if(target.name.StartsWith("StaticInvisible")) {
                if(hit.normal.y > .7f)
                    return "Invisible\nFloor";
                else if(hit.normal.y < -.7f)
                    return "Invisible\nCeiling";
                else
                    return "Invisible\nWall";
            }

            if(target.parent != null) {

                switch(target.parent.name) {

                case "Scrollers":
                    return "Scroller";

                case "PortalGeometry":
                    return "Illegal\nGeometry";

                case "Moving geometry":
                    return "Mover";

                }
            }

            UFClutter clutter = target.GetComponentInParent<UFClutter>();
            if(clutter != null) {
                if(clutter.isSwitch)
                    return "Switch";
                else if(clutter.life <= 0f)
                    return "Solid\nClutter";
                else
                    return "Destru-\nctible\nClutter";
            }

            UFDestructible destr = target.GetComponent<UFDestructible>();
            if(destr != null) {
                string life = UFUtils.GetShortFormat(destr.currentLife, 4);
                string maxLife = UFUtils.GetShortFormat(destr.life, 4);
                return "Destru-\nctible\n" + life + "/" + maxLife;
            }

            if(target.name.ToLower().Contains("help")) {
                return "Secret\nFix";
            }
        }

        Debug.Log("Could not recognize target: " + target);
        return "Unkown";
    }

    protected override bool ValidTrigger(Collider trigger) {
        UFItem item = trigger.GetComponent<UFItem>();
        return item == null;
    }
}
