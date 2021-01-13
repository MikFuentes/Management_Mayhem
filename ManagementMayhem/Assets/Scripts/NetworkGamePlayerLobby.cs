using System.Collections;
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
}
