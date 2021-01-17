using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class NetworkRoomPlayerLobby : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] playerReadyTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] playerCharacterIndexes = new TMP_Text[3];
    [SerializeField] private GameObject[] playerCharacterSprites = new GameObject[3];

    [SerializeField] private Sprite DefaultSprite;
    [SerializeField] private Sprite[] CharacterSprites = new Sprite[3]; //Canvas only renders Images, not Sprites
    [SerializeField] private Sprite[] OpaqueSprites = new Sprite[3]; //Canvas only renders Images, not Sprites
    [SerializeField] private Button[] leftPlayerButtons = new Button[3];
    [SerializeField] private Button[] rightPlayerButtons = new Button[3];
    [SerializeField] private Button startGameButton = null;
    [SerializeField] private Button readyUpButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;
    public bool Ready = false;
    //public bool onlyHost;
    [SyncVar(hook = nameof(HandleCharacterChanged))]
    public int CharacterIndex = 0;
    [SyncVar(hook = nameof(HandleCharacterLocked))]
    public int CharacterSelectedIndex = -1;

    private bool isLeader;
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

    public override void OnStartAuthority()
    {
        CmdSetDisplayName(PlayerNameInput.DisplayName);

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
        Room.RoomPlayers.Add(this);

        CmdDeselectCharacter();

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

    public override void OnNetworkDestroy()
    {
        Room.RoomPlayers.Remove(this);

        UpdateDisplay();
    }

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay(); //call the method without using the parameters (they are not needed)
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();
    public void HandleCharacterChanged(int oldValue, int newValue) => UpdateDisplay();
    public void HandleCharacterLocked(int oldValue, int newValue) 
    {
        if(newValue != -1)
        {
            UpdateDisplay();
        }
    }


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

            playerCharacterIndexes[i].text = Room.RoomPlayers[i].CharacterIndex.ToString();
            playerCharacterSprites[i].GetComponent<Image>().sprite = CharacterSprites[Room.RoomPlayers[i].CharacterIndex];

        }
    }

    public void UpdateAvailableSprites()
    {
        //check if highlighted sprite is available
        //if not, disable ready up button

        if (!hasAuthority) //hasAuthority is better than islocalPlayer because isLocalPlayer ONLY refers to the player object, if this doesn't belong to us
        {
            foreach (var player in Room.RoomPlayers)
            {
                if (player.hasAuthority) //find the one that belongs to us
                {
                    player.UpdateDisplay(); //call this method again
                    break;
                }
            }

            return;
        }

        Debug.Log("hi");

        //if (Room.selectedCharacterIndexes.Contains(CharacterIndex))
        //{
        //    readyUpButton.interactable = false;
        //}

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

        //while (Room.selectedCharacterIndexes.Contains(CharacterIndex))
        //{
        //    CharacterIndex = (CharacterIndex - 1 + 3) % 3;
        //}

        Check();
    }

    [Command]
    public void CmdSetCharacterRight()
    {
        CharacterIndex = (CharacterIndex + 1 + 3) % 3;

        //while (Room.selectedCharacterIndexes.Contains(CharacterIndex))
        //{
        //    CharacterIndex = (CharacterIndex + 1 + 3) % 3;
        //}

        Check();
    }

    [Server]
    public void Check()
    {
        for (int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            for (int j = 0; j < Room.RoomPlayers.Count; j++)
            {
                if (i != j)
                {
                    if (Room.selectedCharacterIndexes[i] == Room.RoomPlayers[j].CharacterIndex)
                    {
                        Room.RoomPlayers[j].readyUpButton.interactable = false;
                    }
                    else
                    {
                        Room.RoomPlayers[j].readyUpButton.interactable = true;
                    }
                }

            }

        }
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

    [Command]
    public void CmdLeaveLobby()
    {
        if (isLeader) { Room.StopHost(); }
    }

    public void LeaveLobby()
    {
        if (!isLeader && hasAuthority)
        {
            Room.StopClient();
            Room.RoomPlayers.Clear();
        }
    }
}
