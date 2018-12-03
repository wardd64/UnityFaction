using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using ExitGames.Client.Photon.Chat;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class Chat : Photon.PunBehaviour, IChatClientListener {

    //text 0 is the newest, Length - 1 is the oldest
    public Text[] chatText;
    public InputField chatInput;
    public string chatAppID;
    public string channel = "UFChatGeneral";
    private EventSystem es;
    private ChatClient chatClient;
    private Queue<string> ownMessages;
    private float ownMessageTimer;

    public float timerMax = 5f;
    public float backGroundAlpha = 0.4f;
    public float refreshTime = 1f;

    private bool receivingInput;
    private float panelTimer, refreshTimer;
    private int skipShift;
    private const int SKIP_SHIFT = 2;

    private const string DEATH_SUF = "@ded";

    public bool lockInput { get { return receivingInput; } }
    public bool recentOpen { get { return panelTimer != 0f; } }

    private void Awake() {
        for(int i = 0; i < chatText.Length; i++) {
            chatText[i].text = "";
        }
        chatInput.text = "";

        panelTimer = 0f;
        SetBackGroundAlpha(0f);
        SetTextAlpha(0f);
        ownMessages = new Queue<string>();

        //clear evrything
        for(int i = 0; i < chatText.Length; i++) {
            chatText[i].text = "";
            NameHeader(chatText[i]).text = "";
        }

        es = FindObjectOfType<EventSystem>();
    }

    public void Connect() {
        if(PhotonNetwork.offlineMode)
            return;
        chatClient = new ChatClient(this);
        chatClient.ChatRegion = "EU";
        ExitGames.Client.Photon.Chat.AuthenticationValues auth;
        auth = new ExitGames.Client.Photon.Chat.AuthenticationValues(Global.save.playerName);
        chatClient.Connect(chatAppID, Global.multiplayerVersion, auth);
    }

    private void Update() {
        if(!Global.InMatchScene()) {
            panelTimer = 0f;
            SetBackGroundAlpha(0f);
            SetTextAlpha(0f);
            chatInput.gameObject.SetActive(false);
            return;
        }

        bool toggleChat = Global.input.GetKeyDown("chat");
        bool submitChat = Global.input.GetKeyDown("submit");
        bool escapeChat = Global.input.GetKeyDown("escape");
        bool allowChatInput = !Global.igMenu.isOpen;

        if(panelTimer < 0f)
            panelTimer = 0f;

        if(receivingInput) {
            es.SetSelectedGameObject(chatInput.gameObject);
            chatInput.ActivateInputField();
            bool atLimit = chatInput.text.Length == chatInput.characterLimit;
            chatInput.caretColor = atLimit ? Color.clear : Color.white;
        }
        if(!receivingInput)
            chatInput.text = "";

        if(allowChatInput && submitChat) {
            bool realContent = chatInput.text.Trim().Length > 0;
            if(realContent) {
                string message = chatInput.text.Replace("@", "_");
                SendChatMessage(message);
                ShowChat(timerMax);
            }
            else
                CloseChat();
            chatInput.text = "";
            receivingInput = false;

        }
        else if(allowChatInput && !receivingInput && toggleChat) {
            ShowChat();
            receivingInput = true;
            chatInput.gameObject.SetActive(true);
            es.SetSelectedGameObject(chatInput.gameObject);
        }
        else if(allowChatInput && escapeChat) {
            receivingInput = false;
            CloseChat();
        }

        //fade in and out
        if(panelTimer > 0f) {
            panelTimer -= Time.deltaTime;

            if(panelTimer >= 1f) {
                SetBackGroundAlpha(backGroundAlpha);
                SetTextAlpha(1f);
            }
            else if(panelTimer > 0f) {
                //smooth fade out
                SetBackGroundAlpha(panelTimer * backGroundAlpha);
                SetTextAlpha(panelTimer);
            }
            else
                CloseChat();
        }

        chatInput.gameObject.SetActive(receivingInput);

        //shift overflow
        if(skipShift == 0) {
            TextGenerator t = chatText[0].cachedTextGenerator;
            int totalLength = chatText[0].text.Length;
            int visLength = Mathf.Max(0, t.characterCountVisible);
            int overFlowLength = totalLength - visLength;

            //put overflow aside
            string overflow = chatText[0].text.Substring(visLength, overFlowLength);

            //cut off overflow
            chatText[0].text = chatText[0].text.Substring(0, visLength);

            //add new message, containing overflow
            if(!string.IsNullOrEmpty(overflow))
                AddMessage(overflow, chatText[0].color);
        }
        else
            skipShift--;

        refreshTimer += Time.deltaTime;
        if(refreshTimer > refreshTime) {
            refreshTimer = 0f;
            if(chatClient != null)
                chatClient.Service();
        }

        if(ownMessageTimer > 0f) {
            ownMessageTimer += Time.deltaTime;
            if(ownMessageTimer > 15f) {
                ownMessages.Clear();
                ownMessageTimer = 0f;
            }
        }
    }

    void LateUpdate() {
        //fix width for every chat entry
        for(int i = 0; i < chatText.Length; i++)
            SetWidth(chatText[i]);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer) {
        AddMessage(newPlayer.NickName, "Joined the game!", new Color(.6f, 1f, .6f));
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer player) {
        AddMessage(player.NickName, "Left the game!", new Color(1f, .6f, .6f));
    }

    public void DeathMessage(UFPlayerLife.DamageType type) {
        DeathMessage(GetDeathMessage(type));
    }

    private string GetDeathMessage(UFPlayerLife.DamageType type) {
        switch(type) {
        case UFPlayerLife.DamageType.Acid: return "Dissolved in acid";
        case UFPlayerLife.DamageType.ArmorPiercing: return "Got pierced and died";
        case UFPlayerLife.DamageType.Bullet: return "Got shot";
        case UFPlayerLife.DamageType.Electrical: return "Was zapped to death";
        case UFPlayerLife.DamageType.Energy: return "Got obliterated";
        case UFPlayerLife.DamageType.Explosive: return "Exploded";
        case UFPlayerLife.DamageType.Melee: return "Died";
        case UFPlayerLife.DamageType.Scalding: return "Melted";
        case UFPlayerLife.DamageType.ccp: return "Is using a custom checkpoint";
        case UFPlayerLife.DamageType.respawn: return "Respawned";
        case UFPlayerLife.DamageType.exitLevel: return "Got lost in the void";
        case UFPlayerLife.DamageType.unkown: return "Died mysteriously";
        default: return "Died somehow";
        }
    }

    public void DeathMessage(string message) {
        string name = PhotonNetwork.playerName;
        if(PhotonNetwork.offlineMode) {
            ownMessages.Enqueue(name + ":" + message);
            AddMessage(name, message, new Color(1f, .2f, .2f));
        }
        else
            SendChatMessage(name, message + DEATH_SUF);
    }

    /**
	 * Sets width of text field to the correct value, depending on the name header
	 */
    void SetWidth(Text msg) {
        float maxWidth = this.GetComponent<RectTransform>().sizeDelta.x - 10f;
        float nameWidth = NameHeader(msg).rectTransform.sizeDelta.x;
        float height = msg.rectTransform.sizeDelta.y;
        Vector2 newDelta = new Vector2(maxWidth - nameWidth, height);
        msg.rectTransform.sizeDelta = newDelta;
    }

    void SetBackGroundAlpha(float alpha) {
        Image backGround = GetComponent<Image>();
        backGround.color = GetFadedColor(backGround.color, alpha);
    }

    void SetTextAlpha(float alpha) {
        for(int i = 0; i < chatText.Length; i++) {
            chatText[i].color = GetFadedColor(chatText[i].color, alpha);
            NameHeader(chatText[i]).color = GetFadedColor(NameHeader(chatText[i]).color, alpha);
        }
    }

    Color GetFadedColor(Color oldColor, float alpha) {
        return new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
    }

    private void SendChatMessage(string message) {
        SendChatMessage(PhotonNetwork.playerName, message);
    }

    private void SendChatMessage(string name, string message) {
        if(PhotonNetwork.offlineMode) {
            ownMessages.Enqueue(name + ":" + message);
            AddMessage(name, message, Color.white);
        }
        else {
            if(chatClient.CanChatInChannel(channel)) {
                ownMessages.Enqueue(name + ":" + message);
                if(ownMessageTimer == 0f)
                    ownMessageTimer = float.Epsilon;
                chatClient.PublishMessage(channel, message);
            }
            else {
                bool normalChat = !message.EndsWith(DEATH_SUF);
                if(normalChat)
                    AddMessage("Client is still connecting to chat, please wait.", Color.red);
            }
                
        }
            
        if(chatClient != null)
            chatClient.Service();
    }

    /**
	 * Locally adds a chat message and shows the chat.
	 */
    public void AddMessage(string message, Color color) {
        if(string.IsNullOrEmpty(message))
            return;

        if(panelTimer < timerMax)
            panelTimer = timerMax;

        ShiftMessages();

        NameHeader(chatText[0]).text = " ";

        chatText[0].text = message;
        chatText[0].color = color;

        skipShift = SKIP_SHIFT;
    }

    /**
	 * Locally adds a chat message coming from the given player
	 */
    public void AddMessage(string sender, string message, Color messageColor) {
        if(panelTimer < timerMax)
            panelTimer = timerMax;

        ShiftMessages();

        //check if message has us as a sender, if so, change the color
        Color senderColor = new Color(.7f, .7f, .7f);
        if(ownMessages.Count > 0) {
            string matchMessage = sender + ":" + message;
            if(matchMessage == ownMessages.Peek()) {
                senderColor = Color.white;
                ownMessages.Dequeue();
                ownMessageTimer = ownMessages.Count > 0 ? float.Epsilon : 0f;
            }
        }

        //check if message has sender flag attached
        if(message.EndsWith(DEATH_SUF)) {
            message = message.Substring(0, message.Length - DEATH_SUF.Length);
            messageColor = new Color(1f, .2f, .2f);
        }

        NameHeader(chatText[0]).text = sender + ": ";
        NameHeader(chatText[0]).color = senderColor;

        chatText[0].text = message;
        chatText[0].color = messageColor;

        skipShift = SKIP_SHIFT;

        //alert player of a new chat message
        this.GetComponent<AudioSource>().Play();
    }

    private string GetPlayerName(PhotonPlayer player) {
        if(PhotonNetwork.offlineMode)
            return Global.save.playerName;
        string toReturn = player.NickName;
        if(string.IsNullOrEmpty(toReturn) || toReturn.Trim().Length == 0)
            return "NoName";
        return toReturn;
    }

    /**
	 * Instantly closes the chat window
	 */
    public void CloseChat() {
        panelTimer = panelTimer > 0f ? -1f : 0f;
        SetBackGroundAlpha(0f);
        SetTextAlpha(0f);
    }

    /**
	 * Indefenitely shows the chat window
	 */
    public void ShowChat() {
        panelTimer = float.PositiveInfinity;
    }

    /**
	 * Shows the chat window for the given amount of time.
	 */
    public void ShowChat(float time) {
        panelTimer = time;
    }

    /**
	 * Shifts all current chatmessages up by one, opening up chatText[0]
	 */
    void ShiftMessages() {
        //move up older messages
        for(int i = chatText.Length - 1; i > 0; i--) {
            Text prevMes = chatText[i - 1];
            Text nextMes = chatText[i];
            nextMes.color = prevMes.color;
            nextMes.text = prevMes.text;
            NameHeader(nextMes).text = NameHeader(prevMes).text;
            NameHeader(nextMes).color = NameHeader(prevMes).color;
        }
    }

    /**
	 * Returns name header text component associated with the given message text
	 */
    private Text NameHeader(Text msg) {
        return msg.transform.parent.GetComponent<Text>();
    }

    public void DebugReturn(DebugLevel level, string message) {}

    public void OnDisconnected() {}

    public void OnConnected() {
        chatClient.Subscribe(new string[] { channel });
    }

    public void OnChatStateChange(ChatState state) {}

    public void OnGetMessages(string channelName, string[] senders, object[] messages) {
        int nbMessages = senders.Length;
        for(int i = 0; i < nbMessages; i++)
            AddMessage(senders[i], messages[i].ToString(), Color.white);
    }

    public void OnPrivateMessage(string sender, object message, string channelName) {
        AddMessage(sender, message.ToString(), Color.white);
    }

    public void OnSubscribed(string[] channels, bool[] results) {
        AddMessage("Connected to chat!", Color.green);
    }

    public void OnUnsubscribed(string[] channels) {}

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) {}
}
