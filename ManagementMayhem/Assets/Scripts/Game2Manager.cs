using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Game2Manager : NetworkBehaviour
{
    public int itemsToSpawn;
    public List<GameObject> itemPrefabs = new List<GameObject>();

    float x;
    float y;
    Vector3 pos;

    public void Start()
    {
        //instantiate and spawn the cube
        Debug.Log("Server Start!");
        Cmd_SpawnItem();
    }

    void Cmd_SpawnItem()
    {
        for (int i = 0; i < itemsToSpawn; i++)
        {

            x = Random.Range(-3, 0);
            y = Random.Range(-5, -3);
            pos = new Vector3(x, y, 0);
            itemPrefabs[i].transform.position = pos;

            GameObject item = (GameObject)Instantiate(itemPrefabs[i], itemPrefabs[i].transform);
            NetworkServer.Spawn(item);
        }

    }
}
