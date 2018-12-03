using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SaveData : MonoBehaviour {

    new public AudioMixer audio;

    // Simple static values

    private const string PLAYER_NAME_SAVE = "PlayerName";
    public const string PLAYER_DEFAULT_NAME = "Default";
    public string playerName
    {
        get { return PlayerPrefs.GetString(PLAYER_NAME_SAVE, "Default"); }
        set {
            PlayerPrefs.SetString(PLAYER_NAME_SAVE, value);
            PhotonNetwork.playerName = value;
        }
    }

    /*
     * Difficulties are
     * 0 - casual
     * 1 - standard
     * 2 - brutal
     */
    private const string DIFFICULTY_SAVE = "Difficulty";
    public int difficulty
    {
        get { return PlayerPrefs.GetInt(DIFFICULTY_SAVE, 1); }
        set { PlayerPrefs.SetInt(DIFFICULTY_SAVE, value); }
    }

    public bool bonusToolsAllowed { get { return difficulty <= 0; } }
    public bool teleportAllowed { get { return difficulty <= 0; } }
    public bool standardToolsAllowed { get { return difficulty <= 1; } }
    public bool ccpAllowed { get { return difficulty <= 1; } }
    public bool doubleJumpAllowed { get { return difficulty <= 0; } }

    public void SetPlayerStats(PlayerMovement player) {
        int statsIndex = difficulty;
        if(overrideStats)
            statsIndex = 2;

        switch(statsIndex) {

        case 0:
        player.walkSpeed = 10f;
        player.airSpeed = 8f;
        player.swimSpeed = 5.5f;
        player.airSteeringMax = 16f;
        player.swimSteering = 12f;
        player.jumpHeight = 1.6f;
        player.crouchSpeed = 8f;
        break;

        case 1:
        player.walkSpeed = 9.5f;
        player.airSpeed = 6.5f;
        player.swimSpeed = 4f;
        player.airSteeringMax = 15f;
        player.swimSteering = 10f;
        player.jumpHeight = 1.35f;
        player.crouchSpeed = 7f;
        break;

        case 2:
        player.walkSpeed = 9f;
        player.airSpeed = 6f;
        player.swimSpeed = 3f;
        player.airSteeringMax = 14.5f;
        player.swimSteering = 8f;
        player.jumpHeight = 1.25f;
        player.crouchSpeed = 6f;
        break;

        default: Debug.LogError("Encountered unkown difficulty setting: " + difficulty); break;
        }
    }

    private const string MOUSE_SENSITIVITY_SAVE = "MouseSstv";
    public float mouseSensitivity
    {
        get { return PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_SAVE, .5f); }
        set { PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_SAVE, value); }
    }
    public float mouseSensFactor {
        get { return Mathf.Pow(10f, 2f * mouseSensitivity); }
    }

    private const string FOV_SAVE = "Fov";
    public float fov
    {
        get { return PlayerPrefs.GetFloat(FOV_SAVE, 90f); }
        set { PlayerPrefs.SetFloat(FOV_SAVE, value); }
    }

    private const string FLAG_SAVE = "Flags";
    private int flags
    {
        get { return PlayerPrefs.GetInt(FLAG_SAVE, 0); }
        set { PlayerPrefs.SetInt(FLAG_SAVE, value); }
    }
    public bool allowShortJump
    {
        get { return GetFlag(0); }
        set { SetFlag(0, value); }
    }
    public bool overrideStats
    {
        get { return GetFlag(1); }
        set { SetFlag(1, value); }
    }
    public bool stableJump
    {
        get { return GetFlag(2); }
        set { SetFlag(2, value); }
    }
    public bool slowScroll
    {
        get { return GetFlag(3); }
        set { SetFlag(3, value); }
    }

    private bool GetFlag(int position) {
        int mask = 1 << position;
        return (flags & mask) != 0;
    }

    private void SetFlag(int position, bool value) {
        int mask = 1 << position;
        if(value)
            flags |= mask;
        else
            flags &= ~mask;
    }

    public const string VOLUME_MASTER = "MasterVolume";
    public const string VOLUME_EFFECTS = "EffectsVolume";
    public const string VOLUME_MUSIC = "MusicVolume";
    public const string VOLUME_AMBIENT = "AmbientVolume";
    public const string VOLUME_UI = "UIVolume";
    public void SetVolume(string parameter, float value) {
        float db = (value - 1f) * 40f;
        if(value <= 0f)
            db = -80f;
        else if(value >= 1f)
            db = 0f;

        audio.SetFloat(parameter, db);
    }
    public float GetVolume(string parameter) {
        float db;
        if(audio.GetFloat(parameter, out db)) {
            if(db <= -40f)
                return 0f;
            else if(db >= 0f)
                return 1f;
            return (db / 40f) + 1f;
        }
        else
            return -1f;
    }

    //temporary saved values
    private string currentLevel;
    private float playTime;
    private float timeSinceFullReset;
    private int resets;

    //map records
    private RecordHolder records;
    private string recordFile = "UFMapRecords.dat";
    private string recordFilePath { get {
            return Application.persistentDataPath + "/" + recordFile;
    } }

    private void Awake() {
        PhotonNetwork.playerName = playerName;
    }

    public void InitializeRecords(string[] mapList) {
        records = UFUtils.Load(recordFilePath) as RecordHolder;
        if(records == null)
            records = new RecordHolder(mapList);
        else
            records.UpdateMaps(mapList);
        UFUtils.Save(recordFilePath, records);
    }

    public void SetRecordText(string map, Text recordText) {
        records.GetRecord(map).SetText(recordText);
    }

    public bool SetRecord(string map, int difficulty, float time, int retries) {
        MapRecord currentRecord = records.GetRecord(map);
        MapRecord newRecord = new MapRecord(difficulty, time, retries);
        if(newRecord.CompareTo(currentRecord) > 0) {
            records.SetRecord(map, newRecord);
            UFUtils.Save(recordFilePath, records);
            return true;
        }
        return false;
    }

    [Serializable]
    private class RecordHolder {
        Dictionary<string, MapRecord> data;

        public RecordHolder() {
            if(data == null)
                data = new Dictionary<string, MapRecord>();
        }

        public RecordHolder(string[] maps) {
            data = new Dictionary<string, MapRecord>();
            foreach(string map in maps) {
                if(string.IsNullOrEmpty(map))
                    continue;
                if(data.ContainsKey(map)) {
                    Debug.LogError("Map name was duplicated: " + map);
                    continue;
                }
                data.Add(map, new MapRecord());
            }
        }

        public void UpdateMaps(string[] maps) {
            foreach(string map in maps) {
                if(!data.ContainsKey(map))
                    data.Add(map, new MapRecord());
            }
        }

        public MapRecord GetRecord(string map) {
            if(string.IsNullOrEmpty(map))
                return new MapRecord();
            if(!data.ContainsKey(map)) {
                Debug.LogError("looking for map record that does not exist: " + map);
                return new MapRecord();
            }
            return data[map];
        }

        public void SetRecord(string map, MapRecord record) {
            data[map] = record;
        }
    }
    [Serializable]
    private class MapRecord : IComparable {
        private int difficulty;
        private float time;
        private int retries;

        public MapRecord(int difficulty, float time, int retries) {
            this.difficulty = difficulty;
            this.time = time;
            this.retries = retries;
        }

        /// <summary>
        /// Initialize a blank "unbeaten" record that is beaten by any real record
        /// </summary>
        public MapRecord() : this(-1, 0f, 0) { }

        public override string ToString() {
            switch(difficulty) {
            case 0: return "Beaten on Casual";
            case 1:
            if(retries <= 1)
                return "Beaten on Standard in one go";
            else
                return "Beaten on Standard using " + retries + " retries";
            case 2: return "Beaten on Brutal in " + UFUtils.GetTimeString(time, 1, 3599f);
            default: return "Unbeaten";
            }
        }

        /// <summary>
        /// Returns positive integer when this record beats the other record.
        /// </summary>
        public int CompareTo(object obj) {
            MapRecord other = obj as MapRecord;
            if(other == null)
                return 1;

            if(this.difficulty > other.difficulty)
                return 1;
            else if(this.difficulty < other.difficulty)
                return -1;

            switch(this.difficulty) {
            default: return 0;
            case 1:
            return other.retries - this.retries;
            case 2:
            return other.time.CompareTo(this.time);
            }
        }

        public void SetText(Text text) {
            text.text = this.ToString();
            text.color = GetDiffColor(difficulty);
        }
    }

    public static Color GetDiffColor(int difficulty) {
        switch(difficulty) {
            case 0: return Color.cyan;
            case 1: return Color.yellow;
            case 2: return Color.red;
            default: return new Color(.39f, .39f, .39f);
        }
    }
}
