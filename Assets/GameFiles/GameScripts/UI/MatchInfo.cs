using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchInfo : Photon.PunBehaviour {

    public Text mapName, mapAuthor;
    public Transform playerPanelTemplate;

    private void Awake() {
        SceneManager.sceneLoaded += UpdateSceneData;
    }

    private void Update() {
        bool show = Global.input.GetKey("matchInfo");
        show &= Global.InMatchScene() && !Global.igMenu.isOpen;

        for(int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(show);
    }

    private void OnEnable() {
        UpdatePlayerList();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer) {
        UpdatePlayerList();
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer newPlayer) {
        UpdatePlayerList();
    }

    private void UpdateSceneData(Scene scene, LoadSceneMode mode) {
        if(!Global.InMatchScene())
            return;

        UpdatePlayerList();
        int mapEntry = MapList.mapData.GetRow(scene.name, "Scene name");
        mapName.text = MapList.mapData.GetValue(mapEntry, "Title");
        mapAuthor.text = MapList.mapData.GetValue(mapEntry, "Main author");
    }

    private void UpdatePlayerList() {
        //set own player panel
        SetPlayerPanel(playerPanelTemplate, PhotonNetwork.player);

        PhotonPlayer[] playerList = PhotonNetwork.playerList;
        int nbPlayers = playerList.Length;
        Transform panel = playerPanelTemplate.parent;

        for(int i = 0; i < panel.childCount; i++) {
            if(panel.GetChild(i) != playerPanelTemplate)
                Destroy(panel.GetChild(i).gameObject);
        }

        for(int i = 0; i < nbPlayers; i++) {
            if(playerList[i].IsLocal)
                continue;
            GameObject nextPanel = Instantiate(playerPanelTemplate.gameObject, panel);
            SetPlayerPanel(nextPanel.transform, playerList[i]);
        }
    }

    private static void SetPlayerPanel(Transform panel, PhotonPlayer player) {
        if(panel == null)
            return;

        GameObject hostIndicator = panel.GetChild(0).gameObject;
        Text nameText = panel.GetChild(1).GetComponent<Text>();
        Text diffText = panel.GetChild(2).GetComponent<Text>();
        bool ownPlayer = player.IsLocal;

        hostIndicator.SetActive(player.IsMasterClient);
        nameText.text = player.NickName;
        nameText.color = ownPlayer ? Color.white : new Color(.7f, .7f, .7f);

        diffText.text = "Unknown";
        diffText.color = new Color(.6f, .6f, .6f);
    }
}
