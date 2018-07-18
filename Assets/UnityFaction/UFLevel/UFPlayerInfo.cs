using System.Collections.Generic;
using UFLevelStructure;
using UnityEngine;

public class UFPlayerInfo : MonoBehaviour {

    public string levelName, author;
    public PosRot playerStart;

    public Color defaultAmbient;
    public float ambChangeTime = 10f;

    public Color fogColor;
    public float fogStart, fogEnd;

    public bool multiplayer;
    public SpawnPoint[] spawnPoints;

    public Room[] rooms;
    public Camera skyCamera;


    public void Set(LevelData level) {
        this.levelName = level.name;
        this.author = level.author;
        this.playerStart = level.playerStart;
        this.multiplayer = level.multiplayer;
        this.spawnPoints = level.spawnPoints;
        this.fogStart = Mathf.Max(10f, level.nearPlane);
        this.fogEnd = Mathf.Max(fogStart + 10f, level.farPlane);
        this.defaultAmbient = level.ambientColor;
        this.fogColor = level.fogColor;

        List<Room> roomList = new List<Room>();
        foreach(Room room in level.staticGeometry.rooms) {
            Vector3 roomExtents = room.aabb.max - room.aabb.min;
            bool realRoom = roomExtents.x > 3f;
            realRoom &= roomExtents.z > 3f;
            realRoom &= roomExtents.y > 1.5f;

            //TODO use life value of rooms
            if(realRoom)
                roomList.Add(room);
        }
        this.rooms = roomList.ToArray();

        foreach(Room room in rooms) {
            if(room.isSkyRoom) {
                GameObject camG = new GameObject("SkyCamera");
                Vector3 skyPos = (room.aabb.min + room.aabb.max) / 2f;
                Vector3 skyDiagonal = room.aabb.max - room.aabb.min;
                camG.transform.SetParent(transform);
                camG.transform.position = skyPos;
                skyCamera = camG.AddComponent<Camera>();
                skyCamera.depth = -10;
                skyCamera.clearFlags = CameraClearFlags.SolidColor;
                skyCamera.backgroundColor = fogColor;
                skyCamera.farClipPlane = skyDiagonal.magnitude / 2f;
                break;
            }
        }
    }

    private void Start() {
        SetFog();
    }

    private void SetFog() {
        RenderSettings.fog = fogStart > 0f;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
    }

    private void SetRenderSettings() {
        RenderSettings.ambientLight = defaultAmbient;
        SetFog();
    }

    public void ApplyCameraSettings(Camera playerCamera) {
        if(skyCamera != null)
            //rely on sky camera to render sky room
            playerCamera.clearFlags = CameraClearFlags.Depth;
        else if(RenderSettings.fog){
            //aply solid color to represent thick fog
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = fogColor;
        }
        else
            //no clearing; time for trippy background effects
            playerCamera.clearFlags = CameraClearFlags.Nothing;

        //far clipping
        playerCamera.farClipPlane = fogEnd;
    }

    public void UpdateCamera(Camera playerCamera) {
        Color targetAmb = defaultAmbient;
        
        Room room;
        if(GetRoom(playerCamera.transform.position, out room)) {
            if(room.hasAmbientLight)
                targetAmb = room.ambientLightColor;

            //TODO apply remaining room effects
        }

        Color currentAmb = RenderSettings.ambientLight;
        float r = Time.deltaTime / ambChangeTime;
        RenderSettings.ambientLight = UFUtils.MoveTowards(currentAmb, targetAmb, r);

        if(skyCamera != null) {
            skyCamera.transform.rotation = playerCamera.transform.rotation;
            skyCamera.fieldOfView = playerCamera.fieldOfView;
        }
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

    

    public string GetLevelInfo() {
        return levelName + ", made by " + author;
    }

    public enum PlayerClass {
        Free, Bot, RedTeam, BlueTeam
    }

    private bool GetRoom(Vector3 position, out Room room) {
        foreach(Room r in rooms) {
            if(r.aabb.IsInside(position)) {
                room = r;
                return true;
            }
        }
        room = default(Room);
        return false;
    }

}
