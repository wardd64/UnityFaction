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

    private bool inStartingScene { get { return SceneManager.GetActiveScene().buildIndex == 0; } }

    private void Awake() {
        if(!inStartingScene && Global.global == null) {
            //script is needed! save it and load starting scene
            DontDestroyOnLoad(this.gameObject);

            returnScene = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(0);
        }
        else {
            //everything is fine, remove this script
            Destroy(this.gameObject);
        }
    }

    private void Update() {
        if(readyToLoad) {
            SceneManager.LoadScene(returnScene);
            Destroy(this.gameObject);
            Debug.LogWarning("Started from non-starting scene. Unexpected behaviour may occur.");
        }
        else if(inStartingScene)
            readyToLoad = true;
    }
}
