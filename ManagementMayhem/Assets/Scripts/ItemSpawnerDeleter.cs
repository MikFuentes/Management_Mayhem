using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class ItemSpawnerDeleter : NetworkBehaviour
{
    public List<GameObject> item_array = null;
    public GameObject pickup;
    public Vector3 v;
    public int prevRand = -1;


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

        spawnRandomObject();
    }

    public void spawnRandomObject()
    {
        item_array = Resources.LoadAll<GameObject>("SpawnablePrefabs/Boxes").ToList();

        int rand = UnityEngine.Random.Range(0, item_array.Count);

        while (rand == prevRand)
        {
            rand = UnityEngine.Random.Range(0, item_array.Count);
        }
        prevRand = rand;

        GameObject itemPrefab = FindItem(item_array[rand].name);
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
