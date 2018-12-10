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

    private static UFPlayerInfo ufPlayerInfo;
    public static UFPlayerInfo playerInfo { get {
            if(ufPlayerInfo == null)
                ufPlayerInfo = singleton.GetComponentInChildren<UFPlayerInfo>();
            return ufPlayerInfo;
    } }

    private static UFGeoModder ufGeo;
    public static UFGeoModder geo { get {
            if(ufGeo == null)
                ufGeo = singleton.GetComponentInChildren<UFGeoModder>();
            return ufGeo;
    } }

    private static UFPlayerMovement ufPlayer;
    public static UFPlayerMovement player { get {
            if(ufPlayer == null) {
                UFPlayerMovement[] candidates = FindObjectsOfType<UFPlayerMovement>();
                foreach(UFPlayerMovement candidate in candidates) {
                    if(candidate.isMine) {
                        if(ufPlayer == null)
                            ufPlayer = candidate;
                        else
                            PhotonNetwork.Destroy(candidate.gameObject);
                    }
                }
            }
            return ufPlayer;
    } }

    [SerializeField]
    public List<IDRef> idDictionary;
    public List<UFRoom> rooms;
    

    public void Awake(){
        if(singleton != this) {
            Debug.LogWarning("Scene contained multiple UFLevel scripts, only 1 (active) UFLevel is allowed per scene.");
            Destroy(this.gameObject);
        }
    }

	public void Set(LevelData level) {
        //set up ID links
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

        //add photon view for critical syncing
        gameObject.AddComponent<PhotonView>();
    }

    public static void SyncTrigger(int id) {
        singleton.GetComponent<PhotonView>().RPC("SyncTrigger_RPC", PhotonTargets.AllBufferedViaServer, id);
    }

    /// <summary>
    /// Activate trigger with the given ID over the network, so it is synced
    /// with all players, even those who join late
    /// </summary>
    [PunRPC]
    private void SyncTrigger_RPC(int id) {
        GetByID(id).objectRef.GetComponent<UFTrigger>().SyncTrigger();
    }

    public static T GetPlayer<T>() where T : Component{
        UFPlayerMovement basePlayer = player;
        if(basePlayer == null)
            return null;
        return basePlayer.GetComponentInChildren<T>();
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

    public static void ClearRooms() {
        instance.rooms = null;
    }

    public static void AddRoom(UFRoom room) {
        if(instance.rooms == null)
            instance.rooms = new List<UFRoom>();
        instance.rooms.Add(room);
    }

    public static UFRoom GetRoom(Vector3 position) {
        for(int i = instance.rooms.Count - 1; i >= 0; i--) {
            if(instance.rooms[i].IsInside(position))
                return instance.rooms[i];
        }
        return null;
    }

    //dynamic level stuff

    private float countDownTimer;

    private void Update() {
        if(countDownTimer > 0f) {
            countDownTimer -= Time.deltaTime;
            if(countDownTimer <= 0f)
                countDownTimer = 0f;                
        }
    }

    public void SetCountDown(float value) {
        countDownTimer = value;
    }

    public float GetCountDownTime() {
        return countDownTimer;
    }
    
}
