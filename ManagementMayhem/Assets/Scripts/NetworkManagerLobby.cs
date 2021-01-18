using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System;
using System.Linq;
using TMPro;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    [Scene] [SerializeField] private string countdownScene = string.Empty;
    [Scene] [SerializeField] private string menuScene = string.Empty;

    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerLobby roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    //[SerializeField] private GameObject roundSystem = null;

    private float matchLength = 180;
    public float currentMatchTime;
    private Coroutine timerCoroutine;

    public float currentWaitTime;

    public RuntimeAnimatorController[] animations;
    public List<int> selectedCharacterIndexes;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnection> OnServerReadied;
    public static event Action OnServerStopped;
    public static event Action OnTimerCountdown;

    public override void OnStartServer()
    { 
        spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();

        animations = new RuntimeAnimatorController[3];
        animations[0] = Resources.Load<RuntimeAnimatorController>("Animation/Finance");
        animations[1] = Resources.Load<RuntimeAnimatorController>("Animation/Logistics");
        animations[2] = Resources.Load<RuntimeAnimatorController>("Animation/Programs");

        selectedCharacterIndexes.Add(-1);
        selectedCharacterIndexes.Add(-1);
        selectedCharacterIndexes.Add(-1);
    } 

    public override void OnStartClient()
    {
        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");

        foreach (var prefab in spawnablePrefabs)
        {
            ClientScene.RegisterPrefab(prefab);
        }

        animations = new RuntimeAnimatorController[3];
        animations[0] = Resources.Load<RuntimeAnimatorController>("Animation/Finance");
        animations[1] = Resources.Load<RuntimeAnimatorController>("Animation/Logistics");
        animations[2] = Resources.Load<RuntimeAnimatorController>("Animation/Programs");
    }

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
        OnServerStopped?.Invoke();

        RoomPlayers.Clear();
        GamePlayers.Clear();
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
            //initializeCountdown();
            InitializeTimer();
        }
    }

    public void StartGameCountdown()
    {
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            if (!IsReadyToStart()) { return; }

            ServerChangeScene("Countdown");
            //initializeCountdown();
            //InitializeTimer();
        }
    }

    private void InitializeTimer()
    {
        currentMatchTime = matchLength;

        timerCoroutine = StartCoroutine(Timer());
    }

    public float GetTime()
    {
        return currentMatchTime;
    }


    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);

        currentMatchTime--;

        if(currentMatchTime <= 0)
        {
            timerCoroutine = null;
            Debug.Log("Time's up!");
        }
        else
        {
            //Debug.Log(currentMatchTime);
            timerCoroutine = StartCoroutine(Timer());
        }
    }
    private void EndGame()
    {
        //set game state to ending

        //set timer to 0
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentMatchTime = 0;

        //show end game UI;
    }


    public override void ServerChangeScene(string newSceneName)
    {
        //From menu to countdown
        if (SceneManager.GetActiveScene().path == menuScene && newSceneName.StartsWith("Countdown"))
        {

        }
        //From menu to game
        else if (SceneManager.GetActiveScene().path == menuScene && newSceneName.StartsWith("Stage"))
        {
            for(int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                var gameplayerInstance = Instantiate(gamePlayerPrefab);
                gameplayerInstance.SetCharacter(RoomPlayers[i].CharacterIndex);
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);

                bool isLeader = i == 0;
                gameplayerInstance.IsLeader = isLeader;

                NetworkServer.Destroy(conn.identity.gameObject); //get rid of room player

                NetworkServer.ReplacePlayerForConnection(conn, gameplayerInstance.gameObject, true); //adding true here gets rid of an error

            }
        }
        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        OnServerReadied?.Invoke(conn);
    }

    //public override void OnServerSceneChanged(string sceneName)
    //{
    //    if (sceneName.StartsWith("Stage"))
    //    {
    //        GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
    //        NetworkServer.Spawn(playerSpawnSystemInstance);

    //        //GameObject roundSystemInstance = Instantiate(roundSystem);
    //        //NetworkServer.Spawn(roundSystemInstance);
    //    }
    //}
}
