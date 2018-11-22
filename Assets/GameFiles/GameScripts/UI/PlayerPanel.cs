using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour {

    public Dropdown difficultyDropdown;
    public Text difficultyPropertyText;
    public InputField playerNameInput;
    public RectTransform keyTargetParent;
    public Toggle shortJumpToggle, overrideStatsToggle, stabilizeDoubleJumpToggle;
    public Text warningText;
    public Slider masterVolumeSlider, effectsVolumeSlider, musicVolumeSlider, ambientVolumeSlider;

    private bool inLevel { get { return UFLevel.singleton != null; } }
    private bool initialized = false;

    private void LoadProperties() {
        playerNameInput.text = Global.save.playerName;
        difficultyDropdown.value = Global.save.difficulty;
        ChangeDifficulty();
        SetKeyBindings();
        shortJumpToggle.isOn = Global.save.allowShortJump;
        overrideStatsToggle.isOn = Global.save.overrideStats;
        stabilizeDoubleJumpToggle.isOn = Global.save.stableJump;
        LoadVolume();
        initialized = true;
    }

    private void SaveProperties() {
        if(LoweringRecord())
            Global.match.TrackDifficulty(difficultyDropdown.value);

        Global.save.playerName = FilterName(playerNameInput.text);
        Global.save.difficulty = difficultyDropdown.value;
        Global.save.allowShortJump = shortJumpToggle.isOn;
        Global.save.overrideStats = overrideStatsToggle.isOn;
        Global.save.stableJump = stabilizeDoubleJumpToggle.isOn;
        ApplyVolume();

        if(inLevel)
            Global.save.SetPlayerStats(UFLevel.GetPlayer<PlayerMovement>());
    }

    private bool LoweringRecord() {
        if(!inLevel)
            return false;
        if(Global.match.RecordIsFrozen())
            return false;

        int oldDifficulty = Global.match.GetCurrentRecordDifficulty();
        int newDifficulty = difficultyDropdown.value;
        return newDifficulty < oldDifficulty;
    }

    private bool TryingIncreaseRecord() {
        if(!inLevel)
            return false;
        if(Global.match.RecordIsFrozen())
            return false;

        int oldDifficulty = Global.match.GetCurrentRecordDifficulty();
        int newDifficulty = difficultyDropdown.value;
        return newDifficulty > oldDifficulty;
    }

    private void OnEnable() {
        LoadProperties();
        for(int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(i == 0);
    }

    public void OnDisable() {
        if(!Global.shutDown) {
            Global.input.SaveBindings();
            SaveProperties();
        }
    }

    private void SetDifficultyText(int value) {
        switch(value) {

        case 0:
        difficultyPropertyText.text =
        "Major boost & double jump\n" +
        "Can use all tools\n" +
        "Can use custom checkpoints";
        difficultyPropertyText.color = new Color(.2f, .8f, 1f);
        break;

        case 1:
        difficultyPropertyText.text =
        "Minor boost to stats\n" +
        "Can use standard tools\n" +
        "Can use custom checkpoints";
        difficultyPropertyText.color = new Color(1f, 1f, 0f);
        break;

        case 2:
        difficultyPropertyText.text =
        "No stats boosts\n" +
        "Necessary tools only\n" +
        "No custom checkpoints";
        difficultyPropertyText.color = new Color(1f, .3f, .3f);
        break;
        default:
        Debug.LogError("Encountered unkown difficulty level: " + difficultyDropdown.value);
        break;
        }
    }

    private void SetKeyBindings() {
        Global.input.LoadBindings();

        int nboBindings = Global.input.bindings.Length;
        Keytarget[] targets = keyTargetParent.GetComponentsInChildren<Keytarget>();

        if(targets.Length != nboBindings) {
            Keytarget baseTarget = targets[0];
            for(int i = 1; i < targets.Length; i++)
                Destroy(targets[i].gameObject);

            targets = new Keytarget[nboBindings];
            targets[0] = baseTarget;

            for(int i = 1; i < nboBindings; i++) {
                targets[i] = Instantiate(baseTarget);
                targets[i].transform.SetParent(keyTargetParent);
                targets[i].transform.localScale = Vector3.one;
            }
        }

        for(int i = 0; i < nboBindings; i++) {
            targets[i].SetBinding(Global.input.bindings[i], true);
        }
    }

    private string FilterName(string name) {
        if(string.IsNullOrEmpty(name))
            return SaveData.PLAYER_DEFAULT_NAME;
        return name;
    }

    public void ChangeDifficulty() {
        int oldValue = difficultyDropdown.value;
        int value = difficultyDropdown.value;

        bool allowShortJump = value < 2;
        if(!allowShortJump)
            shortJumpToggle.isOn = false;
        shortJumpToggle.interactable = allowShortJump;

        bool allowStatsOverride = value < 2;
        overrideStatsToggle.interactable = allowStatsOverride;

        bool allowStableDoubleJump = value <= 0;
        stabilizeDoubleJumpToggle.interactable = allowStableDoubleJump;

        if(LoweringRecord())
            warningText.text = "If you exit the menu your current map record will be downgraded!";
        else if(TryingIncreaseRecord())
            warningText.text = "You have to restart the map to upgrade your map record!";
        else
            warningText.text = "";

        SetDifficultyText(value);
    }

    private void LoadVolume() {
        masterVolumeSlider.value = Global.save.GetVolume(SaveData.VOLUME_MASTER);
        effectsVolumeSlider.value = Global.save.GetVolume(SaveData.VOLUME_EFFECTS);
        musicVolumeSlider.value = Global.save.GetVolume(SaveData.VOLUME_MUSIC);
        ambientVolumeSlider.value = Global.save.GetVolume(SaveData.VOLUME_AMBIENT);
    }

    public void ApplyVolume() {
        if(!initialized)
            return;
        Global.save.SetVolume(SaveData.VOLUME_MASTER, masterVolumeSlider.value);
        Global.save.SetVolume(SaveData.VOLUME_EFFECTS, effectsVolumeSlider.value);
        Global.save.SetVolume(SaveData.VOLUME_MUSIC, musicVolumeSlider.value);
        Global.save.SetVolume(SaveData.VOLUME_AMBIENT, ambientVolumeSlider.value);
    }

}
