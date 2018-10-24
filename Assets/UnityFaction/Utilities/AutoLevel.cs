using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects if game was started in the wrong scene. 
/// If so, it will return to the starting scene, to grab all 
/// static stuff, then return to the new scene.
/// </summary>
public class AutoLevel : MonoBehaviour {

    int returnScene;
    bool readyToLoad;

    private void Awake() {
        bool started = Time.time == 0f;
        if(started && !InStartingScene() && EditorStart()) {
            DontDestroyOnLoad(this.gameObject);
            returnScene = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(0);
        }
        else
            Destroy(this.gameObject);
    }

    private void Update() {
        if(readyToLoad) {
            SceneManager.LoadScene(returnScene);
            Destroy(this.gameObject);
            Debug.LogWarning("Started from non-starting scene. Unexpected behaviour may occur.");
        }
        else if(InStartingScene())
            readyToLoad = true;
    }

    private bool InStartingScene() {
        return SceneManager.GetActiveScene().buildIndex == 0;
    }

    private bool EditorStart() {
        bool editor = false;
#if UNITY_EDITOR
        editor = true;
#endif
        return editor;
    }
}
