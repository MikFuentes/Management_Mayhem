﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    [SyncVar] [SerializeField] public string displayName = "Loading...";
    [SerializeField] private TMP_Text playerName = null;
    [SyncVar] [SerializeField] public int animatorIndex = 0;
    public bool timerStarted = false;
    [SerializeField] private Button retryButton = null;

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
            retryButton.gameObject.SetActive(value);
        }
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject); //don't wanna destory the player between levels ?
        Room.GamePlayers.Add(this);

        gameObject.GetComponent<Animator>().runtimeAnimatorController = Room.animations[animatorIndex];
        playerName.text = displayName;

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
}
