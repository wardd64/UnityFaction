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

    public void Set(UFLevel level) {
        this.levelName = level.name;
        this.author = level.author;
        this.playerStart = level.playerStart;
        this.multiplayer = level.multiplayer;
        this.spawnPoints = level.spawnPoints;
        this.fogStart = level.nearPlane;
        this.fogEnd = level.farPlane;
        this.rooms = level.staticGeometry.rooms;
        this.defaultAmbient = level.ambientColor;
        this.fogColor = level.fogColor;

        //TODO cull useless rooms that are just floating brushes, for performance

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
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
    }

    private void ApplyCameraSettings(Camera playerCamera) {
        if(hasSkyRoom) {
            //TODO: make sky room
            playerCamera.clearFlags = CameraClearFlags.Nothing;
        }
        else {
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = fogColor;
        }
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
