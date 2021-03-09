using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using UnityEngine.UI;

public class NPC_Script : NetworkBehaviour
{
    public List<Sprite> item_sprite_list = null;
    public List<Sprite> item_list = null;
    public GameObject sprite_go;
    public Sprite blank_sprite;
    private Transform bar;
    public int rand = 0;
    public int prevRand = -1;
    public float budget;

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
        //RpcResetSprite();
        sprite_go.GetComponent<SpriteRenderer>().sprite = blank_sprite;

        bar = transform.Find("Health_Bar/Bar");

        item_sprite_list = Resources.LoadAll<Sprite>("Val's_Lovely_Art/Pickups/Items").ToList();

        //queueRandomObjects();
        budget = calculateBudget();
    }

    public void RemoveFromArray(string sprite_name)
    {
        for(int i = 0; i < item_list.Count; ++i)
        {
            if(item_list[i].name == sprite_name)
                item_list.RemoveAt(i);
            break;
        }
    }

    public void ChangeSprite(int rand)
    {
        if (rand < 0)
            sprite_go.GetComponent<SpriteRenderer>().sprite = blank_sprite;
        else
            sprite_go.GetComponent<SpriteRenderer>().sprite = item_list[rand]; // assign random sprite from array
    }

    [ClientRpc]
    public void RpcResetSprite()
    {
        sprite_go.GetComponent<SpriteRenderer>().sprite = blank_sprite;
    }

    public void SetSize(float size_normalized)
    {
        bar.localScale = new Vector3(size_normalized, 1f);
    }

    public void SetColor(Color color)
    {
        bar.transform.Find("Bar_Sprite").GetComponent<SpriteRenderer>().color = color;
    }

    //public void queueRandomObjects()
    //{
    //    for(int i = 0; i < Room.TotalItems; i++)
    //    {
    //        rand = UnityEngine.Random.Range(0, item_sprite_list.Count);

    //        while (rand == prevRand)
    //        {
    //            rand = UnityEngine.Random.Range(0, item_sprite_list.Count);
    //        }
    //        prevRand = rand;

    //        item_list.Add(item_sprite_list[rand]);
    //    }
    //}

    private float calculateBudget()
    {
        int budget = 0;
        foreach (var l in item_list)
        {
            foreach (var prefab in Room.spawnPrefabs)
            {
                if ("Box (" + l.name + ")" == prefab.name)
                {
                    budget += (int)prefab.gameObject.GetComponent<PickupProperties>().value;
                }
            }
        }
        return budget;
    }
}
