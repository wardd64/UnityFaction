using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFRoom : MonoBehaviour {

    public int id;
    public AxisAlignedBoundingBox aabb;
    public bool isCold, isOutside, isAirlock, hasLiquid, hasAmbientLight, isSubRoom;
    public UFLevelStructure.Room.EAXEffectType eaxEffect;
    public Color ambientLightColor;
    public UFLiquid liquid;

    public void Set(Room room) {
        this.id = room.id;
        this.aabb = room.aabb;
        this.isCold = room.isCold;
        this.isOutside = room.isOutside;
        this.isAirlock = room.isAirlock;
        this.hasLiquid = room.hasLiquid;
        this.hasAmbientLight = room.hasAmbientLight;
        this.isSubRoom = room.isSubRoom;
        this.eaxEffect = room.eaxEffect;
        this.ambientLightColor = room.ambientLightColor;
        UFLevel.AddRoom(this);
    }

    public void SetLiquid(UFLiquid liquid) {
        this.liquid = liquid;
    }

    public bool IsInside(Vector3 position) {
        if(!aabb.IsInside(position))
            return false;

        //player is inside bounding box. Double check by looking at the direct environment.
        int roomHits = 0;
        for(int i = 0; i < 8; i++) {
            Vector3 dir = new Vector3(i & 1, (i << 1) & 1, (i << 2) & 1);
            dir = 2f * dir - Vector3.one;
            if(InRoom(position, dir))
                roomHits++;
        }
        roomHits += InRoom(position, Vector3.up) ? 1 : 0;
        roomHits += InRoom(position, Vector3.down) ? 1 : 0;

        return roomHits > 0;
    }

    private bool InRoom(Vector3 pos, Vector3 dir) {
        Ray ray = new Ray(pos, dir);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        float bestDistance = float.PositiveInfinity;
        UFRoom bestRoom = null;

        foreach(RaycastHit hit in hits) {
            if(hit.distance >= bestDistance)
                continue;
            UFRoom room = hit.collider.GetComponentInParent<UFRoom>();
            if(room != null) {
                bestRoom = room;
                bestDistance = hit.distance;
            }
        }

        return bestRoom == this;
    }

    public override string ToString() {
        return "Room " + id + (isSubRoom ? " sub " : " ") + aabb;
    }
}
