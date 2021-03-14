using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkRoomPlayerLobby : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] playerReadyTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] playerCharacterIndexes = new TMP_Text[3];
    [SerializeField] private GameObject[] playerCharacterSprites = new GameObject[3];
    private List<string> SpriteNames = new List<string> { "Finance", "Logistics", "Programs" };

    [SerializeField] private Sprite DefaultSprite;
    [SerializeField] private Sprite[] CharacterSprites = new Sprite[3]; //Canvas only renders Images, not Sprites
    [SerializeField] private Sprite[] OpaqueSprites = new Sprite[3]; //Canvas only renders Images, not Sprites
    [SerializeField] private Button[] leftPlayerButtons = new Button[3];
    [SerializeField] private Button[] rightPlayerButtons = new Button[3];
    [SerializeField] private Button startGameButton = null;
    [SerializeField] private Button readyUpButton = null;

    private GameObject hostDCMessage = null;
    private GameObject backButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;
    public bool Ready = false;
    //public bool onlyHost;
    [SyncVar(hook = nameof(HandleCharacterChanged))]
    public int CharacterIndex = 0;
    [SyncVar]
    public int CharacterSelectedIndex = -1;

    public bool isLeader;
    public bool IsLeader
    {
        set
        {
            isLeader = value;
            startGameButton.gameObject.SetActive(value);
            startGameButton.interactable = false;   
        }
    }

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    public void Start()
    {
        // get a reference to the scene you want to search. 
        Scene s = SceneManager.GetSceneByName("Main Menu");

        GameObject[] gameObjects = s.GetRootGameObjects();

        foreach (GameObject g in gameObjects)
        {
            if (g.name == "Main Camera")
            {
                gameObject.transform.Find("Canvas Lobby").GetComponent<Canvas>().worldCamera = g.GetComponent<Camera>();
            }
            else if (g.name == "Main Menu")
            {
                hostDCMessage = g.transform.Find("Landing_Page/Host_DC_Message_Panel").gameObject;
                backButton = g.transform.Find("Landing_Page/BackButton").gameObject;
            }
        }
    }

    public override void OnStartAuthority()
    {
        Debug.Log("OnStartAuthority()");

        CmdSetDisplayName(PlayerNameInput.DisplayName);
        CmdDeselectCharacter();

        foreach (Button b in leftPlayerButtons)
        {
            b.onClick.AddListener(CmdCharacterLeft);
        }
        foreach (Button b in rightPlayerButtons)
        {
            b.onClick.AddListener(CmdSetCharacterRight);
        }
        lobbyUI.SetActive(true);
    }

    public override void OnStartClient()
    {
        Debug.Log("OnStartClient()");
        Room.RoomPlayers.Add(this);
        Debug.Log(Room.RoomPlayers.Count);

        //CmdDeselectCharacter();

        //Debug.Log(Room.RoomPlayers.Count);
        for (int i = Room.RoomPlayers.Count - 1; i >= 0; i--)
        {
            if (Room.RoomPlayers[i].hasAuthority) //find the one that belongs to us
            {
                leftPlayerButtons[i].gameObject.SetActive(true);
                rightPlayerButtons[i].gameObject.SetActive(true);
            }
        }
        UpdateDisplay();
    }

    public override void OnStopClient()
    {
        Debug.Log("OnStopClient()");
        Room.RoomPlayers.Remove(this);
        Debug.Log(Room.RoomPlayers.Count);

        UpdateDisplay();
    }

    [Command]
    public void CmdResetPlayerCounts()
    {
        if (!isLeader) { return; } // Do nothing
        Debug.Log("CmdResetPlayerCounts()");
        ResetPlayerCounts();
    }

    [Server]
    public void ResetPlayerCounts()
    {
        Debug.Log("ResetPlayerCounts()");
        RpcResetPlayerCount();
    }

    [ClientRpc]
    public void RpcResetPlayerCount()
    {
        for (int i = Room.RoomPlayers.Count-1; i >= 0; --i)
        {
            if (Room.RoomPlayers[i].hasAuthority) //find the one that belongs to us
            {
                hostDCMessage.SetActive(true);
                backButton.SetActive(false);
                if (!Room.RoomPlayers[i].isLeader)
                {
                    Debug.Log("Client left lobby");
                    Room.RoomPlayers[i].LeaveLobby();
                }
                else
                {
                    Debug.Log("Host closed lobby");
                    Room.RoomPlayers[i].CloseLobby();
                }            
            }
        }
    }

    public void LeaveLobby()
    {
        if (isLeader) { return; } // Do nothing

        // Leave the lobby
        Debug.Log("LeaveLobby()");

        Room.StopClient();
        //Debug.Log("StopClient()");
        //Debug.Log(Room.RoomPlayers.Count);

        Room.RoomPlayers.Remove(this);
        //Debug.Log("Room.RoomPlayers.Remove(this)");
        //Debug.Log(Room.RoomPlayers.Count);

        Room.RoomPlayers.Clear(); // Makes Room.RoomPlayers.Count = 0
        //Debug.Log("Room.RoomPlayers.Clear()");
        //Debug.Log(Room.RoomPlayers.Count);
    }

    public void CloseLobby()
    {
        Debug.Log("CloseLobby()");
        Room.StopClient();

        Room.RoomPlayers.Remove(this);

        Room.RoomPlayers.Clear(); // Makes Room.RoomPlayers.Count = 0

        // Close the lobby
        if (Room.RoomPlayers.Count == 0)
        {
            Room.StopHost();
            Debug.Log("StopHost()");
        }
    }

    //public override void OnNetworkDestroy()
    //{
    //    Debug.Log("OnNetworkDestroy()");
    //    Room.RoomPlayers.Remove(this);

    //    UpdateDisplay();
    //}

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay(); //call the method without using the parameters (they are not needed)
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();
    public void HandleCharacterChanged(int oldValue, int newValue) => UpdateDisplay();

    private void UpdateDisplay()
    {
        if (!hasAuthority) //hasAuthority is better than islocalPlayer because isLocalPlayer ONLY refers to the player object, if this doesn't belong to us
        {
            foreach(var player in Room.RoomPlayers)
            {
                if (player.hasAuthority) //find the one that belongs to us
                {
                    player.UpdateDisplay(); //call this method again
                    break;
                }
            }
            return;
        }

        //clear everything
        for(int i = 0; i < playerNameTexts.Length; i++)
        {
            playerNameTexts[i].text = "Waiting For Player...";
            playerReadyTexts[i].text = string.Empty;
            playerCharacterIndexes[i].text = string.Empty;
            playerCharacterSprites[i].GetComponent<Image>().sprite = DefaultSprite;
        }

        //set it all back
        for(int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            playerNameTexts[i].text = Room.RoomPlayers[i].DisplayName;
            playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady ?
                "<color=green>Ready</color>" :
                "<color=red>Not Ready</color>";

            playerCharacterIndexes[i].text = SpriteNames[Room.RoomPlayers[i].CharacterIndex];
            //playerCharacterIndexes[i].text = Room.RoomPlayers[i].CharacterIndex.ToString();
            playerCharacterSprites[i].GetComponent<Image>().sprite = CharacterSprites[Room.RoomPlayers[i].CharacterIndex];
        }
    }

    [Command]
    public void CmdSetCharacter()
    {
        CharacterSelectedIndex = CharacterIndex;

        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            Room.selectedCharacterIndexes[i] = Room.RoomPlayers[i].CharacterSelectedIndex;
        }

        Check();
    }

    [Command]
    public void CmdDeselectCharacter()
    {
        CharacterSelectedIndex = -1;

        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            Room.selectedCharacterIndexes[i] = Room.RoomPlayers[i].CharacterSelectedIndex;
        }

        Check();
    }

    [Command]
    public void CmdCharacterLeft()
    {
        CharacterIndex = (CharacterIndex - 1 + 3) % 3;

        Check();
    }

    [Command]
    public void CmdSetCharacterRight()
    {
        CharacterIndex = (CharacterIndex + 1 + 3) % 3;

        Check();
    }

    [Server]
    public void Check()
    {
        //Enable buttons
        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            for (int j = 0; j < Room.RoomPlayers.Count; j++)
            {
                if (Room.selectedCharacterIndexes[i] != Room.RoomPlayers[j].CharacterIndex && Room.selectedCharacterIndexes[i] == -1)
                {
                    //Debug.Log("Enable: " + j);
                    RpcEnableReadyButton(j, true);
                }
            }
        }

        //Disable buttons
        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            for (int j = 0; j < Room.RoomPlayers.Count; j++)
            {
                if (i != j) //check the other players
                {
                    if (Room.selectedCharacterIndexes[i] == Room.RoomPlayers[j].CharacterIndex && Room.selectedCharacterIndexes[i] != -1)
                    {
                        //Debug.Log("Disable: " + j);
                        RpcEnableReadyButton(j, false);
                    }
                }
            }
        }
    }

    [ClientRpc]
    public void RpcEnableReadyButton(int j, bool Enable)
    {
        Room.RoomPlayers[j].readyUpButton.interactable = Enable;
    }

    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName; //syncvar is changed on the server
    }

    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) { return; }

        startGameButton.interactable = readyToStart;

        if (!readyToStart)
            startGameButton.transform.Find("Start_Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
        else
            startGameButton.transform.Find("Start_Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1f);
    }

    [Command]
    public void CmdReadyUp()
    {
        IsReady = !IsReady; //syncvar is changed on the server

        Room.NotifyPlayersOfReadyState();
    }

    public void ReadyUp()
    {
        Ready = !Ready;

        if (Ready)
        {
            readyUpButton.gameObject.GetComponent<Image>().color = new Color32(214, 36, 17, 255);
            readyUpButton.transform.Find("Ready_Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Cancel";

            CmdSetCharacter();

            for (int i = Room.RoomPlayers.Count - 1; i >= 0; i--)
            {
                if (Room.RoomPlayers[i].hasAuthority) //find the one that belongs to us
                {
                    leftPlayerButtons[i].gameObject.SetActive(false);
                    rightPlayerButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else if (!Ready)
        {
            readyUpButton.gameObject.GetComponent<Image>().color = new Color32(16, 210, 117, 255);
            readyUpButton.transform.Find("Ready_Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Ready Up";

            CmdDeselectCharacter();

            for (int i = Room.RoomPlayers.Count - 1; i >= 0; i--)
            {
                if (Room.RoomPlayers[i].hasAuthority) //find the one that belongs to us
                {
                    leftPlayerButtons[i].gameObject.SetActive(true);
                    rightPlayerButtons[i].gameObject.SetActive(true);
                }
            }
        }

    }

    [Command]
    public void CmdStartGame()
    {
        if(Room.RoomPlayers[0].connectionToClient != connectionToClient) { return; }

        //Start Game

        Room.StartGame();
    }
}
