using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour {

    public Animator ccpAlert;
    public Slider ccpProgressSlider;
    public Animator buttonAlert;
    public Text buttonAlertText;
    public Image armorGauge, healthGauge;
    public GameObject timerPanel;
    public Text timerText;
    public Transform weaponTagPanel;

    //Finish plaque
    public GameObject finishPlaque;
    public Text mapTitleText, mapAuthorText, recordLabelText, recordText;

    private const float PROGRESS_OFFSET = .1f;

    private bool inButtonRange;
    private float timer;
    private Timer weaponTagPanelTimer = new Timer(4f, 0f);

    private void Start() {
        timerPanel.SetActive(false);
        weaponTagPanel.gameObject.SetActive(false);
    }

    private void Update() {
        buttonAlert.SetBool("Visible", inButtonRange);
        inButtonRange = false;

        UFPlayerLife life = UFLevel.GetPlayer<UFPlayerLife>();

        float healthFrac = Mathf.Clamp01(life.GetHealth() / UFPlayerLife.MAX_HP);
        float armorFrac = Mathf.Clamp01(life.GetArmor() / UFPlayerLife.MAX_HP);

        healthGauge.color = Color.Lerp(Color.red, Color.green, healthFrac);
        armorGauge.color = Color.Lerp(Color.black, Color.yellow, armorFrac);
        healthGauge.transform.localScale = healthFrac * Vector3.one;
        armorGauge.fillAmount = armorFrac;

        if(timer > 0f) {
            timer -= Time.deltaTime;
            if(timer <= 0f) {
                timer = 0f;
                timerPanel.SetActive(false);
            }
            else 
                timerText.text = UFUtils.GetTimeString(timer, 1);
        }

        if(weaponTagPanelTimer.TickTrigger())
            weaponTagPanel.gameObject.SetActive(false);
    }

    public void SetCCPProgress(float value, float min, float max, bool trying) {
        value -= PROGRESS_OFFSET;
        max -= PROGRESS_OFFSET;

        ccpProgressSlider.value = Mathf.Max(0f, value) / max;
        if(ccpProgressSlider.isActiveAndEnabled)
            ccpProgressSlider.GetComponent<Animator>().SetBool("Visible", trying);
    }

    public void UsedCP() {

    }

    public void PlacedCP() {
        ccpAlert.SetTrigger("Play");
    }

    public void InButtonRange(KeyCode pressKey) {
        inButtonRange = true;
        buttonAlertText.text = "Press " + pressKey;
    }

    public void StartState() {
        finishPlaque.gameObject.SetActive(false);
    }
	
    public void Finish(string map, bool newRecord) {
        finishPlaque.gameObject.SetActive(true);

        Global.save.SetRecordText(map, recordText);
        CSVReader mapList = MainMenu.mapList;
        int i = mapList.GetRow(map, "Scene name");

        string title = mapList.GetValue(i, "Title");
        string author = mapList.GetValue(i, "Main author");

        mapTitleText.text = title;
        mapAuthorText.text = "by " + author + ",";

        if(newRecord) 
            recordLabelText.text = "with a new record:";
        else 
            recordLabelText.text = "without beating your old record:";

        recordText.text += ".";
        Invoke("EndFinish", 10f);
    }

    private void EndFinish() {
        finishPlaque.gameObject.SetActive(false);
    }

    public void SetTimer(float value) {
        timer = value;
        timerPanel.SetActive(value > 0f);
    }

    public float GetTimer() {
        return timer;
    }

    public void ChangeTool(PlayerTool[] tools, int toolIdx) {
        weaponTagPanelTimer.Reset();
        weaponTagPanel.gameObject.SetActive(true);
        for(int i = 0; i < weaponTagPanel.childCount; i++) {
            weaponTagPanel.GetChild(i).gameObject.SetActive(tools[i].CanUse());
            Color color = toolIdx == i ? Color.white : Color.black;
            weaponTagPanel.GetChild(i).GetComponent<Text>().color = color;
        }
    }
}
