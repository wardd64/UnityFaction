using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

    public GameObject singlePlayerMenu, multiplayerMenu;

    public void Quit() {
        Application.Quit();
    }

    private void Start() {
        Global.hud.gameObject.SetActive(false);
        Global.igMenu.gameObject.SetActive(true);
        UFUtils.SetFPSCursor(false);

        singlePlayerMenu.SetActive(false);
        multiplayerMenu.SetActive(false);

        //turn off test player if necessary
        GameObject testPlayer = FindObjectOfType<PlayerMovement>().gameObject;
        if(testPlayer)
            testPlayer.SetActive(false);
    }

    public void OpenOptions() {
        Global.igMenu.OpenOptions();
    }

    public void OpenMapSelection() {
        Global.igMenu.OpenMapSelection();
    }

    public void OpenMultiplayer() {
        Global.levelLauncher.StartMultiplayer();
    }

    public void CloseMultiplayer() {
        Global.levelLauncher.StopMultiplayer();
    }
}
