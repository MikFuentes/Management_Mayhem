using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    [SyncVar] [SerializeField] public string displayName = "Loading...";
    [SerializeField] private TMP_Text playerName = null;
    [SyncVar] [SerializeField] public int animatorIndex = 0;
    public bool timerStarted = false;
    [SerializeField] private Button retryButton = null;
    [SerializeField] public TMP_Text serverMessage = null;
    private int messageCounter = 0;

    [SerializeField] public TMP_Text Items_Gathered;
    [SerializeField] public TMP_Text Remaining_Balance;
    [SerializeField] public TMP_Text Remaining_Time;
    [SerializeField] public TMP_Text Team_Morale;

    [SerializeField] public TMP_Text Item_Score;
    [SerializeField] public TMP_Text Money_Score;
    [SerializeField] public TMP_Text Time_Score;
    [SerializeField] public TMP_Text Morale_Score;
    [SerializeField] public TMP_Text Total_Score;

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    private bool isLeader;
    public bool IsLeader
    {
        set
        {
            isLeader = value;
            retryButton.gameObject.GetComponent<Button>().interactable = true;
            retryButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1f);
        }
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject); //don't wanna destory the player between levels ?
        Room.GamePlayers.Add(this);

        gameObject.GetComponent<Animator>().runtimeAnimatorController = Room.animations[animatorIndex];
        playerName.text = displayName;

        FindObjectOfType<AudioManager>().Play("GameMusic", false);

        FindObjectOfType<AudioManager>().Play("MenuMusic", false);
        FindObjectOfType<AudioManager>().Play("GameMusic", true);
    }

    public override void OnNetworkDestroy()
    {
        Room.GamePlayers.Remove(this);
    }

    [Server]
    public void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }

    public void UpdateServerMessage(string message)
    {
        if(messageCounter > 3)
        {
            messageCounter = 0;
            serverMessage.text = message;
        }
        else
        {
            serverMessage.text += message + "\n";
        }
        messageCounter++;
    }

    [Server]
    public void SetCharacter(int index)
    {
        this.animatorIndex = index;
        //gameObject.GetComponent<Animator>().runtimeAnimatorController = Room.animations[1];
        //this.gameObject.GetComponent<Animator>().runtimeAnimatorController = Resources.Load("Animation/Logistics") as RuntimeAnimatorController;
    }

    [Command]
    public void CmdRestartGame()
    {
        Debug.Log(Room.GamePlayers[Room.GamePlayers.Count - 1].connectionToClient);
        Debug.Log(connectionToClient);
        //if (Room.GamePlayers[Room.GamePlayers.Count-1].connectionToClient != connectionToClient) { return; }

        //Start Game again

        Room.RestartGame();
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
        for (int i = Room.GamePlayers.Count - 1; i >= 0; --i)
        {
            if (Room.GamePlayers[i].hasAuthority) //find the one that belongs to us
            {
                if (!Room.GamePlayers[i].isLeader)
                {
                    Debug.Log("Client left lobby");
                    Room.GamePlayers[i].LeaveLobby();
                }
                else
                {
                    Debug.Log("Host closed lobby");
                    Room.GamePlayers[i].CloseLobby();
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

        Room.GamePlayers.Remove(this);
        //Debug.Log("Room.RoomPlayers.Remove(this)");
        //Debug.Log(Room.RoomPlayers.Count);

        Room.GamePlayers.Clear(); // Makes Room.RoomPlayers.Count = 0
        //Debug.Log("Room.RoomPlayers.Clear()");
        //Debug.Log(Room.RoomPlayers.Count);

        //Room.ServerChangeScene("Main Menu");
        FindObjectOfType<AudioManager>().Play("GameMusic", false);
        FindObjectOfType<AudioManager>().Play("MenuMusic", true);
        SceneManager.LoadScene("Main Menu");
    }

    public void CloseLobby()
    {
        Debug.Log("CloseLobby()");
        Room.StopClient();

        Room.GamePlayers.Remove(this);

        Room.GamePlayers.Clear(); // Makes Room.RoomPlayers.Count = 0

        // Close the lobby
        if (Room.GamePlayers.Count == 0)
        {
            Room.StopHost();
            Debug.Log("StopHost()");
            FindObjectOfType<AudioManager>().Play("GameMusic", false);
            FindObjectOfType<AudioManager>().Play("MenuMusic", true);
            SceneManager.LoadScene("Main Menu");
        }
    }
}
