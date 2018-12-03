using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IGMenu : MonoBehaviour {

    //menu UI connections
    public GameObject escapeMenu, optionsMenu, mapSelectionMenu;
    public Dropdown qualityDropdown, resolutionDropdown;
    public Text resolutionText, recordText;
    public Slider fovSlider, sensSlider;
    public InputField fovInput, sensInput;
    public Toggle slowScrollToggle;
    public Button restartButton, extendMatchButton, nextMatchButton;

    //debugging UI
    public Text fps;
    public GameObject debugPanel;
    public Text debugText;

    //state variables
    private bool menuOpen;
    private List<Resolution> resolutions;
    Dictionary<string, LightmapData[]> lightmap_data;

    public bool isOpen { get { return menuOpen; } }

    private void Start() {
        CloseMenu();
        resolutions = new List<Resolution>();
        foreach(Resolution res in Screen.resolutions) {
            Resolution strippedRes = default(Resolution);
            strippedRes.width = res.width;
            strippedRes.height = res.height;
            strippedRes.refreshRate = 0;
            if(!resolutions.Contains(strippedRes))
                resolutions.Add(strippedRes);
        }

        resolutions.Reverse();

        fps.gameObject.SetActive(false);
        debugPanel.SetActive(false);

        lightmap_data = new Dictionary<string, LightmapData[]>();
        SetQuality(QualitySettings.GetQualityLevel());
        SceneManager.sceneLoaded += SetLightMaps;
    }

    private void Update() {
        if(Global.InMainMenu())
            menuOpen = false;
        else if(InputInterface.input.GetKeyDown("escape")) {
            if(menuOpen) {
                if(optionsMenu.activeInHierarchy)
                    CloseOptions();
                else if(mapSelectionMenu.activeInHierarchy)
                    CloseOptions();
                else
                    CloseMenu();
            }
            else if(!Global.hud.chat.recentOpen)
                OpenMenu();
        }

        resolutionText.text = Screen.width + " x " + Screen.height;

        Global.match.SetRecordText(recordText);
        UpdateDebug();
    }

    private void UpdateDebug() {
        bool showFPS = fps.gameObject.activeSelf ^ Input.GetKeyDown(KeyCode.Alpha3);
        bool showDebug = debugPanel.activeSelf ^ Input.GetKeyDown(KeyCode.Alpha4);

        fps.gameObject.SetActive(showFPS);
        bool frame = Time.time % 1f < .5f;
        frame &= (Time.time + Time.deltaTime) % 1f >= .5f;
        if(showFPS && frame)
            fps.text = Mathf.RoundToInt(1f / Time.deltaTime).ToString();

        WriteDebugInfo(showDebug);
    }

    private void WriteDebugInfo(bool active) {
        debugPanel.SetActive(active);
        if(!active)
            return;

        PlayerMovement pm = UFLevel.GetPlayer<PlayerMovement>();
        if(pm != null)
            debugText.text = pm.GetDebugText();
        else
            debugText.text = "No player info";
    }

    private void OpenMenu() {
        menuOpen = true;
        escapeMenu.gameObject.SetActive(true);
        restartButton.interactable = PhotonNetwork.offlineMode;
        bool master = !PhotonNetwork.offlineMode && PhotonNetwork.isMasterClient;
        extendMatchButton.gameObject.SetActive(master);
        nextMatchButton.gameObject.SetActive(master);
    }

    public void CloseMenu() {
        for(int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
        menuOpen = false;
    }

    public void ReturnToMainMenu() {
        if(PhotonNetwork.inRoom)
            PhotonNetwork.LeaveRoom();
        CloseMenu();
        Global.LoadMainMenu();
    }

    public void RestartLevel() {
        CloseMenu();
        Global.match.Restart();
    }

    public void Respawn() {
        UFLevel.GetPlayer<PlayerLife>().RespawnDie();
        CloseMenu();
    }

    public void OpenOptions() {
        optionsMenu.SetActive(true);
        mapSelectionMenu.SetActive(false);
        escapeMenu.SetActive(false);

        qualityDropdown.value = QualitySettings.GetQualityLevel();
        sensSlider.value = Global.save.mouseSensitivity;
        fovSlider.value = Global.save.fov;
        slowScrollToggle.isOn = Global.save.slowScroll;
        UpdateAllBySlider();
        SetResolutions();
    }

    public void OpenMapSelection() {
        mapSelectionMenu.SetActive(true);
        optionsMenu.SetActive(false);
        escapeMenu.SetActive(false);
    }

    public void ExtendMatch() {
        Global.match.ExtendMatch();
    }

    public void SkipMatch() {
        Global.match.SkipMatch();
    }

    public void UpdateAllBySlider() {
        int sens = Mathf.RoundToInt(sensSlider.value * 999f);
        sensInput.text = sens.ToString();

        int fov = Mathf.RoundToInt(fovSlider.value);
        fovInput.text = fov.ToString();
        Global.save.fov = fovSlider.value;
    }

    public void UpdateAllByInput() {
        float sens = int.Parse(sensInput.text) / 999f;
        sensSlider.value = sens;
        float fov = int.Parse(fovInput.text);
        fovSlider.value = fov;
    }

    public void SetQuality(int level) {
        QualitySettings.SetQualityLevel(level, true);
        int tier = 1;
        if(level <= 1)
            tier = 0;
        if(level >= 3)
            tier = 2;
        Graphics.activeTier = (UnityEngine.Rendering.GraphicsTier)tier;
        SetLightMaps(level > 0);
    }

    public void CloseOptions() {
        optionsMenu.SetActive(false);
        mapSelectionMenu.SetActive(false);
        escapeMenu.SetActive(!Global.InMainMenu());
        Global.save.mouseSensitivity = sensSlider.value;
        Global.save.fov = fovSlider.value;
        Global.save.slowScroll = slowScrollToggle.isOn;
    }

    public void ToggleFullScreen() {
        Screen.fullScreen = !Screen.fullScreen;
    }

    private void SetResolutions() {
        resolutionDropdown.ClearOptions();

        for (int i = 0; i < resolutions.Count; i++) {
            string resString = resolutions[i].width + " x " + resolutions[i].height;
            resolutionDropdown.options.Add(new Dropdown.OptionData(resString));
        }
     }

    public void SetResolution(int value) {
        Resolution res = resolutions[value];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    private void SetLightMaps(Scene scene, LoadSceneMode mode) {
        bool state = QualitySettings.GetQualityLevel() > 0;
        SetLightMaps(state);
    }

    private void SetLightMaps(bool enabled) {
        string sceneName = SceneManager.GetActiveScene().name;
        bool currentEnabled = LightmapSettings.lightmaps.Length > 0;
        if(enabled && !currentEnabled && lightmap_data.ContainsKey(sceneName))
            LightmapSettings.lightmaps = lightmap_data[sceneName];
        else if(!enabled && currentEnabled) {
            lightmap_data[sceneName] = LightmapSettings.lightmaps;
            LightmapSettings.lightmaps = new LightmapData[] { };
        }
    }
}
