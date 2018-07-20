using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFLevel : MonoBehaviour {

    public static UFLevel singleton { get {
            if(instance == null)
                instance = FindObjectOfType<UFLevel>();
            else if(!instance.gameObject.activeInHierarchy) {
                instance = null;
                instance = FindObjectOfType<UFLevel>();
            }

            return instance;
        } }
    private static UFLevel instance;

    [SerializeField]
    public List<IDRef> idDictionary;

    public void Awake(){
        if(singleton != this) {
            Debug.LogWarning("Scene contained multiple UFLevel scripts, only 1 (active) UFLevel is allowed per scene.");
            Destroy(this.gameObject);
        }
    }

	public void Set(LevelData level) {
        idDictionary = new List<IDRef>();
        foreach(Decal d in level.decals)
            SetID(d.cbTransform.transform.id, IDRef.Type.Decal);
        foreach(ClimbingRegion d in level.climbingRegions)
            SetID(d.cbTransform.transform.id, IDRef.Type.ClimbingRegion);
        foreach(Brush d in level.brushes)
            SetID(d.transform.id, IDRef.Type.Brush);
        foreach(MovingGroup m in level.movingGroups)
            foreach(UFLevelStructure.Keyframe d in m.keys)
                SetID(d.transform.id, IDRef.Type.Keyframe);
        foreach(UFLevelStructure.Light d in level.lights)
            SetID(d.transform.id, IDRef.Type.Light);
        foreach(AmbSound d in level.ambSounds)
            SetID(d.transform.id, IDRef.Type.AmbSound);
        foreach(SpawnPoint d in level.spawnPoints)
            SetID(d.transform.id, IDRef.Type.SpawnPoint);
        foreach(UFLevelStructure.ParticleEmitter d in level.particleEmitters)
            SetID(d.transform.id, IDRef.Type.ParticleEmitter);
        foreach(GeoRegion d in level.geoRegions)
            SetID(d.transform.id, IDRef.Type.GeoRegion);
        foreach(BoltEmitter d in level.boltEmitters)
            SetID(d.transform.id, IDRef.Type.BoltEmitter);
        foreach(Item d in level.items)
            SetID(d.transform.id, IDRef.Type.Item);
        foreach(Clutter d in level.clutter)
            SetID(d.transform.id, IDRef.Type.Clutter);
        foreach(UFLevelStructure.Event d in level.events)
            SetID(d.transform.id, IDRef.Type.Event);
        foreach(Entity d in level.entities)
            SetID(d.transform.id, IDRef.Type.Entity);
        foreach(PushRegion d in level.pushRegions)
            SetID(d.transform.id, IDRef.Type.PushRegion);
        foreach(UFTransform t in level.targets)
            SetID(t.id, IDRef.Type.Target);
        foreach(Trigger d in level.triggers)
            SetID(d.transform.id, IDRef.Type.Trigger);
    }

    public static T GetPlayer<T>() where T : Component{
        UFPlayerMovement mov = FindObjectOfType<UFPlayerMovement>();
        return mov.GetComponentInChildren<T>();
    }

    private void SetID(int id, IDRef.Type type) {
        if(idDictionary.Count <= id) {
            if(idDictionary.Capacity <= id)
                idDictionary.Capacity = id + 1;
            for(int i = idDictionary.Count; i <= id; i++)
                idDictionary.Add(null);
        }
        idDictionary[id] = new IDRef(id, type);
    }

    public static IDRef GetByID(int id) {
        if(singleton != null && id >= 0 && id < singleton.idDictionary.Count)
            return singleton.idDictionary[id];
        return null;
    }

    public static void SetObject(int id, GameObject objectRef) {
        IDRef idRef = GetByID(id);
        if(idRef != null)
            idRef.SetObject(objectRef);
        else
            Debug.LogWarning("Tried to set object to id reference that does not exist: " + id);
    }
}
