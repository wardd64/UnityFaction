using UnityEngine;
using UnityEngine.SceneManagement;

public class Global : MonoBehaviour {

    //General configuration
    public string menuScene;

    public static bool shutDown { get { return global == null; } }

    //singleton structure
    public static Global global{ get {
            if(instance == null) {
                instance = FindObjectOfType<Global>();
                if(instance != null)
                    instance.Awake();
            }
            return instance;
    }}

    public static string multiplayerVersion { get { return levelLauncher.multiplayerVersion; } }

    private static Global instance;

    private void Awake() {
        if(global != this)
            Destroy(this.gameObject);

        DontDestroyOnLoad(this.gameObject);

        launcherInstance = GetComponentInChildren<LevelLauncher>(true);
        igMenuInstance = GetComponentInChildren<IGMenu>(true);
        inputInterfaceInstance = GetComponentInChildren<InputInterface>(true);
        matchProgressInstance = GetComponentInChildren<MatchProgress>(true);
        hudInstance = GetComponentInChildren<HUD>(true);
        saveDataInstance = GetComponentInChildren<SaveData>(true);
    }

    private void Update() {
        //hud toggle
        bool hudState = hudInstance.gameObject.activeSelf ^ Input.GetKeyDown(KeyCode.Alpha2);
        hudState &= InMatchScene();
        hudInstance.gameObject.SetActive(hudState);
    }

    void OnApplicationFocus(bool focus) {
        AudioListener.volume = focus ? 1f : 0f;
    }

    //global scripts
    public static LevelLauncher levelLauncher { get { return global.launcherInstance; } }
    private LevelLauncher launcherInstance;
    public static IGMenu igMenu { get { return global.igMenuInstance; } }
    private IGMenu igMenuInstance;
    public static InputInterface input { get { return global.inputInterfaceInstance; } }
    private InputInterface inputInterfaceInstance;
    public static MatchProgress match { get { return global.matchProgressInstance; } }
    private MatchProgress matchProgressInstance;
    public static HUD hud { get { return global.hudInstance; } }
    private HUD hudInstance;
    public static SaveData save { get { return global.saveDataInstance; } }
    private SaveData saveDataInstance;

    //others
    public static bool InMainMenu() {
        return SceneManager.GetActiveScene().name == global.menuScene;
    }

    public static bool InMatchScene() {
        return IsMatchScene(SceneManager.GetActiveScene());
    }

    public static bool IsMatchScene(Scene scene) {
        return scene.name != global.menuScene;
    }

    public static void LoadMainMenu() {
        
        SceneManager.LoadScene(global.menuScene);
    }
}
