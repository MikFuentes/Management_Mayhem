using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    [Scene] [SerializeField] private string countdownScene = string.Empty;
    [Scene] [SerializeField] private string menuScene = string.Empty;
    [Scene] [SerializeField] private string gameScene = string.Empty;

    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerLobby roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    //[SerializeField] private GameObject roundSystem = null;

    public float matchLength; //in seconds
    public float currentMatchTime;
    public Coroutine timerCoroutine;
    public static event Action<float> OnTimeUpdate;

    public int TotalItems;
    public int ItemsRemaining;

    public float MoraleBar;

    public int remainingSponsorshipslots;

    public Coroutine waitTimerCoroutine;
    public float currentWaitTime;
    public int prev_rand = -1;

    public float currentMessageTime;
    public Coroutine messageTimerCoroutine;
    public static event Action<float> OnMessageTimeUpdate;


    public RuntimeAnimatorController[] animations;
    public List<int> selectedCharacterIndexes;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;

    public static event Action<NetworkConnection> OnServerReadied;
    public static event Action OnServerStopped;
    public static event Action OnTimerCountdown;

    public bool beenCalled;
    public override void OnStartServer()
    {
        Debug.Log("OnStartServer()");
        spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();

        animations = new RuntimeAnimatorController[3];
        animations[0] = Resources.Load<RuntimeAnimatorController>("Animation/Finance");
        animations[1] = Resources.Load<RuntimeAnimatorController>("Animation/Logistics");
        animations[2] = Resources.Load<RuntimeAnimatorController>("Animation/Programs");

        selectedCharacterIndexes.Add(-1);
        selectedCharacterIndexes.Add(-1);
        selectedCharacterIndexes.Add(-1);

        TotalItems = 10;
        ItemsRemaining = TotalItems;

        MoraleBar = 5;

        remainingSponsorshipslots = 2;
        beenCalled = false;

        OnTimeUpdate += HandleTimeUpdate;
        OnMessageTimeUpdate += HandleMessageTimerUpdate;
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

        if (SceneManager.GetActiveScene().path != menuScene)
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
        Debug.Log("OnServerDisconnect()");
        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>(); //get the roomPlayerScript

            RoomPlayers.Remove(player); //remove player from the list

            NotifyPlayersOfReadyState();

            var gamePlayer = conn.identity.GetComponent<NetworkGamePlayerLobby>(); //get the gamePlayerScript
            string message = gamePlayer.gameObject.GetComponent<NetworkGamePlayerLobby>().displayName + " disconnected from the game.";

            //for(int i = 0; i < GamePlayers.Count; i++)
            //{
            //    GamePlayers[i].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, false);
            //}
            if(GamePlayers.Count != 0)
                GamePlayers[0].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, false);

            restartMessageTimer();
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        Debug.Log("OnStopServer()");
        OnServerStopped?.Invoke();
        Debug.Log(RoomPlayers.Count);

        if (RoomPlayers.Count != 0) RoomPlayers[0].ResetPlayerCounts();
        else if (GamePlayers.Count != 0) GamePlayers[0].ResetPlayerCounts();

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
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            if (!IsReadyToStart()) { return; }

            ServerChangeScene("Stage 2 - Coordination");
            //initializeCountdown();
            InitializeTimer();
        }
    }

    public void RestartGame()
    {
        if (SceneManager.GetActiveScene().path == gameScene)
        {
            TotalItems = 10;
            ItemsRemaining = TotalItems;
            MoraleBar = 5;
            remainingSponsorshipslots = 2;
            beenCalled = false;
            ServerChangeScene("Stage 2 - Coordination");
            //initializeCountdown();
            RestartTimer();
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

    private void RestartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        currentMatchTime = matchLength;
        timerCoroutine = StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        OnTimeUpdate?.Invoke(currentMatchTime);
        currentMatchTime--;

        if (currentMatchTime <= 0)
        {
            yield return new WaitForSeconds(1f);
            OnTimeUpdate?.Invoke(currentMatchTime);
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            FindObjectOfType<AudioManager>().Play("GameMusic", false, 1.2f); // stop music
        }
        else if (currentMatchTime == 60)
        {
            yield return new WaitForSeconds(1f);
            string message = "<color=yellow>One minute remaining!</color>";

            //for (int i = 0; i < GamePlayers.Count; i++)
            //{
            //    GamePlayers[i].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, true);
            //}

            if (GamePlayers.Count != 0)
            {
                GamePlayers[0].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, true);
                GamePlayers[0].GetComponent<NetworkGamePlayerLobby>().SpeedUpMusic();
            }

            restartMessageTimer();

            timerCoroutine = StartCoroutine(Timer());
        }
        else if (currentMatchTime == 30 || currentMatchTime == 10)
        {
            yield return new WaitForSeconds(1f);
            string message = "<color=yellow>" + currentMatchTime.ToString() + " seconds remaining!</color>";

            //for (int i = 0; i < GamePlayers.Count; i++)
            //{
            //    GamePlayers[i].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, true);
            //}
            if (GamePlayers.Count != 0)
                GamePlayers[0].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage(message, true);

            restartMessageTimer();
            timerCoroutine = StartCoroutine(Timer());
        }
        //else if (currentMatchTime <= 10 && currentMatchTime > 0)
        //{
        //    yield return new WaitForSeconds(1f);
        //    for (int j = 0; j < GamePlayers.Count; j++)
        //    {
        //        GamePlayers[j].GetComponent<NetworkGamePlayerLobby>().UpdateServerMessage("<color=yellow>" + currentMatchTime.ToString() + "</color>");
        //    }
        //    restartMessageTimer();
        //    timerCoroutine = StartCoroutine(Timer());
        //}
        else
        {
            yield return new WaitForSeconds(1f);
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    //this hook method is triggered when currentMatchTime changes
    private void HandleTimeUpdate(float currentMatchTime)
    {
        for (int i = 0; i < GamePlayers.Count; i++)
        {
            GamePlayers[i].GetComponent<PlayerScript>().currentTime = currentMatchTime; //triggers the hook method of each player
        }
    }

    private void restartMessageTimer()
    {
        if (messageTimerCoroutine != null)
        {
            StopCoroutine(messageTimerCoroutine);
        }
        currentMessageTime = 7;
        messageTimerCoroutine = StartCoroutine(messageTimer());
    }

    private IEnumerator messageTimer()
    {
        currentMessageTime--;
        OnMessageTimeUpdate?.Invoke(currentMessageTime);
        if (currentMessageTime <= 0)
        {
            yield return new WaitForSeconds(1f);
            OnMessageTimeUpdate?.Invoke(currentMessageTime);
            StopCoroutine(messageTimerCoroutine);
            messageTimerCoroutine = null;
        }
        else
        {
            yield return new WaitForSeconds(1f);
            messageTimerCoroutine = StartCoroutine(messageTimer());
        }
    }

    //this hook method is triggered when currentMatchTime changes
    private void HandleMessageTimerUpdate(float currentMessageTime)
    {
        if(GamePlayers.Count != 0)
            GamePlayers[0].GetComponent<NetworkGamePlayerLobby>().HandleMessageTimerUpdate(currentMessageTime);
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
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                //RoomPlayers = [H, C]
                //1, 0
                //leader assigned at last loop
                //leader spawned at last loop
                //GamePlayers = [C, H]
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
        // From game to game
        else if (SceneManager.GetActiveScene().path == gameScene && newSceneName.StartsWith("Stage"))
        {
            //Stacks LIFO 
            Stack<NetworkConnection> connStack = new Stack<NetworkConnection>();
            Stack<GameObject> gameObjectStack = new Stack<GameObject>();

            for (int i = GamePlayers.Count - 1; i >= 0; i--)
            {
                //GamePlayers = [C, H]
                //1, 0
                //stack = [H, C]
                var conn = GamePlayers[i].connectionToClient;
                var gamePlayerInstance = Instantiate(gamePlayerPrefab);
                gamePlayerInstance.SetCharacter(GamePlayers[i].animatorIndex); // Same index as CharacterIndex
                gamePlayerInstance.SetDisplayName(GamePlayers[i].displayName);

                bool isLeader = i == RoomPlayers.Count - 1;

                gamePlayerInstance.IsLeader = isLeader;

                // put into stack
                connStack.Push(conn);
                gameObjectStack.Push(gamePlayerInstance.gameObject);

                NetworkServer.Destroy(conn.identity.gameObject);
            }

            int count = connStack.Count;
            for (int i = 0; i < count; i++)
            {
                //stack = [H, C]
                // 0, 1
                //GamePlayers = [C, H]
                NetworkServer.ReplacePlayerForConnection(connStack.Pop(), gameObjectStack.Pop(), true);
            }
        }
        //From game to menu
        else if (SceneManager.GetActiveScene().path == gameScene && newSceneName.StartsWith("Main"))
        {

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
