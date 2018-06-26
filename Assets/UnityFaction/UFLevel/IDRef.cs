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

    public void SetObject(GameObject objectRef) {
        this.objectPath = GetTransformPath(objectRef.transform);
    }

    public GameObject objectRef { get {
        if(string.IsNullOrEmpty(objectPath))
            return null;
            if(GetTransformFromPath(objectPath) == null)
                Debug.Log(objectPath);
        return GetTransformFromPath(objectPath).gameObject;
    } }

    public int id;
    public string objectPath;
    public Type type;

    public enum Type {
        Decal, ClimbingRegion, Brush, Keyframe, Light,
        AmbSound, SpawnPoint, ParticleEmiter, GeoRegion,
        BoltEmiter, Item, Clutter, Event, Entity, Trigger
    }

    public override string ToString() {
        if(objectRef == null)
            return id + "-" + type + " (null)";
        return id + "-" + type + " (" + objectRef.name + ")"; 
    }

    /// <summary>
	/// Returns a path string that can be used to find the same transform 
	/// in a scene with the same hierarchy, when it has unloaded and loaded again.
	/// </summary>
	private string GetTransformPath(Transform t) {
        StringBuilder path = new StringBuilder();
        while(t != UFLevel.singleton.transform) {
            path.Insert(0, "/" + GetPathStep(t));
            t = t.parent;
        }
        path.Remove(0, 1);

        return path.ToString();
    }

    /// <summary>
    /// Returns Transform at given path. 
    /// Complementary to GetTransformPath(transform);
    /// </summary>
    private Transform GetTransformFromPath(string path) {
        Transform t = UFLevel.singleton.transform;
        while(path.Length > 0)
            t = ReadPathStep(t, ref path);

        return t;
    }

    private static string GetPathStep(Transform t) {
        string name = t.name;
        int length = name.Length;
        if(length > 99) {
            length = 99;
            name = name.Substring(0, 99);
        }
        return length.ToString().PadLeft(2, '0') + name;
    }

    private static Transform ReadPathStep(Transform parent, ref String path) {
        int length = int.Parse(path.Substring(0, 2));
        string match = path.Substring(2, length);

        if(path.Length > 2 + length)
            path = path.Substring(2 + length + 1);
        else
            path = "";

        for(int i = 0; i < parent.childCount; i++) {
            string name = parent.GetChild(i).name;
            if(name.Length > 99)
                name = name.Substring(0, 99);
            if(name == match)
                return parent.GetChild(i);
        }

        return null;
    }
}
