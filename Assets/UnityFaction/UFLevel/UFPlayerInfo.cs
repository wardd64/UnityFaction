using System;
using System.Collections.Generic;
using UFLevelStructure;
using UnityEngine;

public class UFPlayerInfo : MonoBehaviour {

    //general RF level settings
    public string levelRFLPath;
    public string levelName, author;
    public PosRot playerStart;
    public Color fogColor, ambientColor;
    public bool useFog;
    public float fogStart;
    public float clipPlane;

    public bool multiplayer;
    public SpawnPoint[] spawnPoints;
    public Camera skyCamera;

    public LayerMask levelMask;
    public int playerLayer;
    public LayerMask skyMask;

    //dynamic getters
    public LayerMask playerMask { get { return LayerMask.GetMask(LayerMask.LayerToName(playerLayer)); } }

    //dynamic variables
    private float playerMissingTime;
    private int playerMissingFrames;
    private Vector3 angularVelocity;
    private Quaternion cameraRotation;

    private const float DEFAULT_ASPECT = 4f / 3f;
    private const float FP_FOV = 70f;

    public void Set(LevelData level, int levelLayer, int playerLayer, int skyLayer, 
        string rflPath, bool pcFog) {

        this.levelRFLPath = rflPath;

        this.levelName = level.name;
        this.author = level.author;
        this.playerStart = level.playerStart;
        this.multiplayer = level.multiplayer;
        this.spawnPoints = level.spawnPoints;

        this.levelMask = LayerMask.GetMask(LayerMask.LayerToName(levelLayer));
        this.playerLayer = playerLayer;
        this.skyMask = LayerMask.GetMask(LayerMask.LayerToName(skyLayer));

        this.useFog = level.nearPlane <= level.farPlane && level.farPlane > 0f;
        this.fogStart = 0f;
        this.clipPlane = level.farPlane;
        if(this.clipPlane <= 0f)
            this.clipPlane = 1000f;
        if(pcFog && level.nearPlane > 0f && level.nearPlane <= level.farPlane)
            Debug.LogWarning("Found non trivial value " + level.nearPlane +
                " for fog start (near plane). By Default RF behaviour this value was ignored. " +
                "Please set in the UFPlayerInfo script if you which to use the value.");
        if(level.nearPlane > 0f && level.nearPlane > level.farPlane)
            Debug.LogWarning("Value for fog start (near plane) was greater than the value " +
                "for the far clipping plane. As a result, by default RF behaviour, fog has " +
                "been disabled. Visit the UFPlayerInfo object if this was not intended.");

        this.fogColor = level.fogColor;
        this.ambientColor = level.ambientColor;

        bool foundSkyRoom = false;
        Room skyRoom = default(Room);

        AxisAlignedBoundingBox levelBox = level.staticGeometry.rooms[0].aabb;

        foreach(Room room in level.staticGeometry.rooms) {
            levelBox = AxisAlignedBoundingBox.Join(levelBox, room.aabb);

            if(room.isSkyRoom) {
                foundSkyRoom = true;
                skyRoom = room;
                continue;
            }

            float roomRadius = .5f * room.aabb.GetSize().magnitude;
            MakeEAX(room.eaxEffect, room.aabb.GetCenter(), roomRadius);
        }

        BoxCollider bc = gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = (levelBox.min + levelBox.max) / 2f;
        bc.size = levelBox.max - levelBox.min;

        if(foundSkyRoom) {
            GameObject camG = new GameObject("SkyCamera");
            Vector3 skyPos = (skyRoom.aabb.min + skyRoom.aabb.max) / 2f;
            Vector3 skyDiagonal = skyRoom.aabb.max - skyRoom.aabb.min;
            camG.transform.SetParent(transform);
            camG.transform.position = skyPos;
            skyCamera = camG.AddComponent<Camera>();
            skyCamera.depth = -10;
            skyCamera.clearFlags = CameraClearFlags.SolidColor;
            skyCamera.backgroundColor = fogColor;
            skyCamera.farClipPlane = skyDiagonal.magnitude / 2f;
            skyCamera.cullingMask = skyMask;
        }
    }

    private void MakeEAX(Room.EAXEffectType eax, Vector3 roomCenter, float roomRadius) {
        switch(eax) {
        case Room.EAXEffectType.none:
        case Room.EAXEffectType.generic:
        return;
        }

        GameObject g = new GameObject("RoomEAX_" + eax);
        g.transform.SetParent(this.transform);
        g.transform.position = roomCenter;
        AudioReverbZone effect = g.AddComponent<AudioReverbZone>();
        effect.maxDistance = roomRadius;
        effect.minDistance = roomRadius / 2f;
        effect.reverbPreset = GetReverbPreset(eax);
    }

    private AudioReverbPreset GetReverbPreset(Room.EAXEffectType effect) {
        string effectString = UFUtils.Capitalize(effect.ToString());
        if(Enum.IsDefined(typeof(AudioReverbPreset), effectString))
            return (AudioReverbPreset)Enum.Parse(typeof(AudioReverbPreset), effectString);
        
        switch(effect) {
        case Room.EAXEffectType.paddedcell: return AudioReverbPreset.PaddedCell;
        case Room.EAXEffectType.stonecorridor: return AudioReverbPreset.StoneCorridor;
        case Room.EAXEffectType.carpetedhallway: return AudioReverbPreset.CarpetedHallway;
        case Room.EAXEffectType.parkinglot: return AudioReverbPreset.ParkingLot;
        case Room.EAXEffectType.sewerpipe: return AudioReverbPreset.SewerPipe;
        default:
        Debug.LogWarning("Encountered unkown eax effect type: " + effect);
        return AudioReverbPreset.Off;
        }
    }

    private void Start() {
        SetRenderSettings();

        if(this.GetComponent<Collider>() == null)
            Debug.LogWarning("Player Info has no bound collider, please rebuild the level");

        cameraRotation = Quaternion.identity;
        angularVelocity = Vector3.zero;
        RenderSettings.ambientLight = ambientColor;
    }

    private void SetFog() {
        RenderSettings.fog = useFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = clipPlane;
    }

    public void ResetVision() {
        SetFog();
        RenderSettings.ambientLight = ambientColor;
    }

    private void SetRenderSettings() {
        SetFog();
        Application.targetFrameRate = 120;
    }

    public void ApplyCameraSettings(Camera playerCamera) {
        if(skyCamera != null)
            //rely on sky camera to render sky room
            playerCamera.clearFlags = CameraClearFlags.Depth;
        else if(useFog){
            //aply solid color to represent thick fog
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = fogColor;
        }
        else
            //no clearing; time for trippy background effects
            playerCamera.clearFlags = CameraClearFlags.Nothing;

        //far clipping
        if(clipPlane <= 0f) {
            Debug.LogWarning("Far clipping plane was set to invalid value! Reverting to default, 1000.");
            clipPlane = 1000f;
        }
        playerCamera.farClipPlane = clipPlane;
        if((skyMask & levelMask) != 0) {
            Debug.LogWarning("Sky and level mask settings overlap! As a result, sky layer will not " +
                "be culled from direct player view. please create and assign a unique layer for " +
                "the level, sky and player in the level builder to fix this issue");
        }
        else
            playerCamera.cullingMask &= ~skyMask; //remove skymask layers from direct player view
    }

    /*
    public void UpdateCamera(Camera playerCamera) {
        float fov = UFUtils.ScaleFOV(Global.save.fov, 1f / DEFAULT_ASPECT);

        float realAspect = (float) Screen.width / Screen.height;
        float aspect = realAspect * DEFAULT_ASPECT;
        playerCamera.fieldOfView = fov;

        playerCamera.aspect = aspect;
        UpdateFPCamera(playerCamera);

        if(skyCamera != null) {
            float rotAngle = -Time.deltaTime * angularVelocity.magnitude;
            cameraRotation *= Quaternion.AngleAxis(rotAngle, angularVelocity);

            skyCamera.transform.rotation = cameraRotation * playerCamera.transform.rotation;
            skyCamera.fieldOfView = fov;
            skyCamera.aspect = aspect;
        }
    }
    */

    /*
    private void Update() {
        if(UFLevel.player == null)
            return;

        playerMissingTime += Time.deltaTime;
        playerMissingFrames++;
        if(playerMissingTime > 1f && playerMissingFrames > 10)
            UFLevel.GetPlayer<UFPlayerLife>().TakeDamage(500f * Time.deltaTime, 
                UFPlayerLife.DamageType.exitLevel, true);
    }
    */

    private void UpdateFPCamera(Camera playerCamera) {
        if(playerCamera.transform.childCount <= 0)
            return;
        Camera fpCamera = playerCamera.transform.GetChild(0).GetComponent<Camera>();
        if(fpCamera == null)
            return;
        fpCamera.fieldOfView = FP_FOV;
        fpCamera.aspect = DEFAULT_ASPECT;
    }

    /// <summary>
    /// Returns random position and rotation where to spawn a player of the given class.
    /// </summary>
    public PosRot GetSpawn(PlayerClass playerClass) {
        if(!multiplayer || spawnPoints.Length == 0)
            return playerStart;

        List<SpawnPoint> candidates = new List<SpawnPoint>();
        foreach(SpawnPoint p in spawnPoints) {
            bool valid = false;
            switch(playerClass) {
            case PlayerClass.Free: valid = true; break;
            case PlayerClass.Bot: valid = p.bot; break;
            case PlayerClass.RedTeam: valid = p.redTeam; break;
            case PlayerClass.BlueTeam: valid = p.blueTeam; break;
            }
            if(valid)
                candidates.Add(p);
        }

        if(candidates.Count == 0)
            return GetSpawn(PlayerClass.Free);

        return UFUtils.GetRandom(candidates).transform.posRot;
    }

    /*
    private void OnTriggerStay(Collider other) {
        if(other.GetComponent<UFTriggerSensor>()) {
            playerMissingTime = 0f;
            playerMissingFrames = 0;
        }
    }
    */

    public string GetLevelInfo() {
        return levelName + ", made by " + author;
    }

    public enum PlayerClass {
        Free, Bot, RedTeam, BlueTeam
    }

    public void SetSkyboxRotation(string axis, float speed) {
        switch(axis.ToLower()) {
        case "x":
        angularVelocity = speed * Vector3.right;
        break;
        case "y":
        angularVelocity = speed * Vector3.up;
        break;
        case "z":
        angularVelocity = speed * Vector3.forward;
        break;
        }
    }

}
