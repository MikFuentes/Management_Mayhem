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
    [SerializeField] private Button startGameButton = null;
    [SerializeField] private Button readyUpButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;
    public bool onlyHost;

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

        lobbyUI.SetActive(true);

    }

    public override void OnStartClient()
    {
        Room.RoomPlayers.Add(this);

        UpdateDisplay();
    }

    public override void OnNetworkDestroy()
    {
        Room.RoomPlayers.Remove(this);

        UpdateDisplay();
    }

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay(); //call the method without using the parameters (they are not needed)
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

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
        }

        //set it all back
        for(int i = 0; i < Room.RoomPlayers.Count; i++)
        {
            playerNameTexts[i].text = Room.RoomPlayers[i].DisplayName;
            playerReadyTexts[i].text = Room.RoomPlayers[i].IsReady ?
                "<color=green>Ready</color>" :
                "<color=red>Not Ready</color>";
        }
    }

    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) { return; }

        startGameButton.interactable = readyToStart;
    }

    [Command]
    private void CmdSetDisplayName(string displayName)
    {
        DisplayName = displayName; //syncvar is changed on the server
    }

    [Command]
    public void CmdReadyUp()
    {
        IsReady = !IsReady; //syncvar is changed on the server

        Room.NotifyPlayersOfReadyState();
    }

    public void ReadyUp()
    {
        if (IsReady)
        {
            readyUpButton.gameObject.GetComponent<Image>().color = new Color32(214, 36, 17, 255);
            readyUpButton.transform.Find("Ready_Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Cancel";
        }
        else
        {
            readyUpButton.gameObject.GetComponent<Image>().color = new Color32(16, 210, 117, 255);
            readyUpButton.transform.Find("Ready_Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Ready Up";
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
