using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemSpawnerDeleter : NetworkBehaviour
{
    public GameObject pickup;
    public Vector3 v;

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

    private void Start()
    {
        v = gameObject.transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Pickup"))
        {
            pickup = collision.gameObject;

            NetworkServer.Destroy(pickup);
        }

        GameObject itemPrefab = FindItem(pickup.name);
        GameObject item = (GameObject)Instantiate(itemPrefab, v, Quaternion.identity);
        NetworkServer.Spawn(item);
    }

    private GameObject FindItem(string name)
    {
        string temp = "";

        if (name.StartsWith("P") || name.StartsWith("Box"))
        {
            temp = name.Split(')')[0] + ")";
        }
        else
        {
            temp = "Item (" + name + ")";
        }

        foreach (var prefab in Room.spawnPrefabs)
        {
            if (temp == prefab.name)
            {
                return prefab; // return the gameObject
            }
        }
        return null;
    }
}
