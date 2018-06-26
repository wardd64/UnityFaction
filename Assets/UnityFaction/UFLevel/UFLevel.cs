using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFLevel : MonoBehaviour {

    [SerializeField]
    public static UFLevel singleton;

    [SerializeField]
    public List<IDRef> idDictionary;

    public UFLevel() {
        Singleton();
    }

	public void Set(LevelData level) {
        idDictionary = new List<IDRef>();
        foreach(Decal d in level.decals)
            SetID(d.cbTransform.transform.id, d, IDRef.Type.Decal);
        foreach(ClimbingRegion d in level.climbingRegions)
            SetID(d.cbTransform.transform.id, d, IDRef.Type.ClimbingRegion);
        foreach(Brush d in level.brushes)
            SetID(d.transform.id, d, IDRef.Type.Brush);
        foreach(MovingGroup m in level.movingGroups)
            foreach(UFLevelStructure.Keyframe d in m.keys)
                SetID(d.transform.id, d, IDRef.Type.Keyframe);
        foreach(UFLevelStructure.Light d in level.lights)
            SetID(d.transform.id, d, IDRef.Type.Light);
        foreach(AmbSound d in level.ambSounds)
            SetID(d.transform.id, d, IDRef.Type.AmbSound);
        foreach(SpawnPoint d in level.spawnPoints)
            SetID(d.transform.id, d, IDRef.Type.SpawnPoint);
        foreach(ParticleEmiter d in level.particleEmiters)
            SetID(d.transform.id, d, IDRef.Type.ParticleEmiter);
        foreach(GeoRegion d in level.geoRegions)
            SetID(d.transform.id, d, IDRef.Type.GeoRegion);
        foreach(BoltEmiter d in level.boltEmiters)
            SetID(d.transform.id, d, IDRef.Type.BoltEmiter);
        foreach(Item d in level.items)
            SetID(d.transform.id, d, IDRef.Type.Item);
        foreach(Clutter d in level.clutter)
            SetID(d.transform.id, d, IDRef.Type.Clutter);
        foreach(UFLevelStructure.Event d in level.events)
            SetID(d.transform.id, d, IDRef.Type.Event);
        foreach(Entity d in level.entities)
            SetID(d.transform.id, d, IDRef.Type.Entity);
        foreach(Trigger d in level.triggers)
            SetID(d.transform.id, d, IDRef.Type.Trigger);
    }

    private void Singleton() {
        if(singleton == null)
            singleton = this;
        else
            Destroy(this);
    }

    private void SetID(int id, object dataRef, IDRef.Type type) {
        if(idDictionary.Count <= id) {
            if(idDictionary.Capacity <= id)
                idDictionary.Capacity = id + 1;
            for(int i = idDictionary.Count; i <= id; i++)
                idDictionary.Add(null);
        }
        idDictionary[id] = new IDRef(id, type);
        Debug.Log("setting " + id + " to " + type);
    }

    public static IDRef GetByID(int id) {
        if(singleton == null)
            return null;
        if(id >= 0 && id < singleton.idDictionary.Count)
            return singleton.idDictionary[id];
        return null;
    }

    public static void SetObject(int id, GameObject objectRef) {
        IDRef obj = GetByID(id);
        if(obj != null)
            obj.SetObject(objectRef);
        else
            Debug.LogWarning("Tried to set object to id reference that does not exist: " + id);
    }

}
