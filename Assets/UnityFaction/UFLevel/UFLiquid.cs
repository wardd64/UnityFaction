using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFLiquid : MonoBehaviour {

    public Room.LiquidProperties.LiquidType type;
    public float visibility;
    public float alpha;
    public Color color;

    //private UFPlayerMovement player;
    private int nbCols;

    public float absoluteY { get {
            BoxCollider bc = this.GetComponent<BoxCollider>();
            return transform.position.y + bc.center.y + (bc.size.y / 2f);
        } }

    public void Set(Room room) {
        Vector3 center = -transform.position + (.5f * (room.aabb.min + room.aabb.max));
        Vector3 extents = room.aabb.max - room.aabb.min;

        Room.LiquidProperties liquid = room.liquidProperties;

        float depth = liquid.depth;
        float y = -transform.position.y + room.aabb.min.y + depth / 2f;

        BoxCollider bc = gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(center.x, y, center.z);
        bc.size = new Vector3(extents.x, depth, extents.z);
        bc.isTrigger = true;

        alpha = liquid.alpha / 255f;
        color = liquid.color;
        type = liquid.type;
        visibility = liquid.visibility;
    }

    /*
    private void Update() {
        if(player == null)
            return;

        UFRoom room = GetComponentInParent<UFRoom>();
        Vector3 playerPos = player.transform.position + .8f * Vector3.up;
        if(!room.IsInside(playerPos))
            return;

        player.SwimState(this);
        ApplyDPS(player.GetComponent<UFPlayerLife>(), type);
    }

    private void OnTriggerEnter(Collider other) {
        if(other.GetComponent<UFTriggerSensor>() && nbCols++ == 0)
            EnterLiquid();
    }

    private void EnterLiquid() {
        this.player = UFLevel.GetPlayer<UFPlayerMovement>();
    }

    private void OnTriggerExit(Collider other) {
        if(other.GetComponent<UFTriggerSensor>() && --nbCols == 0)
            ExitLiquid();
    }

    private void ExitLiquid() {
        this.player.JumpOutLiquid();
        this.player = null;
    }
    */

    /*
    private static void ApplyDPS(UFPlayerLife player, Room.LiquidProperties.LiquidType type) {
        switch(type) {

        case Room.LiquidProperties.LiquidType.Lava:
        player.TakeDamage(10f * Time.deltaTime, UFPlayerLife.DamageType.Fire, true);
        break;

        case Room.LiquidProperties.LiquidType.Acid:
        player.TakeDamage(5f * Time.deltaTime, UFPlayerLife.DamageType.Acid, true);
        break;
        }
    }
    */

    public void SetLiquidVision() {
        RenderSettings.fog = true;
        RenderSettings.fogColor = color;
        RenderSettings.fogStartDistance = 0f;
        RenderSettings.fogEndDistance = visibility;
        RenderSettings.ambientLight = color;
    }
}
