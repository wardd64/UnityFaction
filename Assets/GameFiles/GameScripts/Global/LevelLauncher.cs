using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLauncher : MonoBehaviour {

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
 

    public bool SceneIsAvailable(string mapScene) {
        return scenes.Contains(mapScene);
    }

    public void Launch(string mapScene) {
        SceneManager.LoadScene(mapScene);
    }

}
