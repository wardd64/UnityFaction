using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLauncher : Photon.PunBehaviour {

    private List<string> availableScenes;
    private List<string> scenes { get {
            if(availableScenes == null) {
                availableScenes = new List<string>();
                for(int i = 1; i < SceneManager.sceneCountInBuildSettings; i++) {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    int lastSlash = scenePath.LastIndexOf("/");
                    availableScenes.Add(scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1));
                }
            }
            return availableScenes;
    } }

    public string multiplayerVersion = "1";
    private float mpSearchTime;
    private bool tryJoining;
    private string mapScene;

    private const float JOIN_INTERVAL = 3f;
    private Dictionary<string, int> mapPreferences;
    private bool tryingMP { get { return mpSearchTime > 0f; } }

    private void Start() {
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.autoCleanUpPlayerObjects = true;
        mapPreferences = new Dictionary<string, int>();
    }

    public bool SceneIsAvailable(string mapScene) {
        return scenes.Contains(mapScene);
    }

    public void Launch(string mapScene) {
        this.mapScene = mapScene;
        PhotonNetwork.offlineMode = true;
        PhotonNetwork.CreateRoom(null);
    }

    private void LaunchMultiplayer() {
        mpSearchTime = 0f;

        string map = GetPreferredMap();
        if(map.StartsWith("@"))
            PhotonNetwork.LoadLevel(int.Parse(map.Substring(1)));
        else
            PhotonNetwork.LoadLevel(map);
    }

    /// <summary>
    /// Set local map preference value for multiplayer purposes
    /// </summary>
    /// <param name="mapScene"></param>
    /// <param name="pref">1 to prioritize, -1 to veto, 0 for neutral.</param>
    public void SetMapPreference(string mapScene, int pref) {
        mapPreferences[mapScene] = pref;
    }

    private int GetMapPreference(string mapScene) {
        if(mapPreferences.ContainsKey(mapScene))
            return mapPreferences[mapScene];
        return 0;
    }

    private string GetPreferredMap() {
        if(mapPreferences.Count <= 0)
            return "@" + GetRandomMap();

        List<string> bestMaps = new List<string>();
        int bestValue = 1;
        foreach(KeyValuePair<string, int> entry in mapPreferences) {
            if(entry.Value > bestValue) {
                bestMaps = new List<string>();
                bestValue = entry.Value;
            }
            if(entry.Value == bestValue)
                bestMaps.Add(entry.Key);
        }

        if(bestMaps.Count <= 0)
            return "@" + GetRandomMap();

        return bestMaps[UnityEngine.Random.Range(0, bestMaps.Count)];
    }

    private int GetRandomMap() {
        return UnityEngine.Random.Range(1, SceneManager.sceneCountInBuildSettings);
    }

    private void Update() {
        if(tryingMP) {
            mpSearchTime += Time.deltaTime;
            if(tryJoining) {
                int nb = Mathf.FloorToInt((mpSearchTime - Time.deltaTime) / JOIN_INTERVAL);
                int nn = Mathf.FloorToInt(mpSearchTime / JOIN_INTERVAL);
                if(nn == nb + 1)
                    JoinMatch();
            }
        }
    }

    public void StartMultiplayer() {
        PhotonNetwork.offlineMode = false;
        mpSearchTime = float.Epsilon;

        if(PhotonNetwork.connected) {
            if(PhotonNetwork.insideLobby)
                SetReadyToJoin();
            else
                PhotonNetwork.JoinLobby();
        }
        else
            PhotonNetwork.ConnectUsingSettings(multiplayerVersion);
    }

    public override void OnConnectedToMaster() {
        if(!PhotonNetwork.offlineMode)
            PhotonNetwork.JoinLobby();
    }

    public override void OnConnectionFail(DisconnectCause cause) {
    }

    public override void OnDisconnectedFromPhoton() {
        PhotonNetwork.offlineMode = true;
    }

    public override void OnJoinedLobby() {
        SetReadyToJoin();
    }

    public void SetReadyToJoin() {
        MainMenu mm = FindObjectOfType<MainMenu>();
        mm.GetComponentInChildren<MultiplayerMenu>(true).SetReady();
    }

    public void JoinOrHostMatch() {
        tryJoining = false;
        if(!tryingMP)
            return;

        Global.hud.ConnectChat();
        PhotonNetwork.JoinRandomRoom();
    }

    public void JoinMatch() {
        tryJoining = true;
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        foreach(RoomInfo room in rooms) {
            bool condition = room.PlayerCount != room.MaxPlayers;
            if(condition) {
                Global.hud.ConnectChat();
                PhotonNetwork.JoinRoom(room.Name);
                return;
            }
        }
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg) {
        PhotonNetwork.CreateRoom(null);
    }

    public override void OnJoinedRoom() {
        Global.hud.ConnectChat();
        if(PhotonNetwork.offlineMode)
            PhotonNetwork.LoadLevel(mapScene);
        else
            if(PhotonNetwork.isMasterClient)
                LaunchMultiplayer();
    }

    public void StopMultiplayer() {
        PhotonNetwork.LeaveLobby();
        PhotonNetwork.Disconnect();
        mpSearchTime = 0f;
        tryJoining = false;
    }
}
