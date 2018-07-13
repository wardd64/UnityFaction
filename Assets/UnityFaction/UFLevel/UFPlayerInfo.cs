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
    private bool hasSkyRoom;
    private Room skyRoom;

    public void Set(LevelData level) {
        this.levelName = level.name;
        this.author = level.author;
        this.playerStart = level.playerStart;
        this.multiplayer = level.multiplayer;
        this.spawnPoints = level.spawnPoints;
        this.fogStart = Mathf.Max(0f, level.nearPlane);
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
                hasSkyRoom = true;
                skyRoom = room;
                break;
            }
        }
    }

    private void Start() {
        //apply fog settings
        RenderSettings.fog = fogStart > 0f;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
    }

    public void ApplyCameraSettings(Camera playerCamera) {
        if(hasSkyRoom) {
            //TODO: make sky room
            playerCamera.clearFlags = CameraClearFlags.Nothing;
        }
        else if(RenderSettings.fog){
            //aply solid color to represent thick fog
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = fogColor;
        }
        else
            //no clearing; time for trippy background effects
            playerCamera.clearFlags = CameraClearFlags.Nothing;
    }

    private void UpdateCamera(Camera playerCamera) {
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
