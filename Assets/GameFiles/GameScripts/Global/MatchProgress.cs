using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchProgress : MonoBehaviour {

    public GameObject playerPrefab;

    //current map record
    private int lowestDifficulty;
    private float totalTime;
    private int totalResets;
    private bool recordFrozen;

    private static bool initialized = false;
    private float matchTimer;

    private const float STANDARD_MATCH_TIME = 1800f;
    private const float EXTEND_MATCH_TIME = 300f;

    public float timeLeft { get { return matchTimer; } }

    private void Awake() {
        SceneManager.sceneLoaded += InitializeMatch;
        initialized = true;
    }

    private void InitializeMatch(Scene scene, LoadSceneMode mode) {
        if(!Global.IsMatchScene(scene))
            return;

        if(!PhotonNetwork.inRoom) {
            PhotonNetwork.offlineMode = true;
            PhotonNetwork.CreateRoom(null);
        }

        if(UFLevel.player != null)
            return;

        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
        player.SetActive(true);
        player.name = "Controlled Player";

        lowestDifficulty = Global.save.difficulty;
        totalTime = 0f;
        totalResets = 0;
        recordFrozen = false;

        if(!PhotonNetwork.offlineMode)
            matchTimer = STANDARD_MATCH_TIME;

        Global.hud.StartState();
    }

    private void Update() {
        if(!initialized)
            Awake();

        if(!recordFrozen)
            totalTime += Time.deltaTime;

        if(!PhotonNetwork.offlineMode)
            CountDown();
    }

    public void ExtendMatch() {
        matchTimer += EXTEND_MATCH_TIME;
    }

    public void SkipMatch() {
        matchTimer = 0f;
        NextMap();
    }

    private void CountDown() {
        matchTimer -= Time.deltaTime;
        if(matchTimer <= 0f) {
            matchTimer = 0f;
            NextMap();
        }
    }

    public void Finish() {
        if(recordFrozen)
            return;

        string map = SceneManager.GetActiveScene().name;
        bool newRecord = Global.save.SetRecord(map, lowestDifficulty, totalTime, totalResets);
        recordFrozen = true;
        Global.hud.Finish(map, newRecord);
    }

    private void NextMap() {
        if(PhotonNetwork.isMasterClient)
            Global.levelLauncher.OnJoinedRoom();
    }

    public void CountReset() {
        if(!recordFrozen)
            totalResets++;
    }

    public void TrackDifficulty(int difficulty) {
        if(!recordFrozen)
            lowestDifficulty = Mathf.Min(lowestDifficulty, difficulty);
    }

    public int GetCurrentRecordDifficulty() {
        return lowestDifficulty;
    }

    public bool RecordIsFrozen() {
        return recordFrozen;
    }

    public void SetRecordText(Text text) {
        text.text = "Beating on " + GetRecordText();
        text.color = SaveData.GetDiffColor(lowestDifficulty);
    }

    private string GetRecordText() {
        switch(lowestDifficulty) {
        case 0: return "Casual";
        case 1:
        if(totalResets <= 1)
            return "Standard in one go";
        else
            return "Standard using " + totalResets + " retries";
        case 2: return "Brutal in " + UFUtils.GetTimeString(totalTime, 1, 3599f);
        default: return "No record";
        }
    }

    public void Restart() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        RestartSequence();
    }

    private IEnumerator RestartSequence() {
        yield return null;
        InitializeMatch(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
}
