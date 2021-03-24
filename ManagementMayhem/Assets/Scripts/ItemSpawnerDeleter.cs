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
    public int rand = 0;
    public int prevRand = -1;
    public int prevRandd = -2;
    public Queue<GameObject> objectQueue = new Queue<GameObject>();
    private bool Cooldown = false;

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

    private void Update()
    {
        if (!Cooldown)
        {
            if (objectQueue.Count != 0 && objectQueue.Peek() != null)
            {
                GameObject item = (GameObject)Instantiate(objectQueue.Dequeue(), v, Quaternion.identity);
                NetworkServer.Spawn(item);

                Cooldown = true;
                StartCoroutine(CooldownTimer(3f));
            }        
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {  
        if (collision.gameObject.CompareTag("Pickup"))
        {
            pickup = collision.gameObject;
            NetworkServer.Destroy(pickup);
            queueRandomObject();
        }
    }

    public void queueRandomObject()
    {
        item_array = Resources.LoadAll<GameObject>("SpawnablePrefabs/Boxes").ToList();

        rand = UnityEngine.Random.Range(0, item_array.Count);

        while (rand == prevRand || rand == prevRandd)
        {
            rand = UnityEngine.Random.Range(0, item_array.Count);
        }
        prevRandd = prevRand;
        prevRand = rand;

        GameObject itemPrefab = FindItem(item_array[rand].name);

        objectQueue.Enqueue(itemPrefab);
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
    private IEnumerator CooldownTimer(float time)
    {
        yield return new WaitForSeconds(time);
        Cooldown = false;
    }
}
