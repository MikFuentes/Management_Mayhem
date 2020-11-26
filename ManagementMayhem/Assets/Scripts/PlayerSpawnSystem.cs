using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
public class PlayerSpawnSystem : NetworkBehaviour
{
    [SerializeField] private NetworkGamePlayerLobby playerPrefab = null;

    private static List<Transform> spawnPoints = new List<Transform>();

    private int nextIndex = 0;

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null)
            {
                return room;
            }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }


    public static void AddSpawnPoint(Transform transform)
    {
        spawnPoints.Add(transform);

        spawnPoints = spawnPoints.OrderBy(x => x.GetSiblingIndex()).ToList();
    }

    public static void RemoveSpawnPoint(Transform transform) => spawnPoints.Remove(transform);

    public override void OnStartServer() => NetworkManagerLobby.OnServerReadied += SpawnPlayer;

    [ServerCallback]
    private void OnDestroy() => NetworkManagerLobby.OnServerReadied -= SpawnPlayer;

    [Server]
    public void SpawnPlayer(NetworkConnection conn)
    {
        Transform spawnPoint = spawnPoints.ElementAtOrDefault(nextIndex);

        if(spawnPoint == null)
        {
            Debug.LogError($"Missing spawn point for player {nextIndex}");
            return;
        }

        var playerInstance = Instantiate(playerPrefab, spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);

        //playerInstance.SetDisplayName(Room.GamePlayers[nextIndex].displayName);

        //NetworkServer.ReplacePlayerForConnection(conn, playerInstance.gameObject, true); //adding true here gets rid of an error

        //NetworkServer.Spawn(playerInstance.gameObject, conn);

        //NetworkServer.AddPlayerForConnection(conn, playerInstance.gameObject);

        nextIndex++;

        Debug.Log(Room.GamePlayers.Count);
        //Debug.Log(nextIndex);

    }
}
