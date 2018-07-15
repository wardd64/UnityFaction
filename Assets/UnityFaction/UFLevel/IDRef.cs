using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class IDRef{

    public IDRef(int id, Type type) {
        this.id = id;
        this.type = type;
    }

    public void SetObject(GameObject obj) {
        this.objectRef = obj;
    }

    public int id;
    public GameObject objectRef;
    public Type type;

    public enum Type {
        None, Decal, ClimbingRegion, Brush, Keyframe,
        Light, AmbSound, SpawnPoint, ParticleEmitter,
        GeoRegion, BoltEmitter, Item, Clutter, Event,
        Entity, Trigger, PushRegion
    }

    public override string ToString() {
        if(objectRef == null)
            return id + "-" + type + " (null)";
        return id + "-" + type + " (" + objectRef.name + ")"; 
    }
}
