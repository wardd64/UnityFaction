using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour {

    public Text connectionStatusText;
    public Text connectionSymbolText;
    public Button launchButton, joinButton;

    private float timer;
    private ClientState lastState;

    private const float DOT_CYCLE_TIME = 3f;
    private const int DOT_NUMBER = 3;

    private void OnEnable() {
        launchButton.interactable = false;
        joinButton.interactable = false;
    }

    public void SetReady() {
        launchButton.interactable = true;
        joinButton.interactable = true;
    }

    public void Launch() {
        Global.levelLauncher.JoinOrHostMatch();
        launchButton.interactable = false;
        joinButton.interactable = false;
    }

    public void JoinOnly() {
        Global.levelLauncher.JoinMatch();
        launchButton.interactable = true;
        joinButton.interactable = false;
    }

    void Update () {
        ClientState state = PhotonNetwork.connectionStateDetailed;
        connectionStatusText.text = GetStateDescription(state);

        bool loading = (int)GetGroup(state) >= 3;
        if(!loading)
            timer = 0f;
        else
            timer = (timer + Time.deltaTime) % DOT_CYCLE_TIME;
        int dots = Mathf.FloorToInt(timer * (DOT_NUMBER + 1) / DOT_CYCLE_TIME);
        connectionSymbolText.text = "".PadRight(dots, '.');
    }

    private string GetStateDescription(ClientState state) {
        StateGroup g = GetGroup(state);

        if(g == StateGroup.unkown) {
            Debug.LogError("Unhandled state: " + state);
            return state.ToString();
        }

        if(g == StateGroup.restart)
            return "Non initialized. A restart may be required";

        if(g == StateGroup.waitingForPlayer) {
            if(joinButton.interactable)
                return "Ready";
            else
                return "Waiting for match";
        }
            

        if(g == StateGroup.intermediate)
            return GetStateDescription(lastState);

        lastState = state;
        switch(g) {
        case StateGroup.step1: return "Connecting to server (1/3)";
        case StateGroup.step2: return "Connecting to server (2/3)";
        case StateGroup.step3: return "Connecting to server (3/3)";
        case StateGroup.step4: return "Looking for available matches";
        case StateGroup.step5: return "Joining or hosting match";
        case StateGroup.step6: return "Launching match";
        }
        return "";
    }

    private static StateGroup GetGroup(ClientState state) {
        switch(state) {
        case ClientState.Authenticating:
        case ClientState.Authenticated:
        return StateGroup.intermediate;

        case ClientState.Uninitialized:
        case ClientState.PeerCreated:
        return StateGroup.restart;

        case ClientState.JoinedLobby: return StateGroup.waitingForPlayer;

        case ClientState.ConnectingToNameServer: return StateGroup.step1;

        case ClientState.ConnectedToNameServer:
        case ClientState.ConnectingToMasterserver:
        return StateGroup.step2;

        case ClientState.ConnectedToMaster: return StateGroup.step3;

        case ClientState.ConnectingToGameserver:
        case ClientState.ConnectedToGameserver:
        return StateGroup.step4;

        case ClientState.Joining: return StateGroup.step5;
        case ClientState.Joined: return StateGroup.step6;
        }

        return StateGroup.unkown;
    }

    private enum StateGroup {
        unkown, restart, waitingForPlayer, intermediate, step1, step2, step3, step4, step5, step6
    }
}
