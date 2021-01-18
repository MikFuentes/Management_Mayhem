using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using UnityEngine.UI;

public class NPC_Script : NetworkBehaviour
{
    public List<Sprite> item_sprite_array = null;
    public GameObject sprite_go;
    public Sprite blank_sprite;
    private Transform bar;

    private void Start()
    {
        //RpcResetSprite();
        sprite_go.GetComponent<SpriteRenderer>().sprite = blank_sprite;
        item_sprite_array = Resources.LoadAll<Sprite>("Val's_Lovely_Art/Pickups/Items").ToList();

        bar = transform.Find("Health_Bar/Bar");
    }

    [ClientRpc]
    public void RpcRemoveFromArray(string sprite_name)
    {
        for(int i = 0; i < item_sprite_array.Count; ++i)
        {
            if(item_sprite_array[i].name == sprite_name)
                item_sprite_array.RemoveAt(i);
        }
    }

    [ClientRpc]
    public void RpcChangeSprite(int rand)
    {
        //Debug.Log(item_sprite_array[rand].name);
        if(rand < 0)
            sprite_go.GetComponent<SpriteRenderer>().sprite = blank_sprite;
        else
            sprite_go.GetComponent<SpriteRenderer>().sprite = item_sprite_array[rand]; // assign random sprite from array
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


}
