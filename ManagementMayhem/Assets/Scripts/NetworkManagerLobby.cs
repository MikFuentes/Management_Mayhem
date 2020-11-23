using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System;
using System.Linq;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    [Scene] [SerializeField] private string menuScene = string.Empty;

    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerLobby roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;

    //public override void OnStartServer() => spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();

    //public override void OnStartClient()
    //{
    //    var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");

    //    foreach (var prefab in spawnablePrefabs)
    //    {
    //        ClientScene.RegisterPrefab(prefab);
    //    }

    //}

    public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
    public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn); //do base logic

        OnClientConnected?.Invoke(); 
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn); //do base logic

        OnClientDisconnected?.Invoke();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        if (numPlayers >= maxConnections)
        {
            //disconnect if too many players
            conn.Disconnect();
            return;
        }

        if(SceneManager.GetActiveScene().path != menuScene)
        {
            //disconnect if not on the menu scene
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            bool isLeader = RoomPlayers.Count == 0; //make the first player the leader

            //if on the menu scene
            //spawn the roomPlayer prefab
            //add the player for connection
            NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(roomPlayerPrefab);

            roomPlayerInstance.IsLeader = isLeader;

            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.identity != null) { 

            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>(); //get the roomPlayerScript

            RoomPlayers.Remove(player); //remove player from the list

            NotifyPlayersOfReadyState();
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        RoomPlayers.Clear();
    }

    public void NotifyPlayersOfReadyState()
    {
        foreach (var player in RoomPlayers)
        {
            player.HandleReadyToStart(IsReadyToStart());
        }
    }

    private bool IsReadyToStart()
    {
        if (numPlayers < minPlayers) { return false; } //don't start if not enough people

        foreach (var player in RoomPlayers)
        {
            if (!player.IsReady) { return false; } //don't start if someone is not ready
        }

        return true;
    }

    public void StartGame()
    {
        if(SceneManager.GetActiveScene().path == menuScene)
        {
            if (!IsReadyToStart()) { return; }

            ServerChangeScene("Stage 2 - Coordination");
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        //From menu to game
        if (SceneManager.GetActiveScene().path == menuScene && newSceneName.StartsWith("Stage"))
        {
            for(int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                var gameplayerInstance = Instantiate(gamePlayerPrefab); //, NetworkManager.startPositions[i]
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);

                NetworkServer.Destroy(conn.identity.gameObject); //get rid of room player

                NetworkServer.ReplacePlayerForConnection(conn, gameplayerInstance.gameObject, true); //adding true here gets rid of an error
            }
        }
        base.ServerChangeScene(newSceneName);
    }
}
