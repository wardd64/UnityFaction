using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapListElement : MonoBehaviour {

    public Image backgroundImage, previewBorderImage, 
        verificationImage, ratingImage, previewImage,
        difficultyImage;

    public Text mapTitleText, mapAuthorText, recordText,
        verificationText, creditsText, infoText;

    public Transform votingToggle;

    private string scene;

    public void SetMap(string scene, string title, string author, 
        string credits, string info, float rating, float difficulty) {

        this.scene = scene;
        mapTitleText.text = title;
        mapAuthorText.text = author;
        creditsText.text = credits;
        infoText.text = info;
        ratingImage.fillAmount = rating / 100f;
        difficultyImage.fillAmount = difficulty / 100f;
    }

    public void SetStatus(bool mapAvailable, string valid) {
        if(!mapAvailable)
            SetStatus(MapStatus.Unavailable);
        else if(string.IsNullOrEmpty(valid))
            SetStatus(MapStatus.Unverified);
        else if(valid.Contains("unav"))
            SetStatus(MapStatus.Unavailable);
        else if(valid.Contains("ver"))
            SetStatus(MapStatus.Verified);
        else if(valid.Contains("pos"))
            SetStatus(MapStatus.Possible);
        else if(valid.Contains("unv"))
            SetStatus(MapStatus.Unverified);
        else {
            Debug.LogWarning("Could not parse map valid status text: " + valid);
            SetStatus(MapStatus.Unverified);
        }
    }

    private void SetStatus(MapStatus status) {
        switch(status) {
        case MapStatus.Unavailable:
        verificationText.text = "NOT AVAILABLE";
        verificationImage.color = new Color(.39f, .39f, .39f);
        GetComponent<Button>().interactable = false;
        break;

        case MapStatus.Unverified:
        verificationText.text = "NOT VERIFIED";
        verificationImage.color = Color.red;
        GetComponent<Button>().interactable = true;
        break;

        case MapStatus.Possible:
        verificationText.text = "POSSIBLE";
        verificationImage.color = Color.yellow;
        GetComponent<Button>().interactable = true;
        break;

        case MapStatus.Verified:
        verificationText.text = "VERIFIED";
        verificationImage.color = Color.green;
        GetComponent<Button>().interactable = true;
        break;

        default:
        Debug.LogError("Unkown map status value: " + status);
        break;
        }
    }

    private enum MapStatus {
        Unavailable, Unverified, Possible, Verified
    }

    public void LaunchScene() {
        Global.levelLauncher.Launch(scene);
    }

    public void Vote(int vote) {
        Global.levelLauncher.SetMapPreference(scene, vote);
    }

    public void CycleVote() {
        int voteIdx = 0;
        for(int i = 3; i < 6; i++) {
            if(votingToggle.GetChild(i).gameObject.activeSelf)
                voteIdx = i;
        }

        voteIdx = (voteIdx - 1) % 3;
        votingToggle.GetChild(voteIdx).GetComponent<Button>().onClick.Invoke();
    }
}
